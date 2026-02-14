using System.IO;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
/// Сервис для экспорта и импорта БД и таблиц через backend API (через UnityWebRequest)
/// </summary>
public class DatabaseExportImportService : MonoBehaviour
{
    public static DatabaseExportImportService Instance { get; private set; }
    [SerializeField] private TableApiService tableApiService;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    /// <summary>
    /// Экспорт всей БД (скачать .sql)
    /// </summary>
    public IEnumerator ExportDatabase(string savePath, Action<bool, string> onResult)
    {
        if (tableApiService == null) tableApiService = FindObjectOfType<TableApiService>();
        if (tableApiService == null)
        {
            Debug.LogError("DatabaseExportImportService: TableApiService не найден на сцене");
            onResult?.Invoke(false, "TableApiService not found");
            yield break;
        }
        var url = $"{tableApiService.apiUrl}/database_export_import/export/db";
        // Debug.Log($"[ExportDatabase] GET {url}");
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    // Надёжная запись через FileStream
                    using (var fs = new FileStream(savePath, FileMode.Create, FileAccess.Write))
                    {
                        fs.Write(www.downloadHandler.data, 0, www.downloadHandler.data.Length);
                        fs.Flush();
                    }
                    long fileSize = new FileInfo(savePath).Length;
                    // Debug.Log($"[ExportDatabase] Файл сохранён: {savePath}, размер: {fileSize} байт");
                    onResult?.Invoke(true, null);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ExportDatabase] Ошибка сохранения файла: {e.Message}");
                    onResult?.Invoke(false, $"Ошибка сохранения файла: {e.Message}");
                }
            }
            else
            {
                Debug.LogError($"[ExportDatabase] Ошибка запроса: {www.error}");
                onResult?.Invoke(false, www.error);
            }
        }
    }

    /// <summary>
    /// Импорт всей БД (загрузить .sql)
    /// </summary>
    public IEnumerator ImportDatabase(string filePath, Action<bool, string> onResult)
    {
        if (tableApiService == null) tableApiService = FindObjectOfType<TableApiService>();
        if (tableApiService == null)
        {
            Debug.LogError("DatabaseExportImportService: TableApiService не найден на сцене");
            onResult?.Invoke(false, "TableApiService not found");
            yield break;
        }
        var url = $"{tableApiService.apiUrl}/database_export_import/import/db";
        // Debug.Log($"[ImportDatabase] POST {url}, файл: {filePath}");
        byte[] fileData = File.ReadAllBytes(filePath);
        WWWForm form = new WWWForm();
        string fileName = Path.GetFileName(filePath);
        // Debug.Log($"[IMPORT/EXPORT] Имя файла для отправки: {fileName}");
        // Удаляем не-ASCII символы для избежания MIME-кодировки
        fileName = System.Text.RegularExpressions.Regex.Replace(fileName, "[^a-zA-Z0-9_.-]", "_");
        form.AddBinaryData("file", fileData, fileName, "application/sql");
        using (UnityWebRequest www = UnityWebRequest.Post(url, form))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                // Debug.Log("[ImportDatabase] Импорт завершён успешно");
                onResult?.Invoke(true, null);
            }
            else
            {
                Debug.LogError($"[ImportDatabase] Ошибка: {www.error}");
                onResult?.Invoke(false, www.error);
            }
        }
    }

    /// <summary>
    /// Экспорт таблицы (csv/xlsx)
    /// </summary>
    public IEnumerator ExportTable(string tableName, string format, string savePath, Action<bool, string> onResult)
    {
        if (tableApiService == null) tableApiService = FindObjectOfType<TableApiService>();
        if (tableApiService == null)
        {
            Debug.LogError("DatabaseExportImportService: TableApiService не найден на сцене");
            onResult?.Invoke(false, "TableApiService not found");
            yield break;
        }
        var url = $"{tableApiService.apiUrl}/database_export_import/export/table?table={tableName}&format={format}";
        // Debug.Log($"[ExportTable] GET {url}");
        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    using (var fs = new FileStream(savePath, FileMode.Create, FileAccess.Write))
                    {
                        fs.Write(www.downloadHandler.data, 0, www.downloadHandler.data.Length);
                        fs.Flush();
                    }
                    long fileSize = new FileInfo(savePath).Length;
                    // Debug.Log($"[ExportTable] Файл сохранён: {savePath}, размер: {fileSize} байт");
                    onResult?.Invoke(true, null);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[ExportTable] Ошибка сохранения файла: {e.Message}");
                    onResult?.Invoke(false, $"Ошибка сохранения файла: {e.Message}");
                }
            }
            else
            {
                Debug.LogError($"[ExportTable] Ошибка запроса: {www.error}");
                onResult?.Invoke(false, www.error);
            }
        }
    }

    /// <summary>
    /// Импорт таблицы (csv/xlsx)
    /// </summary>
    public IEnumerator ImportTable(string tableName, string filePath, Action<bool, string> onResult)
    {
        if (tableApiService == null) tableApiService = FindObjectOfType<TableApiService>();
        if (tableApiService == null)
        {
            Debug.LogError("DatabaseExportImportService: TableApiService не найден на сцене");
            onResult?.Invoke(false, "TableApiService not found");
            yield break;
        }
        var url = $"{tableApiService.apiUrl}/database_export_import/import/table?table={tableName}";
        // Debug.Log($"[ImportTable] POST {url}, файл: {filePath}");
        byte[] fileData = File.ReadAllBytes(filePath);
        WWWForm form = new WWWForm();
        string fileName = System.IO.Path.GetFileName(filePath);
        // Debug.Log($"[IMPORT_TABLE] Имя файла для отправки: {fileName}");
        // Удаляем не-ASCII символы для избежания MIME-кодировки
        fileName = System.Text.RegularExpressions.Regex.Replace(fileName, "[^a-zA-Z0-9_.-]", "_");
        form.AddBinaryData("file", fileData, fileName);
        using (UnityWebRequest www = UnityWebRequest.Post(url, form))
        {
            yield return www.SendWebRequest();
            if (www.result == UnityWebRequest.Result.Success)
            {
                // Debug.Log("[ImportTable] Импорт завершён успешно");
                onResult?.Invoke(true, null);
            }
            else
            {
                Debug.LogError($"[ImportTable] Ошибка: {www.error}");
                onResult?.Invoke(false, www.error);
            }
        }
    }
}
