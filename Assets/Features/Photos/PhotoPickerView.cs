using UnityEngine;
using UnityEngine.UI;

public class PhotoPickerView : MonoBehaviour
{
    [Header("Panel & Header")]
    [SerializeField] private GameObject m_ValueSelectModalPanel; // Панель модалки выбора фото
    [SerializeField] private Button m_ValueUploadButton;         // Кнопка загрузки локального файла
    [SerializeField] private Button m_ValueSelectCloseButton;    // Кнопка закрытия модалки

    [Header("Content")]
    [SerializeField] private Transform m_Content;                // Контейнер скролла
    [SerializeField] private GameObject m_PhotoItemPrefab;       // Префаб элемента (RawImage + Button)

    [Header("Footer (Confirm)")]
    [SerializeField] private GameObject m_ValueSelectModalFooter; // Панель с кнопкой подтверждения
    [SerializeField] private Button m_ValueSelectConfirmButton;   // Кнопка подтверждения выбора

    [Header("Sprites (Selection Icons)")]
    [SerializeField] private Sprite m_NotCheckedSprite;          // Иконка невыбранного
    [SerializeField] private Sprite m_CheckedSprite;             // Иконка выбранного

    public GameObject ValueSelectModalPanel => m_ValueSelectModalPanel;
    public Button ValueUploadButton => m_ValueUploadButton;
    public Button ValueSelectCloseButton => m_ValueSelectCloseButton;
    public Transform Content => m_Content;
    public GameObject PhotoItemPrefab => m_PhotoItemPrefab;
    public GameObject ValueSelectModalFooter => m_ValueSelectModalFooter;
    public Button ValueSelectConfirmButton => m_ValueSelectConfirmButton;
    public Sprite NotCheckedSprite => m_NotCheckedSprite;
    public Sprite CheckedSprite => m_CheckedSprite;

    public void Show()
    {
        if (m_ValueSelectModalPanel != null)
            m_ValueSelectModalPanel.SetActive(true);
    }

    public void Hide()
    {
        if (m_ValueSelectModalPanel != null)
            m_ValueSelectModalPanel.SetActive(false);
    }

    public void ClearContent()
    {
        if (m_Content == null) return;
        // Если первый ребёнок используется как шаблон (prefab в сцене) — не удаляем его, просто скрываем
        if (m_Content.childCount > 0)
        {
            var template = m_Content.GetChild(0).gameObject;
            if (template != null) template.SetActive(false);
        }
        for (int i = m_Content.childCount - 1; i > 0; i--)
        {
            var go = m_Content.GetChild(i).gameObject;
            if (go != null) GameObject.Destroy(go);
        }
    }
}


