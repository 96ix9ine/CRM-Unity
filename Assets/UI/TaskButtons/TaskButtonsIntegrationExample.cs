using UnityEngine;
using UI.Selection;
using System.Collections.Generic;
using UnityEngine.UI;

/// <summary>
/// Пример интеграции новых кнопок задач в существующую систему
/// Этот скрипт показывает, как заменить старую логику в ActionsButtonsMainMenu
/// </summary>
public class TaskButtonsIntegrationExample : MonoBehaviour
{
    [Header("Task System Components")]
    public TaskButtonsManager taskButtonsManager;
    public TaskButtonsUI taskButtonsUI;
    public TaskButtonsController taskButtonsController;

    [Header("Legacy Components")]
    public ActionsButtonsMainMenu actionsButtonsMainMenu;
    public ListItems listItems;
    public UI.TreeView.Views.DatabaseTreeView treeView;

    [Header("UI Elements")]
    public Button legacyAddButton;
    public Button legacyEditButton;
    public Button legacyDeleteButton;
    public Button legacyRefreshButton;

    void Start()
    {
        InitializeTaskSystem();
        SetupEventHandlers();
    }

    /// <summary>
    /// Инициализация системы задач
    /// </summary>
    private void InitializeTaskSystem()
    {
        // Если компоненты не назначены, ищем их
        if (taskButtonsManager == null)
            taskButtonsManager = FindObjectOfType<TaskButtonsManager>();

        if (taskButtonsUI == null)
            taskButtonsUI = FindObjectOfType<TaskButtonsUI>();

        if (taskButtonsController == null)
            taskButtonsController = FindObjectOfType<TaskButtonsController>();

        // Настраиваем компоненты системы задач
        if (taskButtonsUI != null && listItems != null)
        {
            // Подписываемся на события выбора строки
            listItems.OnRowSelected += OnRowSelected;
        }

        if (taskButtonsController != null)
        {
            // Настраиваем контроллер
            taskButtonsController.listItems = listItems;
            taskButtonsController.treeView = treeView;
            taskButtonsController.SetTreeViewMode(treeView, treeView != null);
        }
    }

    /// <summary>
    /// Настройка обработчиков событий
    /// </summary>
    private void SetupEventHandlers()
    {
        // Глобальный обработчик сброса выделения
        SelectionRegistry.RegisterDeselect(() => {
            if (taskButtonsUI != null)
            {
                taskButtonsUI.RefreshUI();
            }
        });
    }

    /// <summary>
    /// Обработчик смены таблицы (замена для SetTable в ActionsButtonsMainMenu)
    /// </summary>
    public void SetTable(string tableName)
    {
        // Управляем видимостью кнопок задач
        if (taskButtonsManager != null)
        {
            taskButtonsManager.OnTableChanged(tableName);
        }
        else if (taskButtonsUI != null)
        {
            taskButtonsUI.SetCurrentTable(tableName);
        }

        // Показываем/скрываем обычные кнопки действий
        bool showLegacyButtons = tableName != "current_tasks";
        SetLegacyButtonsVisibility(showLegacyButtons);
    }

    /// <summary>
    /// Обработчик выбора строки
    /// </summary>
    private void OnRowSelected(Dictionary<string, object> row)
    {
        // Дополнительно обновляем состояние кнопок
        if (taskButtonsUI != null)
        {
            taskButtonsUI.RefreshUI();
        }
    }

    /// <summary>
    /// Управление видимостью обычных кнопок действий
    /// </summary>
    private void SetLegacyButtonsVisibility(bool visible)
    {
        if (legacyAddButton != null)
            legacyAddButton.gameObject.SetActive(visible);

        if (legacyEditButton != null)
            legacyEditButton.gameObject.SetActive(visible);

        if (legacyDeleteButton != null)
            legacyDeleteButton.gameObject.SetActive(visible);

        if (legacyRefreshButton != null)
            legacyRefreshButton.gameObject.SetActive(visible);
    }

    /// <summary>
    /// Обновление данных после операций с задачами
    /// </summary>
    public void RefreshData()
    {
        if (taskButtonsManager != null)
        {
            taskButtonsManager.RefreshTaskButtons();
        }

        // Обновляем основную таблицу
        if (treeView != null)
        {
            treeView.ShowTableTree("current_tasks");
        }
        else if (listItems != null)
        {
            StartCoroutine(listItems.ShowTableData("current_tasks"));
        }
    }

    /// <summary>
    /// Пример метода для создания контекстного меню
    /// </summary>
    public void ShowTaskContextMenu(Dictionary<string, object> selectedRow)
    {
        if (selectedRow == null) return;

        string nodeType = selectedRow.ContainsKey("nodeType") ? selectedRow["nodeType"].ToString() : "";

        if (nodeType == "current_tasks")
        {
            // Показать опции для текущей задачи
            Debug.Log("Контекстное меню для текущей задачи");
        }
        else if (nodeType == "stand_tasks")
        {
            // Показать опции для подзадачи
            Debug.Log("Контекстное меню для подзадачи");
        }
    }

    /// <summary>
    /// Проверка состояния системы задач
    /// </summary>
    public bool IsTaskSystemReady()
    {
        return taskButtonsManager != null &&
               taskButtonsUI != null &&
               taskButtonsController != null &&
               TaskApiService.Instance != null;
    }

    /// <summary>
    /// Получение информации о текущем пользователе
    /// </summary>
    public string GetUserInfo()
    {
        if (TaskApiService.Instance != null)
        {
            return $"Role: {TaskApiService.Instance.GetUserRole()}, " +
                   $"CanTakeTasks: {TaskApiService.Instance.CanUserTakeTasks()}, " +
                   $"HasToken: {TaskApiService.Instance.HasAuthToken()}";
        }
        return "Task API Service not available";
    }

    void OnDestroy()
    {
        // Отписываемся от событий
        if (listItems != null)
        {
            listItems.OnRowSelected -= OnRowSelected;
        }
    }
}