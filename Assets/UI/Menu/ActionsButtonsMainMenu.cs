using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Networking;
using Newtonsoft.Json;
using UnityEngine.UI;
using Newtonsoft.Json.Linq;
using System.Linq;
using UI.Selection;
using TMPro;

/// <summary>
/// Компонент для управления действиями с таблицей
/// Обрабатывает добавление, редактирование, удаление и обновление записей
/// </summary>
public class ActionsButtonsMainMenu : MonoBehaviour
{
    [Header("Actions")]
    public ListItems listItems;           // Компонент для отображения данных таблицы
    public TableApiService apiService;     // Сервис для работы с API
    [SerializeField] private TableEditController editController; // Контроллер модалки
    public DeleteDialogController deleteDialogController;    // Диалог подтверждения удаления
    public ListItems innerListItems;       // Ссылка на ListItems для внутренней таблицы
    public GameObject innerPanel;          // Панель для внутренней таблицы
    public Text detailsButtonText;         // Ссылка на текст кнопки "Подробнее/Закрыть"
    public Button addButton;
    public Button editButton;
    public Button deleteButton;
    public Button refreshButton;
    public TMP_Dropdown addDropdownButton;
  
    private string currentTable;           // Текущая выбранная таблица
    private List<string> currentColumns;   // Колонки текущей таблицы
    private Dictionary<string, object> selectedRow; // Выбранная строка
    private UI.TreeView.Views.DatabaseTreeView treeView;
    private bool isTreeViewMode = false;
    private bool isInnerTableOpen = false;
    private bool isDropdownProcessing = false; // Защита от рекурсии dropdown

    // Храним текущие обработчики модалки удаления, чтобы отписываться корректно
    private System.Action<bool> _dlgConfirmHandler;
    private System.Action<bool, DeleteContext> _dlgConfirmWithCtxHandler;

    // Кеш соответствия nodeType -> tableName
    private Dictionary<string, string> nodeTypeToTable;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        listItems.OnRowSelected += OnRowSelected;
        if (innerListItems != null)
            innerListItems.OnRowSelected += OnRowSelectedInner;
        UpdateActionButtonsState();
        // Предзагрузка карты nodeType->table, чтобы кнопки корректно отображались в дереве
        StartCoroutine(PreloadNodeTypesMap());

        // Глобальный сброс выделения по клику на фон
        SelectionRegistry.RegisterDeselect(() => {
            selectedRow = null;
            GlobalSelectionState.Clear();
            UpdateActionButtonsState();
        });

        // Подписка на события модалки: любое сохранение/удаление приводит к обновлению текущего UI
        if (editController != null)
        {
            editController.OnRecordSaved += () => ReloadCurrentUI();
            editController.OnRecordDeleted += () => ReloadCurrentUI();
        }

        // Инициализация dropdown для PCBs
        if (addDropdownButton != null)
        {
            addDropdownButton.ClearOptions();
            addDropdownButton.AddOptions(new List<string> { "Выбрать действие:", "Добавить ПП", "Добавить компонент" });
            addDropdownButton.value = 0; // Устанавливаем на плейсхолдер
            addDropdownButton.onValueChanged.AddListener(OnDropdownValueChanged);
        }
    }

    /// <summary>
    /// Устанавливает текущую таблицу и загружает её колонки
    /// </summary>
    public void SetTable(string tableName)
    {
        currentTable = tableName;
        selectedRow = null;
        GlobalSelectionState.CurrentTableName = tableName;
        GlobalSelectionState.SelectedRow = null;
        StartCoroutine(apiService.GetColumns(tableName, (cols) => {
            currentColumns = cols;
            if (isTreeViewMode && treeView != null)
            {
                treeView.ShowTableTree(tableName);
                // Для дерева подгружаем карту nodeType->table
                StartCoroutine(PreloadNodeTypesMap());
            }
            else
            {
                StartCoroutine(listItems.ShowTableData(tableName));
            }
            UpdateActionButtonsState();

            // Активируем кнопки задач если это current_tasks
            if (tableName == "current_tasks")
            {
                var taskManager = FindObjectOfType<TaskButtonsManager>();
                if (taskManager != null)
                {
                    taskManager.OnTableChanged(tableName);
                }
            }
        }));
    }

    /// <summary>
    /// Обработчик выбора строки в таблице
    /// </summary>
    private void OnRowSelected(Dictionary<string, object> row)
    {
        DeselectAllRows(listItems.CurrentRows);
        selectedRow = row;
        GlobalSelectionState.SelectedRow = row;
        GlobalSelectionState.CurrentTableName = currentTable;
        UpdateActionButtonsState(); // обновлять состояние кнопок при выборе строки
    }

    private void DeselectAllRows(List<Dictionary<string, object>> rows)
    {
        if (rows == null) return;
        foreach (var r in rows)
        {
            if (r.ContainsKey("isSelected"))
                r["isSelected"] = false;
        }
    }

    private void OnRowSelectedInner(Dictionary<string, object> row)
    {
        selectedRow = row;
        UpdateActionButtonsState();
        // Открытие модального окна для внутренней таблицы
        string innerTableName = null;
        if (currentTable == "bills_for_pay")
            innerTableName = "bills_components";
        else if (currentTable == "arrival_invoices")
            innerTableName = "invoices_components";
            
        if (editController != null)
        {
            editController.ResetOnSaveHandlers();
            editController.OnSave += (data) => OnInnerTableSave(innerTableName, data, row);
            editController.Open(TableNameService.GetEditTitle(innerTableName), innerTableName, new List<string>(), row);
        }
        else
        {
            Debug.LogError("TableEditController не назначен для внутренней таблицы");
        }
    }

    /// <summary>
    /// Обработчик сохранения для внутренней таблицы
    /// </summary>
    private void OnInnerTableSave(string innerTableName, Dictionary<string, object> data, Dictionary<string, object> originalRow)
    {
        if (originalRow.ContainsKey("id"))
        {
            StartCoroutine(apiService.UpdateRecord(innerTableName, originalRow["id"], data, (ok) => {
                if (ok)
                {
                    // Обновляем данные внутренней таблицы
                    string url = null;
                    string id = selectedRow.ContainsKey("id") ? selectedRow["id"].ToString() : null;

                    if (currentTable == "bills_for_pay" && !string.IsNullOrEmpty(id))
                        url = $"/bills_components/generateData/{id}";
                    else if (currentTable == "arrival_invoices" && !string.IsNullOrEmpty(id))
                        url = $"/invoices_components/generateData/{id}";

                    if (url != null && id != null)
                    {
                        StartCoroutine(LoadInnerTableData(url));
                    }
                }
            }));
        }
    }

    /// <summary>
    /// Обработчик нажатия кнопки добавления
    /// Открывает модальное окно для создания новой записи
    /// </summary>
    public void OnAddButton()
    {
        if (currentColumns == null) return;

        // Попробовать кастомные стратегии
        var addStrategy = GetAddStrategy();
        if (addStrategy != null && addStrategy.HandleAdd(this)) return;

        if (editController != null)
        {
            editController.ResetOnSaveHandlers();
            editController.OnSave += OnAddSave;
            editController.Open(TableNameService.GetAddTitle(currentTable), currentTable, currentColumns, null);
        }
        else
        {
            Debug.LogError("TableEditController не назначен для добавления записи");
        }
    }

    // ------- Хелперы для стратегий -------
    public string GetCurrentTable() => currentTable;
    public Dictionary<string, object> GetEffectiveSelectedRow() => GlobalSelectionState.SelectedRow ?? selectedRow;
    public TableApiService Api() => apiService;
    public void ReloadCurrentUI()
    {
        if (isTreeViewMode && treeView != null) treeView.ShowTableTree(currentTable);
        else StartCoroutine(listItems.ShowTableData(currentTable));
    }
    public void OpenAddModal(string tableName, System.Action<Dictionary<string, object>> onSave)
    {
        // Сброс обработчиков и подписка на сохранение
        if (editController != null) editController.ResetOnSaveHandlers();
        if (editController != null) editController.OnSave += onSave;
        StartCoroutine(apiService.GetColumns(tableName, (cols) => {
            if (editController != null)
                editController.Open(TableNameService.GetAddTitle(tableName), tableName, cols, null);
            else
                Debug.LogError("TableEditController не назначен для открытия модалки добавления");
        }));
    }

    public void OpenAddModalWithData(string tableName, Dictionary<string, object> initialData, System.Action<Dictionary<string, object>> onSave)
    {
        if (editController != null) editController.ResetOnSaveHandlers();
        if (editController != null) editController.OnSave += onSave;
        StartCoroutine(apiService.GetColumns(tableName, (cols) => {
            if (editController != null)
                editController.Open(TableNameService.GetAddTitle(tableName), tableName, cols, initialData);
            else
                Debug.LogError("TableEditController не назначен для открытия модалки добавления с данными");
        }));
    }

    /// <summary>
    /// Открывает модальное окно с кастомной стратегией
    /// </summary>
    /// <param name="strategy">Стратегия модального окна</param>
    /// <param name="isEdit">Режим редактирования</param>
    /// <param name="data">Данные для редактирования</param>
    public void OpenModalWithStrategy(IModalStrategy strategy, bool isEdit = false, Dictionary<string, object> data = null)
    {
        if (editController != null)
        {
            editController.ResetOnSaveHandlers();
            editController.OpenWithStrategy(strategy, isEdit, data);
        }
        else
        {
            Debug.LogError("TableEditController не назначен для открытия модалки со стратегией");
        }
    }

    /// <summary>
    /// Открывает модальное окно для редактирования записи
    /// </summary>
    /// <param name="tableName">Название таблицы</param>
    /// <param name="data">Данные для редактирования</param>
    /// <param name="onSave">Callback при сохранении</param>
    public void OpenEditModal(string tableName, Dictionary<string, object> data, System.Action<Dictionary<string, object>> onSave)
    {
        if (editController != null) editController.ResetOnSaveHandlers();
        if (editController != null) editController.OnSave += onSave;
        StartCoroutine(apiService.GetColumns(tableName, (cols) => {
            if (editController != null)
                editController.Open(TableNameService.GetEditTitle(tableName), tableName, cols, data);
            else
                Debug.LogError("TableEditController не назначен для открытия модалки редактирования");
        }));
    }

    /// <summary>
    /// Обработчик сохранения новой записи
    /// </summary>
    private void OnAddSave(Dictionary<string, object> data)
    {
        StartCoroutine(apiService.AddRecord(currentTable, data, (ok) => {
            if (isTreeViewMode && treeView != null)
            {
                treeView.ShowTableTree(currentTable);
            }
            else if (ok)
            {
                StartCoroutine(listItems.ShowTableData(currentTable));
            }
        }));
    }

    /// <summary>
    /// Обработчик нажатия кнопки редактирования
    /// Открывает модальное окно для редактирования выбранной записи
    /// </summary>
    public void OnEditButton()
    {
        var effectiveRow = GlobalSelectionState.SelectedRow ?? selectedRow;
        if (effectiveRow == null) return;
        string nodeType = effectiveRow.ContainsKey("nodeType") ? effectiveRow["nodeType"].ToString() : currentTable;
        
        // Сначала даём шанс add-стратегии обработать редактирование (роутинг по дочерним узлам и т.п.)
        var flowStrategy = GetAddStrategy();
        if (flowStrategy != null && flowStrategy.HandleEdit(this, effectiveRow))
            return;
        
        // Проверяем, есть ли кастомная стратегия для редактирования
        var strategy = GetEditStrategy(currentTable);
        if (strategy != null)
        {
            // Используем кастомную стратегию
            if (isTreeViewMode && treeView != null && effectiveRow.ContainsKey("id"))
            {
                // Получаем полную строку данных по id
                StartCoroutine(apiService.GetRowById(currentTable, effectiveRow["id"].ToString(), (fullRow) => {
                    if (fullRow != null)
                    {
                        OpenModalWithStrategy(strategy, true, fullRow);
                    }
                }));
            }
            else
            {
                OpenModalWithStrategy(strategy, true, effectiveRow);
            }
            return;
        }
        
        // Стандартная логика редактирования
        if (isTreeViewMode && treeView != null && effectiveRow.ContainsKey("id"))
        {
            // Получаем полную строку данных по id
            StartCoroutine(apiService.GetRowById(currentTable, effectiveRow["id"].ToString(), (fullRow) => {
                if (fullRow != null)
                {
                    StartCoroutine(apiService.GetColumns(nodeType, (columns) => {
                        if (editController != null)
                        {
                            editController.ResetOnSaveHandlers();
                            editController.OnSave += OnEditSave;
                            editController.Open(TableNameService.GetEditTitle(nodeType), nodeType, columns, fullRow);
                        }
                        else
                        {
                            Debug.LogError("TableEditController не назначен для стандартного редактирования (tree)");
                        }
                    }));
                }
            }));
            return;
        }
        StartCoroutine(apiService.GetColumns(nodeType, (columns) => {
            if (editController != null)
            {
                editController.ResetOnSaveHandlers();
                editController.OnSave += OnEditSave;
                editController.Open(TableNameService.GetEditTitle(nodeType), nodeType, columns, effectiveRow);
            }
            else
            {
                Debug.LogError("TableEditController не назначен для стандартного редактирования");
            }
        }));
    }
    
    /// <summary>
    /// Получает стратегию редактирования для указанной таблицы
    /// </summary>
    private IModalStrategy GetEditStrategy(string tableName)
    {
        // Здесь можно добавить логику для определения стратегии по названию таблицы
        if (tableName == "employees")
        {
            return new EmployeeModalStrategy(this);
        }
        if (tableName == "shipments")
        {
            return new ShipmentsModalStrategy(this);
        }
        return null;
    }
    
    /// <summary>
    /// Получает стратегию добавления для текущей таблицы
    /// </summary>
    private IAddFlowStrategy GetAddStrategy()
    {
        // Явный маппинг стратегий по таблицам
        switch (currentTable)
        {
            case "employees":
                return new EmployeesAddFlow(this); // требует контекст
            case "organizations":
                return new OrganizationsAddFlow();
            case "license":
                return new LicensesAddFlow();
            case "arrival_invoices":
                return new ArrivalInvoicesAddFlow();
            case "bills_for_pay":
                return new BillsForPayAddFlow();
            case "order_requests":
                return new OrderRequestsAddFlow();
            case "components":
                return new ComponentsAddFlow();
            case "pcbs":
                return new PcbsAddFlow();
            case "stands":
                return new StandsAddFlow();
            case "shipments":
                return new ShipmentsAddFlow();
            case "component_placements":
                return new ComponentPlacementsAddFlow();
            default:
                return null;
        }
    }

    /// <summary>
    /// Обработчик сохранения изменений записи
    /// </summary>
    private void OnEditSave(Dictionary<string, object> data)
    {
        if (!selectedRow.ContainsKey("id")) return;
        StartCoroutine(apiService.UpdateRecord(currentTable, selectedRow["id"], data, (ok) => {
            if (isTreeViewMode && treeView != null)
            {
                treeView.ShowTableTree(currentTable);
            }
            else if (ok)
            {
                StartCoroutine(listItems.ShowTableData(currentTable));
            }
        }));
    }

    /// <summary>
    /// Обработчик нажатия кнопки удаления
    /// Открывает диалог подтверждения удаления
    /// </summary>
    public void OnDeleteButton()
    {
        StartCoroutine(HandleDelete());
    }

    private IEnumerator EnsureNodeTypesMapLoaded()
    {
        if (nodeTypeToTable != null) yield break;
        bool finished = false;
        yield return StartCoroutine(apiService.GetNodeTypesMap((map) => { nodeTypeToTable = map; finished = true; }));
        while (!finished) yield return null;
    }

    private IEnumerator PreloadNodeTypesMap()
    {
        yield return EnsureNodeTypesMapLoaded();
        UpdateActionButtonsState();
    }

    private IEnumerator HandleDelete()
    {
        if (selectedRow == null) yield break;

        // Обеспечим загрузку соответствий nodeType -> table
        yield return EnsureNodeTypesMapLoaded();

        // Определяем nodeType и фактическую таблицу для удаления по карте
        string nodeType = selectedRow.ContainsKey("nodeType") ? selectedRow["nodeType"].ToString() : null;

        // В режиме дерева запрещаем удаление групповых узлов (без nodeType или неподдерживаемых)
        if (isTreeViewMode)
        {
            bool hasId = selectedRow.ContainsKey("id") && selectedRow["id"] != null;
            bool isMapped = !string.IsNullOrEmpty(nodeType) && nodeTypeToTable != null && nodeTypeToTable.ContainsKey(nodeType);
            if (!hasId || !isMapped || nodeType == "FieldTreeNode")
            {
                Debug.LogWarning($"Удаление недоступно для выбранного узла. nodeType='{nodeType}', hasId={hasId}, mapped={isMapped}");
                yield break;
            }
        }

        string mapped = (!string.IsNullOrEmpty(nodeType) && nodeTypeToTable != null && nodeTypeToTable.ContainsKey(nodeType))
            ? nodeTypeToTable[nodeType]
            : null;
        string tableForDelete = !string.IsNullOrEmpty(mapped)
            ? mapped
            : (string.IsNullOrEmpty(nodeType) ? currentTable : nodeType);

        string idStr = selectedRow.ContainsKey("id") ? selectedRow["id"].ToString() : null;
        if (string.IsNullOrEmpty(idStr)) yield break;

        // Отписываем предыдущие обработчики, если были
        if (_dlgConfirmHandler != null)
        {
            deleteDialogController.OnConfirm -= _dlgConfirmHandler;
            _dlgConfirmHandler = null;
        }
        if (_dlgConfirmWithCtxHandler != null)
        {
            deleteDialogController.OnConfirmWithContext -= _dlgConfirmWithCtxHandler;
            _dlgConfirmWithCtxHandler = null;
        }

        if (isTreeViewMode && treeView != null)
        {
            bool finished = false;
            StartCoroutine(apiService.GetRowById(tableForDelete, idStr, (fullRow) => {
                var extra = fullRow ?? selectedRow;
                deleteDialogController.OpenDelete("Удалить выбранную запись?", tableForDelete, idStr, nodeType, extra);
                _dlgConfirmWithCtxHandler = (yes, ctx) => {
                    if (yes && !string.IsNullOrEmpty(ctx?.Id))
                    {
                        StartCoroutine(apiService.DeleteRecord(ctx.TableName, ctx.Id, (ok) => {
                            if (isTreeViewMode && treeView != null)
                            {
                                treeView.ShowTableTree(currentTable);
                            }
                            else if (ok)
                            {
                                StartCoroutine(listItems.ShowTableData(currentTable));
                            }
                        }));
                    }
                };
                deleteDialogController.OnConfirmWithContext += _dlgConfirmWithCtxHandler;
                finished = true;
            }));
            while (!finished) yield return null;
            yield break;
        }

        // Обычный список: используем currentTable для API, в модалке показываем nodeType в extra (уже скрыто)
        deleteDialogController.OpenDelete("Удалить выбранную запись?", tableForDelete, idStr, nodeType, selectedRow);
        _dlgConfirmWithCtxHandler = (yes, ctx) => {
            if (yes && !string.IsNullOrEmpty(ctx?.Id))
            {
                // Используем ctx.TableName для удаления из правильной таблицы
                string tableToDelete = !string.IsNullOrEmpty(ctx.TableName) ? ctx.TableName : currentTable;
                StartCoroutine(apiService.DeleteRecord(tableToDelete, ctx.Id, (ok) => {
                    if (isTreeViewMode && treeView != null)
                    {
                        treeView.ShowTableTree(currentTable);
                    }
                    else if (ok)
                    {
                        StartCoroutine(listItems.ShowTableData(currentTable));
                    }
                }));
            }
        };
        deleteDialogController.OnConfirmWithContext += _dlgConfirmWithCtxHandler;
    }

    /// <summary>
    /// Обработчик нажатия кнопки обновления
    /// Перезагружает данные таблицы
    /// </summary>
    public void OnRefreshButton()
    {
        if (!string.IsNullOrEmpty(currentTable))
        {
            if (isTreeViewMode && treeView != null)
            {
                treeView.ShowTableTree(currentTable);
            }
            else
            {
                StartCoroutine(listItems.ShowTableData(currentTable));
            }
        }
    }

    /// <summary>
    /// Обработчик dropdown опции "Добавить ПП" (печатную плату)
    /// Делегирует выполнение стратегии PcbsAddFlow
    /// </summary>
    public void OnAddPcbButton()
    {
        if (currentTable != "pcbs") return;

        var addStrategy = GetAddStrategy() as PcbsAddFlow;
        if (addStrategy != null)
        {
            addStrategy.HandleAddPcb(this);
        }
        else
        {
            Debug.LogError("Стратегия PcbsAddFlow не найдена");
        }
    }

    /// <summary>
    /// Обработчик dropdown опции "Добавить компонент"
    /// Делегирует выполнение стратегии PcbsAddFlow
    /// </summary>
    public void OnAddComponentButton()
    {
        if (currentTable != "pcbs") return;

        var addStrategy = GetAddStrategy() as PcbsAddFlow;
        if (addStrategy != null)
        {
            addStrategy.HandleAddComponent(this);
        }
        else
        {
            Debug.LogError("Стратегия PcbsAddFlow не найдена");
        }
    }

    /// <summary>
    /// Обработчик изменения значения в dropdown меню
    /// </summary>
    /// <param name="index">Индекс выбранной опции</param>
    private void OnDropdownValueChanged(int index)
    {
        if (currentTable != "pcbs") return;

        // Защита от повторных вызовов (если dropdown уже обрабатывается)
        if (isDropdownProcessing) return;
        isDropdownProcessing = true;

        try
        {
            switch (index)
            {
                case 0: // "Выбрать действие:" - плейсхолдер, ничего не делаем
                    break;
                case 1: // "Добавить ПП"
                    OnAddPcbButton();
                    break;
                case 2: // "Добавить компонент"
                    OnAddComponentButton();
                    break;
            }

            // Планируем сброс dropdown на следующий кадр, чтобы избежать рекурсии
            StartCoroutine(ResetDropdownNextFrame());
        }
        finally
        {
            isDropdownProcessing = false;
        }
    }

    private IEnumerator ResetDropdownNextFrame()
    {
        yield return null; // Ждем следующий кадр
        if (addDropdownButton != null)
        {
            addDropdownButton.value = 0; // Сбрасываем на плейсхолдер
        }
    }

    public void SetTreeViewMode(UI.TreeView.Views.DatabaseTreeView treeView, bool enabled)
    {
        this.treeView = treeView;
        isTreeViewMode = enabled;
        if (treeView != null)
        {
            treeView.OnRowSelected += OnTreeViewRowSelected;
        }
        if (enabled)
        {
            // При входе в режим дерева — предзагружаем карту
            StartCoroutine(PreloadNodeTypesMap());
        }
    }

    private void OnTreeViewRowSelected(Dictionary<string, object> row)
    {
        selectedRow = row;
        UpdateActionButtonsState();
    }

    /// <summary>
    /// Обработчик нажатия кнопки "Подробнее/Закрыть"
    /// Открывает или закрывает внутреннюю таблицу
    /// </summary>
    public void OnDetailsButton()
    {
        if (selectedRow == null) return;

        if (isInnerTableOpen)
        {
            // Полная очистка внутренней таблицы
            if (innerListItems != null)
            {
                // Очищаем все данные внутренней таблицы
                innerListItems.UpdateTableWithData(new List<Dictionary<string, object>>());
            }
            
            if (innerPanel != null)
                innerPanel.SetActive(false);
                
            // Показать основную таблицу полностью
            listItems.EnablePagination(true);
            StartCoroutine(listItems.ShowTableData(currentTable));
            isInnerTableOpen = false;
            if (detailsButtonText != null)
                detailsButtonText.text = "Подробнее";
            
            // Включаем фильтры обратно для основной таблицы
            if (listItems != null)
            {
                // Обновляем фильтры для основной таблицы
                var filtersComponent = FindObjectOfType<FiltersListItems>();
                if (filtersComponent != null && listItems.CurrentRows != null)
                {
                    filtersComponent.SetupFilters(currentTable, listItems.CurrentRows);
                }
            }
            return;
        }

        // Отключаем фильтры для внутренней таблицы
        var filtersListItems = FindObjectOfType<FiltersListItems>();
        if (filtersListItems != null)
        {
            filtersListItems.HidePanel();
        }

        // Обрезать основную таблицу: выбранная строка + 3 строки ниже
        int selectedIndex = listItems.GetSelectedRowIndex();
        listItems.EnablePagination(false);
        listItems.ShowPartialTable(selectedIndex, 0);

        // Показать внутреннюю таблицу под выбранной строкой
        if (innerPanel != null)
            innerPanel.SetActive(true);

        string url = null;
        string id = selectedRow.ContainsKey("id") ? selectedRow["id"].ToString() : null;

        if (currentTable == "bills_for_pay" && !string.IsNullOrEmpty(id))
            url = $"/bills_components/generateData/{id}";
        else if (currentTable == "arrival_invoices" && !string.IsNullOrEmpty(id))
            url = $"/invoices_components/generateData/{id}";
        // Добавьте другие сцены по необходимости

        if (url != null && id != null)
        {
            StartCoroutine(LoadInnerTableData(url));
        }

        isInnerTableOpen = true;
        if (detailsButtonText != null)
            detailsButtonText.text = "Закрыть";
    }

    private IEnumerator LoadInnerTableData(string url)
    {
        // Универсально определяем имя внутренней таблицы по currentTable
        string innerTableName = null;
        if (currentTable == "bills_for_pay")
            innerTableName = "bills_components";
        else if (currentTable == "arrival_invoices")
            innerTableName = "invoices_components";
        // Добавьте другие пары по необходимости

        if (innerPanel != null)
            innerPanel.SetActive(true); // Сначала активируем!

        if (!string.IsNullOrEmpty(innerTableName))
            yield return StartCoroutine(innerListItems.ShowTableData(innerTableName)); // Дождаться построения структуры

        using (UnityWebRequest req = UnityWebRequest.Get(apiService.apiUrl + url))
        {
            yield return req.SendWebRequest();
            if (req.result == UnityWebRequest.Result.Success)
            {
                var rows = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(req.downloadHandler.text);
                innerListItems.UpdateTableWithData(rows);
            }
            else
            {
                Debug.LogError("Ошибка загрузки данных: " + req.error);
            }
        }
    }

    
    private void UpdateActionButtonsState()
    {
        var effectiveRow = GlobalSelectionState.SelectedRow ?? selectedRow;

        // Показываем стандартные кнопки для всех таблиц кроме current_tasks
        bool isCurrentTasks = currentTable == "current_tasks";

        if (isCurrentTasks)
        {
            // Для current_tasks скрываем стандартные кнопки - теперь их обрабатывает новая система
            if (addButton != null) addButton.gameObject.SetActive(false);
            if (editButton != null) editButton.gameObject.SetActive(false);
            if (deleteButton != null) deleteButton.gameObject.SetActive(false);
            if (refreshButton != null) refreshButton.gameObject.SetActive(false);
        }
        else
        {
            // Стандартная логика для остальных таблиц:
            if (addButton != null) addButton.gameObject.SetActive(true);
            if (editButton != null) editButton.gameObject.SetActive(effectiveRow != null);

            bool canDelete = selectedRow != null;
            if (isTreeViewMode && effectiveRow != null)
            {
                // В дереве скрываем удаление для групповых/неудаляемых узлов
                string nodeType = effectiveRow.ContainsKey("nodeType") ? effectiveRow["nodeType"].ToString() : null;
                bool hasId = effectiveRow.ContainsKey("id") && effectiveRow["id"] != null;
                // Если карта ещё не загружена, не блокируем по признаку маппинга
                bool isMapped = !string.IsNullOrEmpty(nodeType) && (nodeTypeToTable == null || nodeTypeToTable.ContainsKey(nodeType));
                if (!hasId || !isMapped || nodeType == "FieldTreeNode")
                    canDelete = false;
            }
            if (deleteButton != null) deleteButton.gameObject.SetActive(canDelete);

            if (refreshButton != null) refreshButton.gameObject.SetActive(true);
        }

        // Управление visibility dropdown кнопки для PCBs
        bool showPcbDropdown = currentTable == "pcbs";
        if (addDropdownButton != null) addDropdownButton.gameObject.SetActive(showPcbDropdown);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
