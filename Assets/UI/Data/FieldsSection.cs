using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine.Networking;
using System.Threading.Tasks;

/// <summary>
/// Компонент для отображения полей (колонок) таблицы
/// Использует пул объектов для оптимизации производительности
/// </summary>
public class FieldsSection : MonoBehaviour
{
    [SerializeField] private Transform contentParent;    // Контейнер для строк с полями
    [SerializeField] private GameObject rowPrefab;       // Префаб для строки (Text или Button)
    [SerializeField] private TableApiService tableApiService;

    private List<GameObject> rowPool = new List<GameObject>(); // Пул объектов для переиспользования
    private string currentTableName;

    void Start()
    {
        // Инициализируем сервис локализации
        var _ = DatabaseLocalizationService.Instance;
    }

    /// <summary>
    /// Загружает и отображает поля указанной таблицы
    /// </summary>
    public void ShowTableFields(string tableName)
    {
        currentTableName = tableName;
        StartCoroutine(LoadTableFields(tableName));
    }

    /// <summary>
    /// Загружает поля таблицы с сервера и создает UI элементы
    /// Использует пул объектов для оптимизации
    /// </summary>
    private IEnumerator LoadTableFields(string tableName)
    {
        if (tableApiService == null) tableApiService = FindObjectOfType<TableApiService>();
        if (tableApiService == null)
        {
            Debug.LogError("FieldsSection: TableApiService не найден на сцене");
            yield break;
        }
        using (UnityWebRequest webRequest = UnityWebRequest.Get($"{tableApiService.apiUrl}/database/{tableName}/columns"))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var columns = JsonConvert.DeserializeObject<List<string>>(webRequest.downloadHandler.text);

                    // Скрываем все строки из пула
                    foreach (var go in rowPool)
                        go.SetActive(false);

                    // Получаем первый дочерний объект (например, New Text)
                    Transform firstChild = contentParent.childCount > 0 ? contentParent.GetChild(0) : null;

                    // Создаем или переиспользуем строки для каждого поля
                    for (int i = 0; i < columns.Count; i++)
                    {
                        GameObject rowObj;
                        if (i == 0 && firstChild != null)
                        {
                            rowObj = firstChild.gameObject;
                        }
                        else if (i - 1 < rowPool.Count)
                        {
                            rowObj = rowPool[i - 1];
                        }
                        else
                        {
                            rowObj = Instantiate(rowPrefab, contentParent);
                            rowPool.Add(rowObj);
                        }
                        rowObj.SetActive(true);
                        Text rowText = rowObj.GetComponentInChildren<Text>();
                        if (rowText != null)
                        {
                            // Сначала устанавливаем базовое название
                            rowText.text = columns[i];
                            // Затем запускаем корутину для локализации
                            StartCoroutine(SetupFieldLocalization(rowText, tableName, columns[i]));
                        }
                    }
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error parsing fields: {e.Message}");
                    Debug.LogError(webRequest.downloadHandler.text);
                }
            }
            else
            {
                Debug.LogError($"Error loading fields: {webRequest.error}");
            }
        }
    }

    /// <summary>
    /// Устанавливает локализованное название для поля таблицы
    /// </summary>
    private IEnumerator SetupFieldLocalization(Text fieldText, string tableName, string fieldName)
    {
        var task = DatabaseLocalizationService.Instance.GetFieldDisplayNameAsync(tableName, fieldName);
        yield return new WaitUntil(() => task.IsCompleted);
        
        if (task.Result != null)
        {
            fieldText.text = task.Result;
        }
    }

    /// <summary>
    /// Преобразует строку данных в текстовое представление
    /// Используется для отладки
    /// </summary>
    private string RowToString(Dictionary<string, object> row)
    {
        string result = "";
        foreach (var kvp in row)
            result += $"{kvp.Key}: {kvp.Value}  ";
        return result;
    }
}