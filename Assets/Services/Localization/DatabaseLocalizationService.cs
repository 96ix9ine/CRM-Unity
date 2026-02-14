using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Threading.Tasks;
using UnityEngine.Networking;
using System;

/// <summary>
/// Сервис для работы с локализацией полей базы данных
/// </summary>
public class DatabaseLocalizationService : MonoBehaviour
{
    private Dictionary<string, TableLocalization> localizationData;
    private static DatabaseLocalizationService instance;
    [SerializeField] private TableApiService tableApiService;
    private bool isInitialized = false;
    private Task initializationTask;

    public static DatabaseLocalizationService Instance
    {
        get
        {
            if (instance == null)
            {
                var go = new GameObject("DatabaseLocalizationService");
                instance = go.AddComponent<DatabaseLocalizationService>();
                DontDestroyOnLoad(go);
            }
            return instance;
        }
    }

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
            initializationTask = LoadLocalizationData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Загружает данные локализации с сервера
    /// </summary>
    private async Task LoadLocalizationData()
    {
        try
        {
            if (tableApiService == null) tableApiService = FindObjectOfType<TableApiService>();
            if (tableApiService == null)
            {
                Debug.LogError("DatabaseLocalizationService: TableApiService не найден на сцене");
                localizationData = new Dictionary<string, TableLocalization>();
                return;
            }
            using (UnityWebRequest webRequest = UnityWebRequest.Get($"{tableApiService.apiUrl}/localization"))
            {
                var operation = webRequest.SendWebRequest();
                while (!operation.isDone)
                    await Task.Yield();

                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    var data = JsonConvert.DeserializeObject<LocalizationData>(webRequest.downloadHandler.text);
                    localizationData = data.tables;
                    isInitialized = true;
                }
                else
                {
                    Debug.LogError($"Ошибка загрузки локализации: {webRequest.error}");
                    localizationData = new Dictionary<string, TableLocalization>();
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Ошибка при загрузке локализации: {e.Message}");
            localizationData = new Dictionary<string, TableLocalization>();
        }
    }

    /// <summary>
    /// Ожидает инициализации сервиса
    /// </summary>
    public async Task WaitForInitialization()
    {
        if (!isInitialized && initializationTask != null)
        {
            await initializationTask;
        }
    }

    /// <summary>
    /// Получает локализованное название таблицы
    /// </summary>
    public async Task<string> GetTableDisplayNameAsync(string tableName)
    {
        await WaitForInitialization();
        if (localizationData.TryGetValue(tableName, out var tableData))
        {
            return tableData.displayName;
        }
        return tableName;
    }

    /// <summary>
    /// Получает локализованное название поля
    /// </summary>
    public async Task<string> GetFieldDisplayNameAsync(string tableName, string fieldName)
    {
        await WaitForInitialization();
        if (localizationData.TryGetValue(tableName, out var tableData) &&
            tableData.fields.TryGetValue(fieldName, out var displayName))
        {
            return displayName;
        }
        return fieldName;
    }

    /// <summary>
    /// Проверяет, есть ли локализация для таблицы
    /// </summary>
    public async Task<bool> HasTableLocalizationAsync(string tableName)
    {
        await WaitForInitialization();
        return localizationData.ContainsKey(tableName);
    }

    /// <summary>
    /// Проверяет, есть ли локализация для поля
    /// </summary>
    public async Task<bool> HasFieldLocalizationAsync(string tableName, string fieldName)
    {
        await WaitForInitialization();
        return localizationData.TryGetValue(tableName, out var tableData) &&
               tableData.fields.ContainsKey(fieldName);
    }

    // Синхронные методы для обратной совместимости
    public string GetTableDisplayName(string tableName)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("Локализация еще не загружена. Используется оригинальное название.");
            return tableName;
        }
        return localizationData.TryGetValue(tableName, out var tableData) ? tableData.displayName : tableName;
    }

    public string GetFieldDisplayName(string tableName, string fieldName)
    {
        if (!isInitialized)
        {
            Debug.LogWarning("Локализация еще не загружена. Используется оригинальное название.");
            return fieldName;
        }
        return localizationData.TryGetValue(tableName, out var tableData) &&
               tableData.fields.TryGetValue(fieldName, out var displayName) ? displayName : fieldName;
    }
}

[System.Serializable]
public class LocalizationData
{
    public Dictionary<string, TableLocalization> tables;
}

[System.Serializable]
public class TableLocalization
{
    public string displayName;
    public Dictionary<string, string> fields;
} 