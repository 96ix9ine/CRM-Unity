using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine.Networking;
using UI.TreeView.Views;
using System.Threading.Tasks;

/// <summary>
/// Класс для хранения данных о таблице
/// Используется для передачи информации о выбранной таблице между компонентами
/// </summary>
[System.Serializable]
public class TableData
{
    public string tableName;
}

/// <summary>
/// Компонент для отображения списка таблиц
/// Управляет выбором таблицы и обновлением связанных компонентов
/// 
/// СВЯЗИ С ДРУГИМИ КОМПОНЕНТАМИ:
/// - FieldsSection: показывает поля выбранной таблицы через ShowTableFields
/// - ListItems: показывает данные таблицы через ShowTableData
/// - DatabaseTreeView: управляет отображением иерархических таблиц
/// - FiltersListItems: настраивает фильтры для выбранной таблицы
/// - SearchField: очищает поиск при смене таблицы
/// - ActionsButtonsMainMenu: передает информацию о выбранной таблице
/// 
/// ЛОГИКА РАБОТЫ:
/// - Загружает список доступных таблиц с сервера
/// - Фильтрует таблицы по роли пользователя
/// - При выборе таблицы обновляет все связанные компоненты
/// - Переключает между обычными и иерархическими таблицами
/// </summary>
public class MenuButtons : MonoBehaviour
{
    [Header("Основные компоненты")]
    [SerializeField] private Transform contentParent;              // Контейнер для кнопок таблиц
    [SerializeField] private GameObject tableButtonPrefab;         // Префаб кнопки таблицы
    [SerializeField] private FieldsSection fieldsSection;          // Компонент для отображения полей
    [SerializeField] private ListItems listItemsSection;           // Компонент для отображения данных
    [SerializeField] private ActionsButtonsMainMenu actionsButtonsMainMenu; // Компонент с кнопками действий
    [Header("Иконки")]
    [SerializeField] private Image tablesSectionIcon;              // Иконка секции таблиц
    [SerializeField] private Sprite defaultSprite;                 // Спрайт по умолчанию (Background)
    [SerializeField] private Sprite selectedSprite;                // Спрайт при выборе (Check_0)
    [Header("Фильтры")]
    [SerializeField] private Button filtersButton;                 // Кнопка открытия фильтров
    [SerializeField] private FiltersListItems filtersListItems;    // Компонент фильтров
    [SerializeField] private SearchField searchField;             // Компонент поиска
    [SerializeField] private GameObject filtersTreeViewPanel;     // Панель фильтров для TreeView
    [Header("TreeView")]
    [SerializeField] private DatabaseTreeView databaseTreeView; // Ссылка на компонент дерева данных
    [SerializeField] private GameObject ListItemsPanelGeneral;  // Панель для отображения данных
    [SerializeField] private GameObject ListItemsTreeViewPanel; // Панель для отображения дерева

    [SerializeField] public TableApiService tableApiService;
    
    // Динамические данные о сущностях и их таблицах
    private Dictionary<string, HashSet<string>> dynamicEntityToTables = new Dictionary<string, HashSet<string>>();
    
    public string CurrentTableName { get; private set; }
    /// <summary>
    /// Список таблиц, поддерживающих TreeView (иерархическое отображение)
    /// Определяет, какие таблицы будут отображаться в виде дерева
    /// </summary>
    private HashSet<string> allowedTreeViewTables = new HashSet<string> { "employees", "organizations", "pcbs", "pcb_orders", "components", "current_tasks", "current_task_states", "bills_for_pay", "arrival_invoices", "inventarization", "license", "stand_tasks", "shipments", "order_requests", "stands", "peoples", "component_placements" };

    /// <summary>
    /// Инициализация компонента
    /// Настраивает обработчики событий и загружает дерево сущностей
    /// </summary>
    IEnumerator Start()
    {
        // Инициализируем сервис локализации
        var _ = DatabaseLocalizationService.Instance;
        if (filtersButton != null && filtersListItems != null)
            filtersButton.onClick.AddListener(() => filtersListItems.ShowRelevantFiltersPanel(CurrentTableName));
        if (filtersListItems != null && listItemsSection != null)
            filtersListItems.OnApplyFilters += listItemsSection.SetSelectedColumns;
        if (filtersListItems != null && filtersTreeViewPanel != null)
            filtersListItems.FiltersTreeViewPanel = filtersTreeViewPanel;
        
        if (tableApiService == null) tableApiService = FindObjectOfType<TableApiService>();
        if (tableApiService == null)
        {
            Debug.LogError("MenuButtons: TableApiService не найден на сцене");
            yield break;
        }
        
    }

    /// <summary>
    /// Публичный метод для загрузки конкретной таблицы на другой сцене
    /// Используется для программного выбора таблицы (например, для лицензий)
    /// 
    /// ПАРАМЕТРЫ:
    /// - tableName: название таблицы для загрузки
    /// </summary>
    public void SelectTable(string tableName)
    {
        OnTableSelected(tableName);
    }

    /// <summary>
    /// Сброс иконки секции таблиц на стандартную
    /// Вызывается при необходимости сброса визуального состояния
    /// </summary>
    public void ResetTableSectionIcon()
    {
        if (tablesSectionIcon != null && defaultSprite != null)
            tablesSectionIcon.sprite = defaultSprite;
    }

    /// <summary>
    /// Обработчик выбора таблицы
    /// Обновляет все связанные компоненты при выборе новой таблицы
    /// 
    /// ПАРАМЕТРЫ:
    /// - tableName: название выбранной таблицы
    /// 
    /// ЛОГИКА:
    /// - Очищаем поле поиска при смене таблицы
    /// - Показываем поля выбранной таблицы
    /// - Определяем тип таблицы (обычная или иерархическая)
    /// - Переключаем панели отображения (обычная таблица или TreeView)
    /// - Настраиваем компоненты в зависимости от типа таблицы
    /// </summary>
    private void OnTableSelected(string tableName, bool fromEntityTree = false)
    {
        CurrentTableName = tableName;
        // Очищаем поле поиска при смене таблицы
        if (searchField != null)
            searchField.ClearSearchField();
        // Скрываем все панели фильтров при смене таблицы
        if (filtersListItems != null)
            filtersListItems.HideAllPanels();
        fieldsSection.ShowTableFields(tableName);
        
        // Проверяем, является ли это сущностью (из EntityTreeView)
        bool isEntity = dynamicEntityToTables.ContainsKey(tableName);
        // Для TreeView используем таблицы из allowedTreeViewTables
        // Эти таблицы должны отображаться как иерархические данные
        // Сущности ДОЛЖНЫ отображаться как TreeView
        bool isTreeView = allowedTreeViewTables.Contains(tableName);
        
        if (isTreeView)
        {
            if (ListItemsTreeViewPanel != null) ListItemsTreeViewPanel.SetActive(true);
            if (ListItemsPanelGeneral != null) ListItemsPanelGeneral.SetActive(false);
            if (databaseTreeView != null)
            {
                databaseTreeView.SetAllowedTablesForRole(allowedTreeViewTables);
                databaseTreeView.ShowTableTree(tableName);
            }
            // Передаем TreeView в ActionsButtonsMainMenu
            actionsButtonsMainMenu.SetTreeViewMode(databaseTreeView, true);
            // Передаем TreeViewProvider в фильтры и поиск
            filtersListItems.SetTreeViewProvider(databaseTreeView);
            searchField.SetTreeViewProvider(databaseTreeView);
        }
        else
        {
            if (ListItemsTreeViewPanel != null) ListItemsTreeViewPanel.SetActive(false);
            if (ListItemsPanelGeneral != null) ListItemsPanelGeneral.SetActive(true);
            if (databaseTreeView != null)
                databaseTreeView.Clear();
            actionsButtonsMainMenu.SetTreeViewMode(null, false);
            filtersListItems.SetTreeViewProvider(null);
            searchField.SetTreeViewProvider(null);
        }
        StartCoroutine(listItemsSection.ShowTableData(tableName));
        actionsButtonsMainMenu.SetTable(tableName);
        
        // Настраиваем фильтры
        if (filtersListItems != null)
        {
            if (isTreeView)
            {
                filtersListItems.SetupTreeViewFilters(tableName);
            }
            else
            {
                filtersListItems.SetupFilters(tableName, listItemsSection.CurrentRows);
            }
        }
    }
}