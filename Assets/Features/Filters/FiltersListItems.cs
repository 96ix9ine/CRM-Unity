using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;
using UnityEngine.EventSystems;
using System.Linq;
using Strategies;
using Core.TreeView.Models;
using UI.TreeView.Views;
using Newtonsoft.Json.Linq;
using TMPro;

/// <summary>
/// Компонент для управления фильтрами колонок таблицы
/// Позволяет включать/выключать отображение колонок в таблицах и иерархических деревьях
/// 
/// СВЯЗИ С ДРУГИМИ КОМПОНЕНТАМИ:
/// - TablesSection: получает ссылку через SetTreeViewProvider, вызывается SetupFilters при смене таблицы
/// - DatabaseTreeViewProvider: применяет фильтры через ApplyFilter, получает текущие фильтры через GetSelectedFilters
/// - SearchField: использует GetSelectedFilters для получения актуальных фильтров при сбросе поиска
/// - ListItems: получает события OnApplyFilters для обновления отображения обычных таблиц
/// 
/// ЛОГИКА РАБОТЫ:
/// - Для обычных таблиц: фильтрует колонки в ListItems
/// - Для иерархических таблиц: фильтрует детей корня в DatabaseTreeViewProvider
/// - При смене таблицы: сбрасывает фильтры и показывает все доступные колонки
/// </summary>
public class FiltersListItems : MonoBehaviour
{
    [Header("Обычные фильтры")]
    [SerializeField] private GameObject panelRoot;       // Корневая панель обычных фильтров
    [SerializeField] private Transform togglesParent;    // Контейнер тогглов
    [SerializeField] private GameObject togglePrefab;    // Префаб тоггла
    [SerializeField] private Button applyButton;         // Кнопка "Применить"
    [SerializeField] private Button selectAllButton;     // Кнопка "Выбрать все"

    [Header("TreeView Filters")]
    [SerializeField] private GameObject filtersTreeViewPanel; // Панель фильтров для TreeView
    [SerializeField] private Transform dropdownsParent;       // Контейнер для блоков фильтров (Text+Dropdown)
    [SerializeField] private GameObject dropdownPrefab;       // Префаб Dropdown (если используется отдельно)
    [SerializeField] private GameObject dropdownBlockPrefab;  // Префаб блока (Text+Dropdown), если есть
    [SerializeField] private Button filtersClearAllButton;    // Кнопка "Очистить всё"
    [SerializeField] private Button filtersApplyButton;       // Кнопка "Применить" для TreeView
    [SerializeField] private DatabaseTreeView databaseTreeView; // Ссылка на компонент дерева
    private ITreeNode fullTreeRoot; // Полное дерево
    [Header("Связанные секции")]
    [SerializeField] private ListItems listItemsSection;      // Секция данных для получения rows

    public GameObject FiltersTreeViewPanel
    {
        get => filtersTreeViewPanel;
        set => filtersTreeViewPanel = value;
    }

    /// <summary>
    /// Событие применения фильтров
    /// Вызывается при нажатии кнопки "Применить"
    /// Передает список выбранных колонок для фильтрации
    /// </summary>
    public event Action<List<string>> OnApplyFilters;

    private List<Toggle> toggles = new List<Toggle>();   // Список переключателей
    private List<string> columns = new List<string>();   // Список названий колонок
    private UI.TreeView.Views.DatabaseTreeView treeView; // Ссылка на TreeView для иерархических таблиц
    private string currentTableName;                     // Текущая таблица

    private List<TMP_Dropdown> activeDropdowns = new List<TMP_Dropdown>();
    private string currentTreeTableName;
    private List<Dictionary<string, object>> currentRows;

    private const string NoFilterOption = "Без фильтров";

    /// <summary>
    /// Инициализация компонента
    /// Настраивает обработчики событий для кнопок
    /// </summary>
    private void Awake()
    {
        if (applyButton != null)
            applyButton.onClick.AddListener(OnApplyClicked);
        if (selectAllButton != null)
            selectAllButton.onClick.AddListener(SelectAllToggles);
        if (panelRoot != null)
            panelRoot.SetActive(false); // По умолчанию скрыто
        if (filtersTreeViewPanel != null)
            filtersTreeViewPanel.SetActive(false);
        if (filtersClearAllButton != null)
            filtersClearAllButton.onClick.AddListener(ClearAllTreeViewFilters);
        if (filtersApplyButton != null)
            filtersApplyButton.onClick.AddListener(OnApplyTreeViewFilters);
    }

    /// <summary>
    /// Настраивает фильтры для указанных колонок
    /// Создает переключатели для каждой колонки с локализованными названиями
    /// 
    /// ПАРАМЕТРЫ:
    /// - tableName: название таблицы для локализации
    /// - rows: список строк данных (generateData)
    /// - selectedFilters: предустановленные выбранные фильтры (null = все выбраны)
    /// 
    /// ЛОГИКА:
    /// - Для обычных таблиц: использует ключи из rows
    /// - Создает переключатели с названиями полей
    /// </summary>
    public async void SetupFilters(string tableName, List<Dictionary<string, object>> rows, List<string> selectedFilters = null)
    {
        currentTableName = tableName;
        // Динамически определяем все уникальные ключи (колонки)
        var keysSet = new HashSet<string>();
        if (rows != null && rows.Count > 0)
        {
            foreach (var row in rows)
                foreach (var key in row.Keys)
                    keysSet.Add(key);
        }
        columns = keysSet.ToList();
        if (selectedFilters == null)
            selectedFilters = columns;
        // Удаляем все старые тогглы (кроме префаба, если он в иерархии)
        var toDelete = new List<GameObject>();
        foreach (Transform child in togglesParent)
        {
            if (child.gameObject != togglePrefab)
                toDelete.Add(child.gameObject);
        }
        foreach (var go in toDelete)
            Destroy(go);
        toggles.Clear();
        // Создаем новые тогглы
        for (int i = 0; i < columns.Count; i++)
        {
            var toggleObj = Instantiate(togglePrefab, togglesParent);
            toggleObj.SetActive(true);
            var toggle = toggleObj.GetComponent<Toggle>();
            var label = toggleObj.GetComponentInChildren<Text>();
            if (label != null)
            {
                var displayName = await DatabaseLocalizationService.Instance.GetFieldDisplayNameAsync(currentTableName, columns[i]);
                label.text = displayName ?? columns[i];
            }
            toggle.isOn = selectedFilters.Contains(columns[i]);
            toggles.Add(toggle);
        }
        // Скрываем эталонный тоггл (если он есть в иерархии)
        if (togglePrefab.transform.parent == togglesParent)
            togglePrefab.SetActive(false);
    }

    /// <summary>
    /// Настроить фильтры для TreeViewPanel по стратегии
    /// </summary>
    public void SetupTreeViewFilters(string tableName, List<Dictionary<string, object>> rows = null)
    {
        currentTreeTableName = tableName;
        // Получаем оригинальное дерево из DatabaseTreeViewProvider
        JObject treeData = null;
        if (treeView != null)
        {
            var provider = treeView.GetComponent<Core.Database.Providers.DatabaseTreeViewProvider>();
            if (provider != null && provider.originalHierarchyData != null)
                treeData = provider.originalHierarchyData;
        }
        // Debug: выводим дерево
        // Debug.Log($"treeData: {treeData}");
        // Эталонный блок (Text+Dropdown) — первый ребёнок, всегда неактивен!
        if (dropdownsParent.childCount == 0) return;
        var template = dropdownsParent.GetChild(0).gameObject;
        template.SetActive(false);
        // Скрываем все существующие блоки, кроме эталона
        for (int i = 1; i < dropdownsParent.childCount; i++)
            dropdownsParent.GetChild(i).gameObject.SetActive(false);
        var strategy = TableFilterStrategyFactory.GetStrategy(tableName);
        if (strategy == null) return;
        var fields = strategy.GetAvailableFilters();
        // Debug.Log($"fields: {string.Join(", ", fields)}");
        activeDropdowns.Clear();
        for (int i = 0; i < fields.Count; i++)
        {
            GameObject block;
            if (i + 1 < dropdownsParent.childCount)
            {
                block = dropdownsParent.GetChild(i + 1).gameObject;
            }
            else
            {
                block = Instantiate(template, dropdownsParent);
            }
            block.SetActive(true);
            // Найти Text и Dropdown внутри блока
            var label = block.GetComponentInChildren<Text>();
            var dropdown = block.GetComponentInChildren<TMP_Dropdown>();
            if (label != null) label.text = fields[i];
            // Собрать значения из дерева
            var values = new HashSet<string>();
            if (treeData != null)
                CollectValuesFromTree(treeData, fields[i], values);
            // Debug.Log($"{fields[i]}: {string.Join(", ", values)}");
            var options = new List<string> { NoFilterOption };
            options.AddRange(values);
            if (dropdown != null)
            {
                dropdown.ClearOptions();
                dropdown.AddOptions(options);
                dropdown.value = 0;
                if (!activeDropdowns.Contains(dropdown))
                    activeDropdowns.Add(dropdown);
            }
        }
        // Если полей меньше, чем было блоков — скрыть лишние
        for (int i = fields.Count + 1; i < dropdownsParent.childCount; i++)
            dropdownsParent.GetChild(i).gameObject.SetActive(false);
    }

    // Рекурсивно собирает значения по ключу из дерева
    private void CollectValuesFromTree(JToken node, string key, HashSet<string> values)
    {
        if (node == null) return;
        if (node.Type == JTokenType.Object)
        {
            var obj = (JObject)node;
            if (obj.ContainsKey(key) && obj[key] != null)
                values.Add(obj[key].ToString());
            if (obj.ContainsKey("children") && obj["children"] is JArray arr)
            {
                foreach (var child in arr)
                    CollectValuesFromTree(child, key, values);
            }
        }
        else if (node.Type == JTokenType.Array)
        {
            foreach (var child in node)
                CollectValuesFromTree(child, key, values);
        }
    }

    /// <summary>
    /// Сбросить все фильтры (установить "Без фильтров" во все Dropdown'ы)
    /// </summary>
    public void ClearAllTreeViewFilters()
    {
        foreach (var dropdown in activeDropdowns)
        {
            dropdown.value = 0;
        }
    }

    /// <summary>
    /// Получить выбранные фильтры из Dropdown'ов (null если "Без фильтров")
    /// </summary>
    public Dictionary<string, string> GetSelectedTreeViewFilters()
    {
        var result = new Dictionary<string, string>();
        var strategy = TableFilterStrategyFactory.GetStrategy(currentTreeTableName);
        if (strategy == null) return result;
        var fields = strategy.GetAvailableFilters();
        for (int i = 0; i < fields.Count; i++)
        {
            var dropdown = activeDropdowns[i];
            var value = dropdown.options[dropdown.value].text;
            if (value != NoFilterOption)
                result[fields[i]] = value;
        }
        return result;
    }

    /// <summary>
    /// Показывает панель фильтров
    /// Вызывается из TablesSection при нажатии кнопки фильтров
    /// </summary>
    public void ShowPanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);
    }

    /// <summary>
    /// Скрывает панель фильтров
    /// Вызывается автоматически после применения фильтров
    /// </summary>
    public void HidePanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    /// <summary>
    /// Переключает видимость панели фильтров
    /// Вызывается из TablesSection при нажатии кнопки фильтров
    /// </summary>
    public void TogglePanel()
    {
        if (panelRoot != null)
            panelRoot.SetActive(!panelRoot.activeSelf);
    }

    /// <summary>
    /// Выбирает или снимает выбор со всех переключателей
    /// Если все выбраны - снимает выбор, иначе выбирает все
    /// Вызывается при нажатии кнопки "Выбрать все"
    /// </summary>
    public void SelectAllToggles()
    {
        bool allOn = toggles.Count > 0 && toggles.TrueForAll(t => t.isOn);
        foreach (var t in toggles)
            t.isOn = !allOn;
    }

    /// <summary>
    /// Обработчик нажатия кнопки "Применить"
    /// Собирает выбранные колонки и вызывает событие OnApplyFilters
    /// 
    /// ЛОГИКА:
    /// - Для иерархических таблиц: вызывает provider.ApplyFilter
    /// - Для обычных таблиц: вызывает событие OnApplyFilters
    /// - Автоматически скрывает панель после применения
    /// </summary>
    public void OnApplyClicked()
    {
        var selected = new List<string>();
        for (int i = 0; i < toggles.Count; i++)
        {
            if (toggles[i].isOn)
                selected.Add(columns[i]);
        }
        if (treeView != null)
        {
            var provider = treeView.GetComponent<Core.Database.Providers.DatabaseTreeViewProvider>();
            if (provider != null)
            {
                // Сначала сбрасываем поле поиска и обновляем ссылку на актуальный treeView
                var searchField = treeView.GetComponentInChildren<SearchField>();
                if (searchField != null)
                    searchField.SetTreeViewProvider(treeView);
                provider.ApplyFilter(selected);
                provider.ApplySearch(""); // Сразу сбрасываем поиск
            }
        }
        else
        {
            OnApplyFilters?.Invoke(selected);
        }
        // Сбросить поле поиска
        var searchFieldGlobal = treeView != null ? treeView.GetComponentInChildren<SearchField>() : null;
        if (searchFieldGlobal != null)
            searchFieldGlobal.ClearSearchField();
        HidePanel();
    }

    /// <summary>
    /// Применить фильтры TreeView (вызывается по кнопке "Принять")
    /// </summary>
    public void OnApplyTreeViewFilters()
    {
        // Debug.LogError("[TreeView] OnApplyTreeViewFilters called");
        var filters = GetSelectedTreeViewFilters();
        var provider = treeView != null ? treeView.GetComponent<Core.Database.Providers.DatabaseTreeViewProvider>() : null;
        if (provider != null)
        {
            // Преобразуем filters (Dictionary<string, string>) в List<string> для ApplyFilter
            var filterValues = filters.Values.ToList();
            // Сначала сбрасываем поле поиска и обновляем ссылку на актуальный treeView
            var searchField = treeView.GetComponentInChildren<SearchField>();
            if (searchField != null)
                searchField.SetTreeViewProvider(treeView);
            provider.ApplyFilter(filterValues);
            provider.ApplySearch(""); // Сразу сбрасываем поиск
        }
        // Сбросить поле поиска
        var searchFieldGlobal = treeView != null ? treeView.GetComponentInChildren<SearchField>() : null;
        if (searchFieldGlobal != null)
            searchFieldGlobal.ClearSearchField();
        if (filtersTreeViewPanel != null)
            filtersTreeViewPanel.SetActive(false);
    }

    /// <summary>
    /// Устанавливает ссылку на TreeView для работы с иерархическими таблицами
    /// Вызывается из TablesSection при смене таблицы
    /// 
    /// ПАРАМЕТРЫ:
    /// - treeView: ссылка на DatabaseTreeView или null для обычных таблиц
    /// 
    /// ЛОГИКА:
    /// - Подписывается на события OnDataLoaded провайдера
    /// - При загрузке данных обновляет фильтры с учетом текущих выбранных значений
    /// </summary>
    public void SetTreeViewProvider(UI.TreeView.Views.DatabaseTreeView treeView)
    {
        this.treeView = treeView;
        var provider = treeView != null ? treeView.GetComponent<Core.Database.Providers.DatabaseTreeViewProvider>() : null;
        if (provider != null)
        {
            provider.OnDataLoaded -= OnProviderDataLoaded; // чтобы не было двойной подписки
            provider.OnDataLoaded += OnProviderDataLoaded;
            // При первой загрузке дерева сразу инициализируем fullTreeRoot
            fullTreeRoot = provider.Root;
        }
    }

    /// <summary>
    /// Обработчик события загрузки данных провайдера
    /// Обновляет фильтры при смене таблицы или перезагрузке данных
    /// 
    /// ЛОГИКА:
    /// - Для иерархических таблиц: использует текущие выбранные фильтры из UI
    /// - Для обычных таблиц: использует фильтры из провайдера
    /// - Предотвращает циклические вызовы при смене таблицы
    /// </summary>
    private void OnProviderDataLoaded()
    {
        var provider = treeView != null ? treeView.GetComponent<Core.Database.Providers.DatabaseTreeViewProvider>() : null;
        if (provider == null) return;
        fullTreeRoot = provider.Root;
        var tableName = currentTableName;
        var rows = provider.GetCurrentRows();
        SetupFilters(tableName, rows, provider.GetCurrentHierarchyFilter());
    }

    /// <summary>
    /// Возвращает список выбранных фильтров
    /// Используется в SearchField и DatabaseTreeViewProvider для получения актуальных фильтров
    /// 
    /// ВОЗВРАЩАЕТ:
    /// - Список названий выбранных колонок
    /// - Пустой список, если ничего не выбрано
    /// </summary>
    public List<string> GetSelectedFilters()
    {
        var selected = new List<string>();
        for (int i = 0; i < toggles.Count; i++)
        {
            if (toggles[i].isOn)
                selected.Add(columns[i]);
        }
        return selected;
    }

    public void SetFullTreeRoot(ITreeNode root)
    {
        fullTreeRoot = root;
    }
    private ITreeNode FilterTreeRecursive(ITreeNode node, Dictionary<string, string> filters)
    {
        if (node == null) return null;
        bool matches = true;
        if (filters.Count > 0 && node.Data is Dictionary<string, object> dataDict)
        {
            foreach (var filter in filters)
            {
                if (!dataDict.ContainsKey(filter.Key) || dataDict[filter.Key]?.ToString() != filter.Value)
                {
                    matches = false;
                    break;
                }
            }
        }
        var filteredChildren = new List<ITreeNode>();
        foreach (var child in node.Children)
        {
            var filteredChild = FilterTreeRecursive(child, filters);
            if (filteredChild != null)
                filteredChildren.Add(filteredChild);
        }
        if (matches || filteredChildren.Count > 0)
        {
            var newNode = new TreeNode(node.Id, node.Name, node.Data);
            foreach (var child in filteredChildren)
                newNode.AddChild(child);
            return newNode;
        }
        return null;
    }

    /// <summary>
    /// Показывает нужную панель фильтров в зависимости от типа таблицы
    /// </summary>
    public void ShowRelevantFiltersPanel(string tableName)
    {
        bool anyActive = (panelRoot != null && panelRoot.activeSelf) || (filtersTreeViewPanel != null && filtersTreeViewPanel.activeSelf);
        if (anyActive)
        {
            HideAllPanels();
            return;
        }
        HideAllPanels();
        var strategy = Strategies.TableFilterStrategyFactory.GetStrategy(tableName);
        var treeViewTables = new HashSet<string> { "bills_for_pay", "arrival_invoices", "components", "current_tasks", "employees", "organizations", "pcb_orders", "pcbs" };
        if (strategy != null && filtersTreeViewPanel != null && treeViewTables.Contains(tableName))
        {
            if (listItemsSection != null)
                SetupTreeViewFilters(tableName, listItemsSection.CurrentRows);
            filtersTreeViewPanel.SetActive(true);
            return;
        }
        if (panelRoot != null)
            panelRoot.SetActive(true);
    }

    /// <summary>
    /// Скрывает обе панели фильтров
    /// </summary>
    public void HideAllPanels()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
        if (filtersTreeViewPanel != null) filtersTreeViewPanel.SetActive(false);
    }
}