using UnityEngine;
using UI.Selection;
using UnityEngine.UI;

/// <summary>
/// Менеджер для интеграции кнопок задач в существующий UI
/// Подключает функционал кнопок к ActionsButtonsMainMenu и другим компонентам
/// </summary>
public class TaskButtonsManager : MonoBehaviour
{
    [Header("Components")]
    public TaskButtonsUI taskUI;
    public TaskButtonsController taskController;
    public ActionsButtonsMainMenu actionsButtons;

    [Header("Task Buttons References")]
    public Button takeTaskButton;
    public Button completeTaskButton;

    void Start()
    {
        InitializeComponents();
        SetupIntegration();
    }

    /// <summary>
    /// Инициализация компонентов и установка связей
    /// </summary>
    private void InitializeComponents()
    {
        // Убеждаемся, что TaskApiService существует
        if (TaskApiService.Instance == null)
        {
            Debug.LogWarning("TaskApiService не найден! Создаем новый экземпляр.");
            GameObject apiServiceGo = new GameObject("TaskApiService");
            apiServiceGo.AddComponent<TaskApiService>();
        }

        // Настраиваем UI компонент
        if (taskUI != null)
        {
            taskUI.SetCurrentTable("current_tasks");
        }

        // Настраиваем контроллер
        if (taskController != null)
        {
            taskController.apiService = FindObjectOfType<TableApiService>();
            taskController.listItems = FindObjectOfType<ListItems>();
            taskController.taskUI = taskUI;
        }
    }

    /// <summary>
    /// Настройка интеграции с существующими компонентами
    /// </summary>
    private void SetupIntegration()
    {
        // Если есть ActionsButtonsMainMenu, настраиваем интеграцию
        if (actionsButtons != null)
        {
            SetupActionsButtonsIntegration();
        }
        else
        {
            Debug.LogWarning("ActionsButtonsMainMenu не найден. Кнопки задач будут работать независимо.");
        }

        // Настраиваем прямую интеграцию кнопок
        SetupDirectButtonIntegration();
    }

    /// <summary>
    /// Интеграция с ActionsButtonsMainMenu для замены старой логики
    /// </summary>
    private void SetupActionsButtonsIntegration()
    {
        // Примечание: кнопки задач теперь управляются независимо от ActionsButtonsMainMenu
        // Старая логика удалена для избежания конфликтов

        // Подписываемся на события смены таблицы
        var tableSelector = FindObjectOfType<UI.TreeView.Views.DatabaseTreeView>();
        if (tableSelector != null)
        {
            // При смене таблицы на current_tasks активируем наш UI
            // Это потребует доработки в зависимости от архитектуры
        }
    }

    /// <summary>
    /// Настройка прямой интеграции кнопок
    /// </summary>
    private void SetupDirectButtonIntegration()
    {
        // Убеждаемся, что кнопки назначены
        if (takeTaskButton != null && completeTaskButton != null)
        {
            // Если компоненты UI не назначены в инспекторе, ищем их
            if (taskUI == null)
                taskUI = FindObjectOfType<TaskButtonsUI>();

            if (taskController == null)
                taskController = FindObjectOfType<TaskButtonsController>();

            // Назначаем кнопки в UI компонент
            if (taskUI != null)
            {
                taskUI.takeTaskButton = takeTaskButton;
                taskUI.completeTaskButton = completeTaskButton;
                taskUI.takeTaskButtonText = takeTaskButton.GetComponentInChildren<Text>();
                taskUI.completeTaskButtonText = completeTaskButton.GetComponentInChildren<Text>();
                taskUI.takeTaskButtonImage = takeTaskButton.GetComponent<UnityEngine.UI.Image>();
                taskUI.completeTaskButtonImage = completeTaskButton.GetComponent<UnityEngine.UI.Image>();
            }
        }
    }

    /// <summary>
    /// Активировать кнопки задач (вызывать при переходе в таблицу current_tasks)
    /// </summary>
    public void EnableTaskButtons()
    {
        if (taskUI != null)
        {
            taskUI.SetCurrentTable("current_tasks");
        }

        if (taskController != null)
        {
            taskController.SetTreeViewMode(FindObjectOfType<UI.TreeView.Views.DatabaseTreeView>(),
                                          FindObjectOfType<UI.TreeView.Views.DatabaseTreeView>() != null);
        }
    }

    /// <summary>
    /// Деактивировать кнопки задач (вызывать при уходе из таблицы current_tasks)
    /// </summary>
    public void DisableTaskButtons()
    {
        if (taskUI != null)
        {
            taskUI.SetCurrentTable("");
        }
    }

    /// <summary>
    /// Обновить состояние кнопок (вызывать после изменения данных)
    /// </summary>
    public void RefreshTaskButtons()
    {
        if (taskController != null)
        {
            taskController.RefreshUI();
        }
        else if (taskUI != null)
        {
            taskUI.RefreshUI();
        }
    }

    /// <summary>
    /// Метод для вызова из старого ActionsButtonsMainMenu
    /// Позволяет оставить совместимость со старым кодом
    /// </summary>
    public void OnTableChanged(string tableName)
    {
        if (tableName == "current_tasks")
        {
            EnableTaskButtons();
        }
        else
        {
            DisableTaskButtons();
        }
    }

    void OnDestroy()
    {
        // Очистка при уничтожении объекта
    }
}