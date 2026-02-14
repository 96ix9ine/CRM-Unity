using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using Auth.Models;
using UnityEngine.Networking;
using System;
using System.Globalization;
using System.Linq;

/// <summary>
/// Компонент для отображения данных таблицы в виде списка
/// Поддерживает выбор строк, фильтрацию колонок и поиск
/// Использует пул объектов для оптимизации производительности
/// </summary>
public class ListItems : MonoBehaviour
{
    [SerializeField] private Transform headerParent;      // Панель для заголовков
    [SerializeField] private GameObject headerCellPrefab; // Префаб для ячейки заголовка
    [SerializeField] private Transform contentParent;     // Контейнер для строк
    [SerializeField] private GameObject rowPrefab;        // Префаб для строки
    [SerializeField] private Text sectionNameText;        // Поле для названия таблицы
    [SerializeField] private TableApiService tableApiService;
    [SerializeField] private FiltersListItems filtersListItems; // Компонент фильтров
    [SerializeField] private SearchField searchField;     // Компонент поиска
    [SerializeField] public PaginationListItems paginationListItems; // Компонент пагинации

    private List<GameObject> headerPool = new List<GameObject>(); // Пул заголовков
    private List<GameObject> rowPool = new List<GameObject>();    // Пул строк
    private List<List<GameObject>> cellPools = new List<List<GameObject>>(); // Пул ячеек для каждой строки
    public event Action<Dictionary<string, object>> OnRowSelected; // Событие выбора строки
    private int selectedRowIndex = -1;                    // Индекс выбранной строки
    private List<Dictionary<string, object>> currentRows = new List<Dictionary<string, object>>(); // Текущие данные
    public List<Dictionary<string, object>> CurrentRows => currentRows;

    private List<string> selectedColumns = new List<string>(); // Выбранные колонки
    private string currentTableName;                      // Текущая таблица
    private List<string> allColumns = new List<string>(); // Все колонки таблицы

    private Dictionary<string, string> localizedFieldNames = new Dictionary<string, string>();
    private Dictionary<string, string> localizedTableNames = new Dictionary<string, string>();

    // --- Добавляем коллекцию скрываемых колонок ---
    private static readonly HashSet<string> HiddenColumns = new HashSet<string> { "id" };

    // Централизованный метод фильтрации
    private List<string> GetVisibleColumns(List<string> columns)
    {
        return columns?.Where(col => !HiddenColumns.Contains(col.ToLower())).ToList() ?? new List<string>();
    }

    void Start()
    {
        if (filtersListItems != null)
            filtersListItems.OnApplyFilters += SetSelectedColumns;
        // Гарантируем перерисовку таблицы после сохранений/удалений через модалку
        var editController = FindAnyObjectByType<TableEditController>();
        if (editController != null)
        {
            editController.OnRecordSaved += () => {
                if (!string.IsNullOrEmpty(currentTableName))
                    StartCoroutine(ShowTableData(currentTableName));
            };
            editController.OnRecordDeleted += () => {
                if (!string.IsNullOrEmpty(currentTableName))
                    StartCoroutine(ShowTableData(currentTableName));
            };
        }
    }

    /// <summary>
    /// Отображает данные указанной таблицы
    /// </summary>
    public IEnumerator ShowTableData(string tableName)
    {
        currentTableName = tableName;
        if (filtersListItems != null) 
        {
            filtersListItems.SelectAllToggles();
        }
        selectedColumns = null;
        selectedRowIndex = -1;
        currentRows.Clear();
        if (sectionNameText != null)
        {
            // Используем displayName, если есть
            if (localizedTableNames != null && localizedTableNames.ContainsKey(tableName))
                sectionNameText.text = localizedTableNames[tableName];
            else
                sectionNameText.text = tableName;
        }
        yield return StartCoroutine(LoadTableNamesLocalization());
        yield return StartCoroutine(LoadLocalizationAndTableData(tableName));
        yield break;
    }

    private IEnumerator LoadTableNamesLocalization()
    {
        if (tableApiService == null) tableApiService = FindObjectOfType<TableApiService>();
        using (UnityWebRequest locRequest = UnityWebRequest.Get($"{tableApiService.apiUrl}/localization/tables"))
        {
            // Добавляем заголовок авторизации
            string token = PlayerPrefs.GetString("token", "");
            if (!string.IsNullOrEmpty(token))
                locRequest.SetRequestHeader("Authorization", "Bearer " + token);

            yield return locRequest.SendWebRequest();
            if (locRequest.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    // Ожидается структура: { "tableName": "displayName", ... }
                    localizedTableNames = JsonConvert.DeserializeObject<Dictionary<string, string>>(locRequest.downloadHandler.text);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Ошибка парсинга локализации таблиц: {e.Message}");
                }
            }
            else
            {
                Debug.LogError($"Ошибка загрузки локализации таблиц: {locRequest.error}");
            }
        }
    }

    private IEnumerator LoadLocalizationAndTableData(string tableName)
    {
        // Загружаем локализацию полей
        if (tableApiService == null) tableApiService = FindObjectOfType<TableApiService>();
        using (UnityWebRequest locRequest = UnityWebRequest.Get($"{tableApiService.apiUrl}/localization/tables/{tableName}/fields"))
        {
            // Добавляем заголовок авторизации
            string token = PlayerPrefs.GetString("token", "");
            if (!string.IsNullOrEmpty(token))
                locRequest.SetRequestHeader("Authorization", "Bearer " + token);

            yield return locRequest.SendWebRequest();
            if (locRequest.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    localizedFieldNames = JsonConvert.DeserializeObject<Dictionary<string, string>>(locRequest.downloadHandler.text);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Ошибка парсинга локализации: {e.Message}");
                    localizedFieldNames = new Dictionary<string, string>();
                }
            }
            else
            {
                Debug.LogError($"Ошибка загрузки локализации: {locRequest.error}");
                localizedFieldNames = new Dictionary<string, string>();
            }
        }
        yield return StartCoroutine(LoadTableData(tableName));
    }

    /// <summary>
    /// Загружает данные таблицы с сервера и создает UI элементы
    /// Использует пул объектов для оптимизации
    /// Приоритетно использует generateData endpoint, если доступен
    /// </summary>
    private IEnumerator LoadTableData(string tableName)
    {
        // Деактивируем исходный префаб в иерархии
        Transform originalRowPrefabTransform = contentParent.Find("ListItemsRowPrefab");
        if (originalRowPrefabTransform != null && originalRowPrefabTransform.gameObject.activeSelf)
        {
            originalRowPrefabTransform.gameObject.SetActive(false);
        }

        // Скрываем все строки
        foreach (var go in rowPool)
            go.SetActive(false);

        // Получаем строки данных с приоритетом generateData
        yield return StartCoroutine(LoadTableDataWithGenerateData(tableName, selectedColumns));
    }

    /// <summary>
    /// Загружает данные таблицы с приоритетом generateData endpoint
    /// </summary>
    private IEnumerator LoadTableDataWithGenerateData(string tableName, List<string> displayColumns)
    {
        // Сначала пытаемся использовать generateData
        if (tableApiService == null) tableApiService = FindObjectOfType<TableApiService>();
        using (UnityWebRequest generateDataRequest = UnityWebRequest.Get($"{tableApiService.apiUrl}/{tableName}/generateData"))
        {
            // Добавляем заголовок авторизации
            string token = PlayerPrefs.GetString("token", "");
            if (!string.IsNullOrEmpty(token))
                generateDataRequest.SetRequestHeader("Authorization", "Bearer " + token);

            yield return generateDataRequest.SendWebRequest();
            
            if (generateDataRequest.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var rows = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(generateDataRequest.downloadHandler.text);
                    Debug.Log($"Успешно получены данные через generateData для таблицы {tableName}");
                    ProcessTableData(rows, displayColumns);
                    yield break;
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"Ошибка парсинга generateData для {tableName}: {e.Message}");
                    // Продолжаем к стандартному методу
                }
            }
            else
            {
                Debug.Log($"generateData недоступен для таблицы {tableName}, используем стандартный метод");
            }
        }

        // Если generateData недоступен, используем стандартный метод
        if (tableApiService == null) tableApiService = FindObjectOfType<TableApiService>();
        using (UnityWebRequest webRequest = UnityWebRequest.Get($"{tableApiService.apiUrl}/database/{tableName}"))
        {
            // Добавляем заголовок авторизации
            string token = PlayerPrefs.GetString("token", "");
            if (!string.IsNullOrEmpty(token))
                webRequest.SetRequestHeader("Authorization", "Bearer " + token);

            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var rows = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(webRequest.downloadHandler.text);
                    Debug.Log($"Получены данные через стандартный метод для таблицы {tableName}");
                    ProcessTableData(rows, displayColumns);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error parsing table data: {e.Message}");
                    Debug.LogError(webRequest.downloadHandler.text);
                }
            }
            else
            {
                Debug.LogError($"Error loading table data: {webRequest.error}");
            }
        }
    }

    /// <summary>
    /// Обрабатывает полученные данные таблицы и создает UI элементы
    /// Теперь строит колонки динамически по ключам из данных
    /// </summary>
    private void ProcessTableData(List<Dictionary<string, object>> rows, List<string> displayColumns)
    {
        currentRows = rows;

        // Динамически определяем все уникальные ключи (колонки)
        var keysSet = new HashSet<string>();
        if (rows != null && rows.Count > 0)
        {
            foreach (var row in rows)
                foreach (var key in row.Keys)
                    keysSet.Add(key);
        }
        allColumns = keysSet.ToList();

        // Если selectedColumns не установлены или пусты, используем все колонки
        if (selectedColumns == null || selectedColumns.Count == 0)
            selectedColumns = new List<string>(allColumns);

        // --- ГАРАНТИЯ: selectedColumns всегда фильтруется для UI ---
        selectedColumns = GetVisibleColumns(selectedColumns);

        // Если displayColumns не передан или пуст, используем selectedColumns
        if (displayColumns == null || displayColumns.Count == 0)
            displayColumns = selectedColumns;

        // --- Централизованная фильтрация ---
        var visibleColumns = GetVisibleColumns(displayColumns);

        // Передаем данные в SearchField
        if (searchField != null)
        {
            searchField.SetOriginalData(rows);
        }
        
        // --- ФИКС: сбрасываем всю пагинацию (TreeView-режим и обычный) при обновлении данных ---
        //   if (paginationListItems != null)
        //     paginationListItems.SetAllRows(currentRows);
        
        // Гарантируем, что пул ячеек не меньше количества строк
        while (cellPools.Count < rows.Count)
            cellPools.Add(new List<GameObject>());

        // Обновляем заголовки
        foreach (var go in headerPool)
            go.SetActive(false);
        for (int i = 0; i < visibleColumns.Count; i++)
        {
            GameObject headerCell;
            Text headerText;
            if (i == 0)
            {
                Transform firstChild = headerParent.GetChild(0);
                if (firstChild != null && firstChild.GetComponent<Image>() != null)
                {
                    headerCell = firstChild.gameObject;
                    headerText = headerCell.GetComponentInChildren<Text>();
                }
                else
                {
                    if (i < headerPool.Count)
                        headerCell = headerPool[i];
                    else
                    {
                        headerCell = Instantiate(headerCellPrefab, headerParent);
                        headerPool.Add(headerCell);
                    }
                    headerText = headerCell.GetComponentInChildren<Text>();
                }
            }
            else
            {
                int poolIndex = i - 1;
                if (poolIndex < headerPool.Count)
                    headerCell = headerPool[poolIndex];
                else
                {
                    headerCell = Instantiate(headerCellPrefab, headerParent);
                    headerPool.Add(headerCell);
                }
                headerText = headerCell.GetComponentInChildren<Text>();
            }
            headerCell.SetActive(true);
            if (headerText != null)
            {
                string col = visibleColumns[i];
                headerText.text = localizedFieldNames.ContainsKey(col) ? localizedFieldNames[col] : col;
            }
        }

        // Создаем или переиспользуем строки
        for (int i = 0; i < rows.Count; i++)
        {
            GameObject rowObj;
            if (i < rowPool.Count)
                rowObj = rowPool[i];
            else
            {
                rowObj = Instantiate(rowPrefab, contentParent);
                rowPool.Add(rowObj);
            }
            rowObj.SetActive(true);

            // Обновляем обработчик клика
            int rowIdx = i;
            var btn = rowObj.GetComponent<UnityEngine.UI.Button>();
            if (btn == null)
                btn = rowObj.AddComponent<UnityEngine.UI.Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnRowClicked(rowIdx));

            // Обновляем выделение
            var img = rowObj.GetComponent<UnityEngine.UI.Image>();
            if (img != null)
                img.color = (rowIdx == selectedRowIndex) ? new Color(0.8f, 0.9f, 1f, 1f) : Color.white;

            // Пул ячеек для этой строки
            var cellPool = cellPools[i];
            while (cellPool.Count < visibleColumns.Count)
            {
                GameObject newCell = Instantiate(headerCellPrefab, rowObj.transform);
                cellPool.Add(newCell);
            }
            foreach (var cell in cellPool)
                cell.SetActive(false);

            // Заполняем только по visibleColumns
            for (int j = 0; j < visibleColumns.Count; j++)
            {
                GameObject cell = cellPool[j];
                Text text = cell.GetComponentInChildren<Text>();
                cell.SetActive(true);
                if (cell.transform.parent != rowObj.transform || cell.transform.GetSiblingIndex() != j)
                {
                    cell.transform.SetParent(rowObj.transform, false);
                    cell.transform.SetSiblingIndex(j);
                }
                var column = visibleColumns[j];
                var cellValue = rows[i].ContainsKey(column) && rows[i][column] != null ? rows[i][column].ToString() : "";
                if (column.ToLower().Contains("date"))
                    text.text = FormatDateOnly(cellValue);
                else
                    text.text = cellValue;
            }
            for (int j = visibleColumns.Count; j < cellPool.Count; j++)
                cellPool[j].SetActive(false);
        }
        for (int i = rows.Count; i < rowPool.Count; i++)
            rowPool[i].SetActive(false);
        for (int i = rows.Count; i < cellPools.Count; i++)
            foreach (var cell in cellPools[i])
                cell.SetActive(false);
        if (paginationListItems != null && paginationListItems.isActiveAndEnabled)
        {
            paginationListItems.SetAllRows(currentRows);
            // Синхронизируем UI и логику пагинации
            var dropdown = typeof(PaginationListItems).GetField("dropdown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(paginationListItems) as TMPro.TMP_Dropdown;
            if (dropdown != null)
                paginationListItems.GetType().GetMethod("OnDropdownChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(paginationListItems, new object[] { dropdown.value });
        }
        
        // Настраиваем фильтры
        if (filtersListItems != null && !string.IsNullOrEmpty(currentTableName))
        {
            filtersListItems.SetupFilters(currentTableName, currentRows);
        }
    }

    /// <summary>
    /// Обработчик клика по строке
    /// Обновляет выделение и вызывает событие OnRowSelected
    /// </summary>
    private void OnRowClicked(int rowIdx)
    {
        selectedRowIndex = rowIdx;
        OnRowSelected?.Invoke(currentRows[rowIdx]);
        // Обновляем выделение
        for (int i = 0; i < rowPool.Count; i++)
        {
            var img = rowPool[i].GetComponent<UnityEngine.UI.Image>();
            if (img != null)
                img.color = (i == selectedRowIndex) ? new Color(0.8f, 0.9f, 1f, 1f) : Color.white;
        }
    }

    /// <summary>
    /// Устанавливает выбранные колонки и обновляет отображение
    /// </summary>
    public void SetSelectedColumns(List<string> columns)
    {
        selectedColumns = columns;
        
        // Если у нас уже есть данные, просто обновляем отображение
        if (currentRows != null && currentRows.Count > 0)
        {
            ProcessTableData(currentRows, selectedColumns);
        }
        else if (!string.IsNullOrEmpty(currentTableName))
        {
            // Если данных нет, загружаем их
            StartCoroutine(LoadTableData(currentTableName));
        }
    }

    /// <summary>
    /// Обновляет отображение данных без перезагрузки с сервера
    /// </summary>
    public void UpdateTableWithData(List<Dictionary<string, object>> rows)
    {
        currentRows = rows;

        // Гарантируем, что пул строк не меньше количества отображаемых строк
        while (rowPool.Count < rows.Count)
        {
            var rowObj = Instantiate(rowPrefab, contentParent);
            rowPool.Add(rowObj);
        }

        // Гарантируем, что пул ячеек не меньше количества строк
        while (cellPools.Count < rows.Count)
            cellPools.Add(new List<GameObject>());

        for (int i = 0; i < rows.Count; i++)
        {
            GameObject rowObj = rowPool[i];
            rowObj.SetActive(true);

            // Удаляем первый пустой Image, если он есть (аналогично как было раньше)
            if (rowObj.transform.childCount > 0)
            {
                Transform firstRowChild = rowObj.transform.GetChild(0);
                Image firstRowImageComponent = firstRowChild.GetComponent<Image>();
                if (firstRowImageComponent != null && firstRowChild.GetComponentInChildren<Text>(true) == null)
                {
                    Destroy(firstRowChild.gameObject);
                }
            }

            // Обновляем обработчик клика
            int rowIdx = i;
            var btn = rowObj.GetComponent<UnityEngine.UI.Button>();
            if (btn == null)
                btn = rowObj.AddComponent<UnityEngine.UI.Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => OnRowClicked(rowIdx));

            // Обновляем выделение
            var img = rowObj.GetComponent<UnityEngine.UI.Image>();
            if (img != null)
                img.color = (rowIdx == selectedRowIndex) ? new Color(0.8f, 0.9f, 1f, 1f) : Color.white;

            // Пул ячеек для этой строки
            var cellPool = cellPools[i];

            // Гарантируем, что пул ячеек не меньше количества колонок
            while (cellPool.Count < selectedColumns.Count)
            {
                GameObject newCell = Instantiate(headerCellPrefab, rowObj.transform);
                cellPool.Add(newCell);
            }

            // Скрываем все ячейки из пула перед использованием
            foreach (var cell in cellPool)
                cell.SetActive(false);

            // Используем ячейки из пула для всех колонок
            for (int j = 0; j < selectedColumns.Count; j++)
            {
                GameObject cell = cellPool[j];
                Text text = cell.GetComponentInChildren<Text>();

                cell.SetActive(true);
                if (text != null)
                {
                    var cellValue = rows[i].ContainsKey(selectedColumns[j]) && rows[i][selectedColumns[j]] != null ? rows[i][selectedColumns[j]].ToString() : "";
                    if (selectedColumns[j].ToLower().Contains("date"))
                        text.text = FormatDateOnly(cellValue);
                    else
                        text.text = cellValue;
                }
            }

            // Скрываем лишние ячейки
            for (int j = selectedColumns.Count; j < cellPool.Count; j++)
            {
                cellPool[j].SetActive(false);
            }
        }

        // Скрываем лишние строки
        for (int i = rows.Count; i < rowPool.Count; i++)
            rowPool[i].SetActive(false);

        // Скрываем лишние пулы ячеек
        for (int i = rows.Count; i < cellPools.Count; i++)
        {
            foreach (var cell in cellPools[i])
            {
                cell.SetActive(false);
            }
        }
    }

    private string FormatDateOnly(string value)
    {
        DateTime dt;
        if (DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
            return dt.ToString("yyyy-MM-dd");
        return value;
    }

    public void ShowPartialTable(int selectedIndex, int range)
    {
        if (currentRows == null || currentRows.Count == 0) return;
        int start = selectedIndex;
        int end = Mathf.Min(currentRows.Count - 1, selectedIndex + range);
        var partialRows = currentRows.GetRange(start, end - start + 1);
        // Отключаем пагинацию временно
        if (paginationListItems != null)
            paginationListItems.enabled = false;
        UpdateTableWithData(partialRows);
    }

    public void EnablePagination(bool enable)
    {
        if (paginationListItems != null)
            paginationListItems.enabled = enable;
    }

    public List<string> GetAllColumns() => allColumns;

    public int GetSelectedRowIndex()
    {
        return selectedRowIndex;
    }
}