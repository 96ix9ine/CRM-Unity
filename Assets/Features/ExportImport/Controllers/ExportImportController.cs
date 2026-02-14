using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System;
using Crosstales.FB;

public class ExportImportController : MonoBehaviour
{
    [Header("Dropdown 'Ещё'")]
    public TMPro.TMP_Dropdown moreDropdown;

    [Header("UI для статуса (опционально)")]
    public Text statusText;

    [Header("Popup для выбора формата экспорта")]
    public GameObject formatPopupPanel; // Панель popup
    public Button csvButton;
    public Button xlsxButton;

    [Header("Ссылки на другие компоненты")]
    public MenuButtons menuButtons; // Для получения текущей таблицы

    private string pendingExportFormat = null;

    private void Start()
    {
        if (moreDropdown != null)
            moreDropdown.onValueChanged.AddListener(OnDropdownChanged);
        if (csvButton != null)
            csvButton.onClick.AddListener(() => OnFormatSelected("csv"));
        if (xlsxButton != null)
            xlsxButton.onClick.AddListener(() => OnFormatSelected("xlsx"));
        if (formatPopupPanel != null)
            formatPopupPanel.SetActive(false);
    }

    public void OnDropdownChanged(int index)
    {
        switch (index)
        {
            case 1: OnExportDb(); break;
            case 2: OnImportDb(); break;
            case 3: OnExportTable(); break;
            case 4: OnImportTable(); break;
            default: break;
        }
        moreDropdown.value = 0;
    }

    public void OnExportDb()
    {
        // Debug.Log("[EXPORT_DB] Запуск экспорта БД");
        StartCoroutine(ShowSaveDialogCoroutine("db_dump.sql", (path) => {
            if (string.IsNullOrEmpty(path))
            {
                // Debug.Log("[EXPORT_DB] Экспорт отменён пользователем");
                SetStatus("Экспорт отменён пользователем.");
                NotificationManager.Instance.AddNotification("Экспорт БД отменён пользователем", NotificationType.Warning);
                return;
            }
            // Debug.Log($"[EXPORT_DB] Сохраняем дамп по пути: {path}");
            SetStatus("Экспорт базы данных...");
            StartCoroutine(DatabaseExportImportService.Instance.ExportDatabase(path, (success, error) => {
                if (success)
                {
                    // Debug.Log("[EXPORT_DB] Экспорт завершён успешно");
                    SetStatus("Экспорт базы данных завершён!");
                    NotificationManager.Instance.AddNotification("Экспорт БД завершён успешно", NotificationType.Success);
                }
                else
                {
                    // Debug.LogError($"[EXPORT_DB] Ошибка экспорта: {error}");
                    SetStatus("Ошибка экспорта: " + error);
                    NotificationManager.Instance.AddNotification("Ошибка экспорта БД: " + error, NotificationType.Error);
                }
            }));
        }));
    }

    public void OnImportDb()
    {
        // Debug.Log("[IMPORT_DB] Запуск импорта БД");
        StartCoroutine(ShowLoadDialogCoroutine("sql", false, (path) => {
            if (string.IsNullOrEmpty(path))
            {
                // Debug.Log("[IMPORT_DB] Импорт отменён пользователем");
                SetStatus("Импорт отменён пользователем.");
                NotificationManager.Instance.AddNotification("Импорт БД отменён пользователем", NotificationType.Warning);
                return;
            }
            // Debug.Log($"[IMPORT_DB] Импортируем из файла: {path}");
            SetStatus("Импорт базы данных...");
            StartCoroutine(DatabaseExportImportService.Instance.ImportDatabase(path, (success, error) => {
                if (success)
                {
                    // Debug.Log("[IMPORT_DB] Импорт завершён успешно");
                    SetStatus("Импорт базы данных завершён!");
                    NotificationManager.Instance.AddNotification("Импорт БД завершён успешно", NotificationType.Success);
                }
                else
                {
                    // Debug.LogError($"[IMPORT_DB] Ошибка импорта: {error}");
                    SetStatus("Ошибка импорта: " + error);
                    NotificationManager.Instance.AddNotification("Ошибка импорта БД: " + error, NotificationType.Error);
                }
            }));
        }));
    }

    public void OnExportTable()
    {
        // Debug.Log("[EXPORT_TABLE] Запуск экспорта таблицы, открываем popup выбора формата");
        if (formatPopupPanel != null)
            formatPopupPanel.SetActive(true);
        PopupUI.Instance.Show();
    }

    private void OnFormatSelected(string format)
    {
        // Debug.Log($"[EXPORT_TABLE] Выбран формат: {format}");
        if (formatPopupPanel != null)
            formatPopupPanel.SetActive(false);
        PopupUI.Instance.Hide();
        string tableName = menuButtons != null ? menuButtons.CurrentTableName : null;
        if (string.IsNullOrEmpty(tableName) || string.IsNullOrEmpty(format))
        {
            // Debug.LogWarning("[EXPORT_TABLE] Не выбрана таблица или формат!");
            SetStatus("Не выбрана таблица или формат!");
            NotificationManager.Instance.AddNotification("Экспорт таблицы отменён: не выбрана таблица или формат", NotificationType.Warning);
            return;
        }
        StartCoroutine(ShowSaveDialogCoroutine($"{tableName}.{format}", (path) => {
            if (string.IsNullOrEmpty(path))
            {
                // Debug.Log("[EXPORT_TABLE] Экспорт отменён пользователем");
                SetStatus("Экспорт отменён пользователем.");
                NotificationManager.Instance.AddNotification("Экспорт таблицы отменён пользователем", NotificationType.Warning);
                return;
            }
            // Debug.Log($"[EXPORT_TABLE] Экспортируем таблицу '{tableName}' в файл: {path}, формат: {format}");
            SetStatus("Экспорт таблицы...");
            StartCoroutine(DatabaseExportImportService.Instance.ExportTable(tableName, format, path, (success, error) => {
                if (success)
                {
                    // Debug.Log("[EXPORT_TABLE] Экспорт таблицы завершён успешно");
                    SetStatus("Экспорт таблицы завершён!");
                    NotificationManager.Instance.AddNotification($"Экспорт таблицы '{tableName}' завершён успешно", NotificationType.Success);
                }
                else
                {
                    // Debug.LogError($"[EXPORT_TABLE] Ошибка экспорта: {error}");
                    SetStatus("Ошибка экспорта: " + error);
                    NotificationManager.Instance.AddNotification($"Ошибка экспорта таблицы '{tableName}': {error}", NotificationType.Error);
                }
            }));
        }));
    }

    public void OnImportTable()
    {
        // Debug.Log("[IMPORT_TABLE] Запуск импорта таблицы");
        string tableName = menuButtons != null ? menuButtons.CurrentTableName : null;
        if (string.IsNullOrEmpty(tableName))
        {
            // Debug.LogWarning("[IMPORT_TABLE] Не выбрана таблица!");
            SetStatus("Не выбрана таблица!");
            NotificationManager.Instance.AddNotification("Импорт таблицы отменён: не выбрана таблица", NotificationType.Warning);
            return;
        }
        StartCoroutine(ShowLoadDialogCoroutine("csv,xlsx", false, (path) => {
            if (string.IsNullOrEmpty(path))
            {
                // Debug.Log("[IMPORT_TABLE] Импорт отменён пользователем");
                SetStatus("Импорт отменён пользователем.");
                NotificationManager.Instance.AddNotification("Импорт таблицы отменён пользователем", NotificationType.Warning);
                return;
            }
            // Debug.Log($"[IMPORT_TABLE] Импортируем в таблицу '{tableName}' из файла: {path}");
            SetStatus("Импорт таблицы...");
            StartCoroutine(DatabaseExportImportService.Instance.ImportTable(tableName, path, (success, error) => {
                if (success)
                {
                    // Debug.Log("[IMPORT_TABLE] Импорт таблицы завершён успешно");
                    SetStatus("Импорт таблицы завершён!");
                    NotificationManager.Instance.AddNotification($"Импорт таблицы '{tableName}' завершён успешно", NotificationType.Success);
                }
                else
                {
                    // Debug.LogError($"[IMPORT_TABLE] Ошибка импорта: {error}");
                    SetStatus("Ошибка импорта: " + error);
                    NotificationManager.Instance.AddNotification($"Ошибка импорта таблицы '{tableName}': {error}", NotificationType.Error);
                }
            }));
        }));
    }

    private void SetStatus(string msg)
    {
        if (statusText != null)
            statusText.text = msg;
        // Debug.Log($"[STATUS] {msg}");
    }

    private IEnumerator ShowSaveDialogCoroutine(string defaultName, Action<string> onResult)
    {
        yield return null;
        string ext = System.IO.Path.GetExtension(defaultName);
        string extNoDot = string.IsNullOrEmpty(ext) ? string.Empty : ext.TrimStart('.');
        string path = FileBrowser.Instance.SaveFile("Сохранить", string.Empty, defaultName, extNoDot);
        if (!string.IsNullOrEmpty(path))
        {
            if (!string.IsNullOrEmpty(ext) && !path.EndsWith(ext, StringComparison.OrdinalIgnoreCase))
                path += ext;
            onResult(path);
        }
        else onResult(null);
    }

    private IEnumerator ShowLoadDialogCoroutine(string extensions, bool multiselect, Action<string> onResult)
    {
        yield return null;
        string[] filters = extensions.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
        string primary = filters != null && filters.Length > 0 ? filters[0].Trim().TrimStart('.') : string.Empty;
        var extFilter = new ExtensionFilter("Files", filters);
        string path = FileBrowser.Instance.OpenSingleFile("Открыть", string.Empty, primary, new ExtensionFilter[] { extFilter });
        if (!string.IsNullOrEmpty(path))
        {
            // Валидация расширения, если передавалось несколько вариантов
            if (filters != null && filters.Length > 1)
            {
                var ext = System.IO.Path.GetExtension(path)?.TrimStart('.')?.ToLower();
                bool ok = false;
                foreach (var f in filters) { if (string.Equals(f.Trim().TrimStart('.'), ext, StringComparison.OrdinalIgnoreCase)) { ok = true; break; } }
                if (!ok) { onResult(null); yield break; }
            }
            onResult(path);
        }
        else onResult(null);
    }
} 