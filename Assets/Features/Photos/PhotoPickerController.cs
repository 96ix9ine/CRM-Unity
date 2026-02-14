using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections;
using System.Collections.Generic;

public class PhotoPickerController : MonoBehaviour
{
    [SerializeField] private PhotoPickerView m_View;
    [SerializeField] private TableApiService m_Api;

    public event Action<List<(string fileName, Texture2D preview)>> OnPhotosConfirmed; // мультивыбор
    private readonly HashSet<GameObject> m_SelectedItems = new HashSet<GameObject>();
    private readonly Dictionary<GameObject, (string fileName, Texture2D tex)> m_ItemData = new Dictionary<GameObject, (string, Texture2D)>();
    private string m_CurrentTargetType;
    private string m_CurrentTargetId;

    private void Awake()
    {
        if (m_View == null) m_View = GetComponent<PhotoPickerView>();
        if (m_Api == null) m_Api = FindAnyObjectByType<TableApiService>();
    }

    public void Open(string targetType = null, string targetId = null)
    {
        if (m_View == null) return;
        m_CurrentTargetType = targetType;
        m_CurrentTargetId = targetId;
        HookHeaderButtons();
        StartCoroutine(LoadPhotosFromBackend());
        SetupFooter(false);
        m_View.Show();
    }

    public void Close()
    {
        if (m_View == null) return;
        m_View.Hide();
    }

    private void HookHeaderButtons()
    {
        if (m_View.ValueSelectCloseButton != null)
        {
            m_View.ValueSelectCloseButton.onClick.RemoveAllListeners();
            m_View.ValueSelectCloseButton.onClick.AddListener(() => Close());
        }

        if (m_View.ValueUploadButton != null)
        {
            m_View.ValueUploadButton.onClick.RemoveAllListeners();
            m_View.ValueUploadButton.onClick.AddListener(UploadLocalFile);
        }

        if (m_View.ValueSelectConfirmButton != null)
        {
            m_View.ValueSelectConfirmButton.onClick.RemoveAllListeners();
            m_View.ValueSelectConfirmButton.onClick.AddListener(ConfirmSelection);
        }
    }

    private IEnumerator LoadPhotosFromBackend()
    {
        if (m_View == null || m_Api == null) yield break;
        m_View.ClearContent();

        List<Dictionary<string, object>> images = null;
        // Требование: показывать все фото по targetType (таблица), а не только по конкретному id
        if (!string.IsNullOrEmpty(m_CurrentTargetType))
        {
            yield return m_Api.GetJson<List<Dictionary<string, object>>>("/database/images", (list) => images = list);
            if (images != null)
                images = images.FindAll(row => TryGet(row, "targetType") == m_CurrentTargetType);
        }
        else
        {
            // Общая галерея
            yield return m_Api.GetJson<List<Dictionary<string, object>>>("/database/images", (list) => images = list);
        }
        if (images == null) yield break;

        foreach (var row in images)
        {
            string id = TryGet(row, "id");
            string fileName = TryGet(row, "filename");
            string path = TryGet(row, "path"); // uploads/...
            if (string.IsNullOrEmpty(id) && string.IsNullOrEmpty(path)) continue;

            GameObject item = null;
            if (m_View.Content != null && m_View.Content.childCount > 0)
            {
                var template = m_View.Content.GetChild(0).gameObject;
                if (template != null)
                {
                    item = Instantiate(template, m_View.Content);
                    item.SetActive(true);
                }
            }
            if (item == null)
            {
                if (m_View.PhotoItemPrefab == null) continue;
                item = Instantiate(m_View.PhotoItemPrefab, m_View.Content);
            }
            var raw = item.GetComponentInChildren<RawImage>(true);
            var btn = item.GetComponentInChildren<Button>(true);
            Image selectIcon = null;
            if (btn != null)
            {
                // ищем Image строго внутри кнопки (дети кнопки), чтобы не перепутать с RawImage превью
                foreach (var childIcon in btn.GetComponentsInChildren<Image>(true))
                {
                    if (childIcon.gameObject != btn.gameObject) { selectIcon = childIcon; break; }
                }
            }

            Texture2D tex = null; bool loaded = false;
            if (!string.IsNullOrEmpty(id))
            {
                yield return m_Api.GetBytes($"/images/{id}", bytes => {
                    if (bytes != null)
                    {
                        tex = new Texture2D(2,2,TextureFormat.RGBA32,false);
                        tex.LoadImage(bytes);
                    }
                    loaded = true;
                });
            }
            else
            {
                var rel = RelativePathFromFull(path);
                yield return m_Api.GetBytes(rel, bytes => {
                    if (bytes != null)
                    {
                        tex = new Texture2D(2,2,TextureFormat.RGBA32,false);
                        tex.LoadImage(bytes);
                    }
                    loaded = true;
                });
            }
            if (!loaded) yield break;

            if (raw != null && tex != null)
            {
                raw.texture = tex;
                var c = raw.color; c.a = 1f; raw.color = c;
            }

            m_ItemData[item] = (fileName, tex);

            if (selectIcon != null)
            {
                // изначально показываем NotCheckIcon_0 (назначь в префабе)
                SetChecked(selectIcon, false);
            }
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => {
                    bool nowSelected = ToggleSelect(item, selectIcon);
                    SetupFooter(m_SelectedItems.Count > 0);
                });
            }
        }
    }

    private void UploadLocalFile()
    {
#if CT_FILEBROWSER
        var filter = new Crosstales.FB.ExtensionFilter("Images", new string[] {"png","jpg","jpeg"});
        string path = Crosstales.FB.FileBrowser.Instance.OpenSingleFile("Выберите изображение", string.Empty, "png", new Crosstales.FB.ExtensionFilter[] { filter });
        if (!string.IsNullOrEmpty(path)) StartCoroutine(DoUpload(path));
#elif UNITY_EDITOR
        string path = UnityEditor.EditorUtility.OpenFilePanel("Выберите изображение", "", "png,jpg,jpeg");
        if (!string.IsNullOrEmpty(path)) StartCoroutine(DoUpload(path));
#else
        Debug.Log("Файловый диалог не подключён. Добавьте CT_FILEBROWSER или используйте редактор.");
#endif
    }

    private IEnumerator DoUpload(string path)
    {
        if (m_Api == null || string.IsNullOrEmpty(path)) yield break;
        byte[] bytes = null;
        try { bytes = System.IO.File.ReadAllBytes(path); } catch { }
        if (bytes == null || bytes.Length == 0) yield break;
        string fileName = System.IO.Path.GetFileName(path);

        bool ok = false; bool done = false;
        // Если есть контекст — грузим с targetType/targetId текущей записи, иначе в "gallery/0"
        string tt = string.IsNullOrEmpty(m_CurrentTargetType) ? "gallery" : m_CurrentTargetType;
        string tid = string.IsNullOrEmpty(m_CurrentTargetId) ? "0" : m_CurrentTargetId;
        yield return m_Api.UploadImage(tt, tid, fileName, bytes, (succ) => { ok = succ; done = true; });
        if (ok)
        {
            // Перезагрузим список
            StartCoroutine(LoadPhotosFromBackend());
        }
    }

    private string TryGet(Dictionary<string, object> dict, string key)
    {
        if (dict != null && dict.TryGetValue(key, out var v) && v != null) return v.ToString();
        return null;
    }

    private string RelativePathFromFull(string url)
    {
        // Если бэк отдаёт абсолютный url, вычтем apiUrl
        if (string.IsNullOrEmpty(url)) return url;
        if (m_Api == null) return url;
        if (url.StartsWith(m_Api.apiUrl))
        {
            return url.Substring(m_Api.apiUrl.Length);
        }
        // Иначе предполагаем, что уже относительный
        // добавим ведущий слэш, если его нет
        return url.StartsWith("/") ? url : "/" + url;
    }

    private bool ToggleSelect(GameObject item, Image icon)
    {
        bool nowSelected;
        if (m_SelectedItems.Contains(item))
        {
            m_SelectedItems.Remove(item);
            if (icon != null) SetChecked(icon, false);
            nowSelected = false;
        }
        else
        {

            m_SelectedItems.Add(item);
            if (icon != null) SetChecked(icon, true);
            nowSelected = true;
        }
        return nowSelected;
    }

    private void SetChecked(Image icon, bool isChecked)
    {
        if (icon == null) return;
        // Выбор спрайта вынесен в инспектор (PhotoPickerView)
        var sprite = isChecked ? m_View.CheckedSprite : m_View.NotCheckedSprite;
        if (sprite != null) icon.sprite = sprite;
        var col = icon.color; col.a = 1f; icon.color = col;
    }

    private void SetupFooter(bool anySelected)
    {
        if (m_View.ValueSelectModalFooter != null)
            m_View.ValueSelectModalFooter.SetActive(anySelected);
    }

    private void ConfirmSelection()
    {
        var list = new List<(string, Texture2D)>();
        foreach (var go in m_SelectedItems)
        {
            if (go != null && m_ItemData.TryGetValue(go, out var data))
                list.Add((data.fileName, data.tex));
        }
        OnPhotosConfirmed?.Invoke(list);
        Close();
    }
}


