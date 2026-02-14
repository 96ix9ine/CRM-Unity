using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Компонент для поиска по данным таблицы
/// Реализует поиск с задержкой для оптимизации производительности
/// Поддерживает сброс поиска по Enter
/// 
/// СВЯЗИ С ДРУГИМИ КОМПОНЕНТАМИ:
/// - TablesSection: получает ссылку через SetTreeViewProvider, вызывается ClearSearchField при смене таблицы
/// - DatabaseTreeViewProvider: выполняет поиск через ApplySearch для иерархических таблиц
/// - ListItems: обновляет данные через UpdateTableWithData для обычных таблиц
/// - PaginationListItems: обновляет отфильтрованные данные через SetFilteredRows
/// 
/// ЛОГИКА РАБОТЫ:
/// - Для иерархических таблиц: делегирует поиск DatabaseTreeViewProvider
/// - Для обычных таблиц: фильтрует данные локально и обновляет ListItems
/// - При сбросе поиска: восстанавливает исходные данные с учетом фильтров
/// </summary>
public class SearchField : MonoBehaviour, ISearchable
{
    [SerializeField] private InputField searchInput;    // Поле ввода для поиска
    [SerializeField] private ListItems listItems;       // Ссылка на компонент с данными таблицы
    [SerializeField] private float searchDelay = 0.05f; // Задержка между поисками в секундах

    private string currentQuery = "";                   // Текущий поисковый запрос
    private float lastSearchTime;                       // Время последнего поиска
    private List<Dictionary<string, object>> originalData; // Оригинальные данные таблицы
    private List<Dictionary<string, object>> filteredData; // Отфильтрованные данные
    private bool isSearching = false;                   // Флаг для предотвращения рекурсивных вызовов
    private UI.TreeView.Views.DatabaseTreeView treeView; // Ссылка на TreeView для иерархических таблиц
    private bool searchCleared = false;                 // Флаг для предотвращения двойного сброса поиска

    /// <summary>
    /// Инициализация компонента
    /// Настраивает обработчики событий для поля ввода
    /// </summary>
    void Start()
    {
        if (searchInput == null)
            searchInput = GetComponent<InputField>();
            
        searchInput.onValueChanged.AddListener(OnSearchInputChanged);
        searchInput.onEndEdit.AddListener(OnSearchEndEdit);
    }

    /// <summary>
    /// Обработчик окончания редактирования поля поиска
    /// Сбрасывает поиск при нажатии Enter
    /// 
    /// ЛОГИКА:
    /// - При нажатии Enter очищает поле поиска и сбрасывает поиск
    /// - Использует флаг searchCleared для предотвращения двойного сброса
    /// - Активирует поле ввода для продолжения работы
    /// </summary>
    private void OnSearchEndEdit(string value)
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            if (!searchCleared)
            {
                ClearSearch();
                searchCleared = true;
            }
            searchInput.text = "";
            searchInput.ActivateInputField();
        }
        else
        {
            searchCleared = false;
        }
    }

    /// <summary>
    /// Обработчик изменения текста в поле поиска
    /// Реализует поиск с задержкой для оптимизации
    /// 
    /// ЛОГИКА:
    /// - Использует задержку searchDelay для предотвращения частых вызовов
    /// - Сбрасывает флаг searchCleared при изменении текста
    /// - Предотвращает рекурсивные вызовы через флаг isSearching
    /// </summary>
    private void OnSearchInputChanged(string value)
    {
        if (isSearching) return;
        searchCleared = false;
        currentQuery = value;
        if (Time.time - lastSearchTime >= searchDelay)
        {
            isSearching = true;
            Search(value);
            lastSearchTime = Time.time;
            isSearching = false;
        }
    }

    /// <summary>
    /// Выполняет поиск по всем полям таблицы
    /// 
    /// ПАРАМЕТРЫ:
    /// - query: поисковый запрос (пустая строка = сброс поиска)
    /// 
    /// ЛОГИКА:
    /// - Для иерархических таблиц: делегирует поиск DatabaseTreeViewProvider
    /// - Для обычных таблиц: фильтрует данные локально по всем полям
    /// - При пустом запросе: сбрасывает поиск и восстанавливает исходные данные
    /// </summary>
    public void Search(string query)
    {
        if (treeView != null)
        {
            var provider = treeView.GetComponent<Core.Database.Providers.DatabaseTreeViewProvider>();
            if (provider != null)
            {
                // Для иерархических таблиц при пустом запросе просто вызываем ApplySearch
                // Логика сброса поиска и применения фильтров теперь полностью в провайдере
                if (provider.IsHierarchicalTable(provider.LastTableName) && string.IsNullOrEmpty(query))
                {
                    searchInput.text = "";
                    provider.ApplySearch("");
                    return;
                }
                provider.ApplySearch(query);
                return;
            }
        }
        
        if (originalData == null)
            return; // Нет данных для поиска
            
        if (string.IsNullOrEmpty(query))
        {
            ClearSearch();
            return;
        }
        
        query = query.ToLower();
        filteredData = originalData.Where(row =>
            row.Values.Any(value =>
                value?.ToString().ToLower().Contains(query) ?? false
            )
        ).ToList();
        // --- Фикс: для обычных таблиц всегда обновляем пагинацию через SetAllRows ---
        if (listItems != null && listItems.paginationListItems != null && listItems.paginationListItems.isActiveAndEnabled)
        {
            listItems.paginationListItems.SetAllRows(filteredData);
        }
        else
        {
            UpdateTableData();
        }
    }

    /// <summary>
    /// Сбрасывает поиск и возвращает все данные
    /// 
    /// ЛОГИКА:
    /// - Для иерархических таблиц: вызывает provider.ApplySearch("")
    /// - Для обычных таблиц: восстанавливает originalData и обновляет отображение
    /// - Восстанавливает исходное состояние с учетом активных фильтров
    /// </summary>
    public void ClearSearch()
    {
        if (treeView != null)
        {
            var provider = treeView.GetComponent<Core.Database.Providers.DatabaseTreeViewProvider>();
            if (provider != null)
            {
                provider.ApplySearch("");
                return;
            }
        }
        if (originalData == null)
            return;
        filteredData = new List<Dictionary<string, object>>(originalData);
        // --- Фикс: для обычных таблиц всегда обновляем пагинацию через SetAllRows ---
        if (listItems != null && listItems.paginationListItems != null && listItems.paginationListItems.isActiveAndEnabled)
        {
            listItems.paginationListItems.SetAllRows(filteredData);
        }
        else
        {
            UpdateTableData();
        }
    }

    /// <summary>
    /// Обновляет отображение данных в таблице
    /// 
    /// ЛОГИКА:
    /// - Если включена пагинация: обновляет PaginationListItems
    /// - Иначе: обновляет ListItems напрямую
    /// - Использует отфильтрованные данные filteredData
    /// </summary>
    private void UpdateTableData()
    {
        if (listItems != null)
        {
            if (listItems.paginationListItems != null && listItems.paginationListItems.isActiveAndEnabled)
            {
                listItems.paginationListItems.SetFilteredRows(filteredData);
            }
            else
            {
                listItems.UpdateTableWithData(filteredData);
            }
        }
    }

    /// <summary>
    /// Устанавливает оригинальные данные для поиска
    /// Вызывается из ListItems при загрузке новых данных
    /// 
    /// ПАРАМЕТРЫ:
    /// - data: исходные данные таблицы для поиска
    /// </summary>
    public void SetOriginalData(List<Dictionary<string, object>> data)
    {
        originalData = data;
        filteredData = new List<Dictionary<string, object>>(data);
    }

    /// <summary>
    /// Устанавливает ссылку на TreeView для работы с иерархическими таблицами
    /// Вызывается из TablesSection при смене таблицы
    /// 
    /// ПАРАМЕТРЫ:
    /// - treeView: ссылка на DatabaseTreeView или null для обычных таблиц
    /// </summary>
    public void SetTreeViewProvider(UI.TreeView.Views.DatabaseTreeView treeView)
    {
        this.treeView = treeView;
    }

    /// <summary>
    /// Очищает поле поиска при смене таблицы
    /// Вызывается из TablesSection.OnTableSelected для сброса состояния поиска
    /// 
    /// ЛОГИКА:
    /// - Очищает текст в поле ввода
    /// - Сбрасывает текущий запрос
    /// - Подготавливает компонент для новой таблицы
    /// </summary>
    public void ClearSearchField()
    {
        if (searchInput != null)
        {
            searchInput.text = "";
            currentQuery = "";
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
