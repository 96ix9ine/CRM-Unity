using UnityEngine;
using UnityEngine.UI;
using UI.Selection;
using System.Collections.Generic;

/// <summary>
/// UI компонент для управления кнопками взятия и завершения задач
/// Автоматически управляет видимостью и интерактивностью кнопок в зависимости от состояния задачи
/// </summary>
public class TaskButtonsUI : MonoBehaviour
{
    [Header("Task Buttons")]
    public Button takeTaskButton;
    public Button completeTaskButton;

    [Header("UI Components")]
    public Text takeTaskButtonText;
    public Text completeTaskButtonText;
    public Image takeTaskButtonImage;
    public Image completeTaskButtonImage;

    [Header("References")]
    public TaskButtonsController taskController;

    private Dictionary<string, object> selectedRow;
    private string currentTable;

    // Состояния задач из БД
    private const int TASK_STATE_NEW = 1;         // "Новая"
    private const int TASK_STATE_IN_PROGRESS = 2; // "Выполняется"
    private const int TASK_STATE_COMPLETED = 3;   // "Завершена"
    private const int TASK_STATE_CANCELLED = 4;   // "Отменена"

    void Start()
    {
        // Подписываемся на события выбора строки
        var listItems = FindObjectOfType<ListItems>();
        Debug.Log($"TaskButtonsUI.Start: Найден ListItems: {listItems != null}");
        if (listItems != null)
        {
            listItems.OnRowSelected += OnRowSelected;
            Debug.Log("TaskButtonsUI.Start: Подписались на ListItems.OnRowSelected");
        }

        // Ищем TreeView и подписываемся на его события выбора
        var treeView = FindObjectOfType<UI.TreeView.Views.DatabaseTreeView>();
        Debug.Log($"TaskButtonsUI.Start: Найден DatabaseTreeView: {treeView != null}");
        if (treeView != null)
        {
            treeView.OnRowSelected += OnRowSelected;
            Debug.Log("TaskButtonsUI.Start: Подписались на DatabaseTreeView.OnRowSelected");
        }

        // Подписываемся на события глобального выбора
        SelectionRegistry.RegisterDeselect(() => {
            Debug.Log("TaskButtonsUI: Глобальный deselect - сбрасываем selectedRow");
            selectedRow = null;
            UpdateButtonsVisibility();
        });

        // Примечание: SelectionRegistry.RegisterSelect не существует, используем другие методы

        // Назначаем обработчики нажатий
        if (takeTaskButton != null)
            takeTaskButton.onClick.AddListener(OnTakeTaskButtonClicked);

        if (completeTaskButton != null)
            completeTaskButton.onClick.AddListener(OnCompleteTaskButtonClicked);

        // Изначально скрываем кнопки
        UpdateButtonsVisibility();
    }

    /// <summary>
    /// Устанавливает текущую таблицу для контекста
    /// </summary>
    public void SetCurrentTable(string tableName)
    {
        Debug.Log($"TaskButtonsUI.SetCurrentTable: tableName='{tableName}'");
        currentTable = tableName;
        UpdateButtonsVisibility();
    }

    /// <summary>
    /// Обработчик выбора строки в таблице
    /// </summary>
    private void OnRowSelected(Dictionary<string, object> row)
    {
        Debug.Log($"TaskButtonsUI.OnRowSelected: Получена строка с {row?.Count ?? 0} полями");
        if (row != null)
        {
            Debug.Log($"TaskButtonsUI.OnRowSelected: nodeType={(row.ContainsKey("nodeType") ? row["nodeType"].ToString() : "нет")}");
        }
        selectedRow = row;
        UpdateButtonsVisibility();
    }

    /// <summary>
    /// Обработчик нажатия кнопки "Взять задачу"
    /// </summary>
    private void OnTakeTaskButtonClicked()
    {
        if (taskController != null)
        {
            taskController.TakeTask(selectedRow);
        }
        else
        {
            Debug.LogError("TaskButtonsController не назначен!");
        }
    }

    /// <summary>
    /// Обработчик нажатия кнопки "Завершить задачу"
    /// </summary>
    private void OnCompleteTaskButtonClicked()
    {
        if (taskController != null)
        {
            taskController.CompleteTask(selectedRow);
        }
        else
        {
            Debug.LogError("TaskButtonsController не назначен!");
        }
    }

    /// <summary>
    /// Обновляет видимость и состояние кнопок на основе выбранной задачи
    /// </summary>
    private void UpdateButtonsVisibility()
    {
        // Проверяем, находимся ли мы в таблице current_tasks
        bool isCurrentTasksTable = currentTable == "current_tasks";
        Debug.Log($"UpdateButtonsVisibility: currentTable='{currentTable}', isCurrentTasksTable={isCurrentTasksTable}");

        // Проверяем роль пользователя (директора и админы не могут брать задачи)
        string currentProfession = PlayerPrefs.GetString("profession", "");
        bool isForbiddenRole = currentProfession.Contains("Директор") ||
                               currentProfession.Contains("Администратор") ||
                               currentProfession.Contains("Test");
        Debug.Log($"UpdateButtonsVisibility: profession='{currentProfession}', isForbiddenRole={isForbiddenRole}");

        // По умолчанию скрываем кнопки
        SetButtonState(takeTaskButton, false);
        SetButtonState(completeTaskButton, false);

        if (!isCurrentTasksTable || isForbiddenRole || selectedRow == null)
        {
            Debug.Log($"UpdateButtonsVisibility: Выход - isCurrentTasksTable={isCurrentTasksTable}, isForbiddenRole={isForbiddenRole}, selectedRow={selectedRow != null}");
            return;
        }

        // Определяем текущую задачу и её состояние
        var taskInfo = GetCurrentTaskInfo(selectedRow);
        if (taskInfo == null)
        {
            return;
        }

        int currentStateId = taskInfo.StateId;
        string nodeType = taskInfo.NodeType;

        // Работаем только с current_tasks
        if (nodeType == "current_tasks")
        {
            string currentState = GetCurrentTaskState(selectedRow);
            bool isCompleted = GetIsCompleted(selectedRow);

            // Отладочная информация
            Debug.Log($"TaskButtonsUI: nodeType={nodeType}, currentState={currentState}, isCompleted={isCompleted}");
            Debug.Log($"TaskButtonsUI: selectedRow keys={string.Join(", ", selectedRow?.Keys ?? new Dictionary<string, object>().Keys)}");

            // Кнопка "Взять задачу" - для задач в состоянии "Новая"
            if ((currentState == "Новая" || currentState == "New" || currentState == "NOT_STARTED") && !isCompleted)
            {
                SetButtonState(takeTaskButton, true);
                if (takeTaskButtonText != null)
                {
                    takeTaskButtonText.text = "Взять задачу";
                }
                Debug.Log("TaskButtonsUI: Показываем кнопку 'Взять задачу'");
            }
            else
            {
                SetButtonState(takeTaskButton, false);
                Debug.Log($"TaskButtonsUI: Скрываем кнопку 'Взять задачу' (состояние: {currentState}, завершена: {isCompleted})");
            }

            // Кнопка "Завершить задачу" - для задач в состоянии "Выполняется"
            if ((currentState == "Выполняется" || currentState == "In Progress" || currentState == "IN_PROGRESS") && !isCompleted)
            {
                SetButtonState(completeTaskButton, true);
                if (completeTaskButtonText != null)
                {
                    completeTaskButtonText.text = "Завершить задачу";
                }
                Debug.Log("TaskButtonsUI: Показываем кнопку 'Завершить задачу'");
            }
            else
            {
                SetButtonState(completeTaskButton, false);
                Debug.Log($"TaskButtonsUI: Скрываем кнопку 'Завершить задачу' (состояние: {currentState}, завершена: {isCompleted})");
            }
        }
        else
        {
            Debug.Log($"TaskButtonsUI: Пропускаем, nodeType={nodeType} (ожидал current_tasks)");
        }
    }

    /// <summary>
    /// Устанавливает состояние кнопки (видимость и интерактивность)
    /// </summary>
    private void SetButtonState(Button button, bool visible, bool interactable = true)
    {
        if (button == null) return;

        button.gameObject.SetActive(visible);
        button.interactable = visible && interactable;

        // Дополнительно управляем цветом текста
        var buttonText = button.GetComponentInChildren<Text>();
        if (buttonText != null)
        {
            buttonText.color = visible && interactable ? Color.black : Color.red;
        }
    }

    /// <summary>
    /// Получает информацию о текущей задаче из выбранной строки
    /// </summary>
    private TaskInfo GetCurrentTaskInfo(Dictionary<string, object> row)
    {
        if (row == null || !row.ContainsKey("nodeType"))
            return null;

        string nodeType = row["nodeType"].ToString();
        Dictionary<string, object> currentTaskRow = null;

        // Ищем текущую задачу
        if (nodeType == "current_tasks")
        {
            currentTaskRow = row;
        }
        else if (nodeType == "stand_tasks" && row.ContainsKey("_parent"))
        {
            currentTaskRow = GetParentTaskRow(row["_parent"]);
        }

        if (currentTaskRow == null)
            return null;

        // Получаем ID состояния
        int stateId = -1;
        if (currentTaskRow.ContainsKey("currentTaskStateId"))
        {
            object stateObj = currentTaskRow["currentTaskStateId"];
            if (stateObj is int)
                stateId = (int)stateObj;
            else if (stateObj is long)
                stateId = (int)(long)stateObj;
            else if (stateObj is string)
                int.TryParse((string)stateObj, out stateId);
        }
        else
        {
            // Если currentTaskStateId нет, но есть currentTaskState, пробуем определить по названию
            if (currentTaskRow.ContainsKey("currentTaskState"))
            {
                string stateName = currentTaskRow["currentTaskState"].ToString();
                switch (stateName)
                {
                    case "Новая": stateId = 1; break;
                    case "Выполняется": stateId = 2; break;
                    case "Завершена": stateId = 3; break;
                    case "Отменена": stateId = 4; break;
                }
            }
        }

        return new TaskInfo
        {
            StateId = stateId,
            NodeType = nodeType,
            TaskRow = currentTaskRow
        };
    }

    /// <summary>
    /// Получает родительскую задачу из объекта _parent
    /// </summary>
    private Dictionary<string, object> GetParentTaskRow(object parentObj)
    {
        if (parentObj is Dictionary<string, object> parentDict)
        {
            return parentDict;
        }

        // Обработка JObject из Newtonsoft.Json
        if (parentObj != null && parentObj.GetType().Name.Contains("JObject"))
        {
            var jObj = parentObj as Newtonsoft.Json.Linq.JObject;
            if (jObj != null)
            {
                var result = new Dictionary<string, object>();
                foreach (var prop in jObj.Properties())
                {
                    var value = prop.Value is Newtonsoft.Json.Linq.JValue jval ? jval.Value : prop.Value.ToString();
                    result[prop.Name] = value;
                }
                return result;
            }
        }

        return null;
    }

    /// <summary>
    /// Получает текущее состояние задачи из строки
    /// </summary>
    private string GetCurrentTaskState(Dictionary<string, object> row)
    {
        Debug.Log($"GetCurrentTaskState: Проверяем ключи в row: {string.Join(", ", row?.Keys ?? new Dictionary<string, object>().Keys)}");

        // Приоритет 1: Ищем currentTaskState (название состояния)
        if (row.ContainsKey("currentTaskState"))
        {
            string state = row["currentTaskState"].ToString();
            Debug.Log($"GetCurrentTaskState: Найдено currentTaskState = {state}");
            return state;
        }

        // Приоритет 2: Ищем title (может быть во вложенном объекте)
        if (row.ContainsKey("title"))
        {
            string title = row["title"].ToString();
            Debug.Log($"GetCurrentTaskState: Найдено title = {title}");
            return title;
        }

        // Приоритет 3: Ищем currentTaskStateId (ID состояния)
        if (row.ContainsKey("currentTaskStateId"))
        {
            int stateId = System.Convert.ToInt32(row["currentTaskStateId"]);
            string stateName = GetStateNameById(stateId);
            Debug.Log($"GetCurrentTaskState: Найдено currentTaskStateId = {stateId} -> {stateName}");
            return stateName;
        }

        Debug.Log("GetCurrentTaskState: Состояние не найдено, возвращаем 'Неизвестно'");
        return "Неизвестно";
    }

    /// <summary>
    /// Получает название состояния по ID
    /// </summary>
    private string GetStateNameById(int stateId)
    {
        switch (stateId)
        {
            case 1: return "Новая";
            case 2: return "Выполняется";
            case 3: return "Завершена";
            case 4: return "Отменена";
            default: return $"Неизвестно ({stateId})";
        }
    }

    /// <summary>
    /// Проверяет, завершена ли задача
    /// </summary>
    private bool GetIsCompleted(Dictionary<string, object> row)
    {
        if (row.ContainsKey("isCompleted"))
        {
            return System.Convert.ToBoolean(row["isCompleted"]);
        }
        return false;
    }

    /// <summary>
    /// Обновляет UI после успешного выполнения операции с задачей
    /// </summary>
    public void RefreshUI()
    {
        UpdateButtonsVisibility();
    }

    void OnDestroy()
    {
        // Отписываемся от событий
        var listItems = FindObjectOfType<ListItems>();
        if (listItems != null)
        {
            listItems.OnRowSelected -= OnRowSelected;
        }

        if (takeTaskButton != null)
            takeTaskButton.onClick.RemoveListener(OnTakeTaskButtonClicked);

        if (completeTaskButton != null)
            completeTaskButton.onClick.RemoveListener(OnCompleteTaskButtonClicked);
    }

    /// <summary>
    /// Вспомогательная структура для хранения информации о задаче
    /// </summary>
    private class TaskInfo
    {
        public int StateId { get; set; }
        public string NodeType { get; set; }
        public Dictionary<string, object> TaskRow { get; set; }
    }
}