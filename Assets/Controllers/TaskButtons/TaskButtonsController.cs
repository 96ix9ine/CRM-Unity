using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UI.Selection;
using UI.TreeView.Views;

/// <summary>
/// Контроллер для управления операциями с задачами
/// Обрабатывает логику взятия и завершения задач
/// </summary>
public class TaskButtonsController : MonoBehaviour
{
    [Header("Dependencies")]
    public TaskButtonsUI taskUI;
    public TableApiService apiService;
    public ListItems listItems;
    public DatabaseTreeView treeView;

    [Header("Tree View Settings")]
    public bool isTreeViewMode = false;

    private Dictionary<string, object> selectedRow;

    // Константы для типов узлов
    private const string CURRENT_TASKS_NODE = "current_tasks";
    private const string STAND_TASKS_NODE = "stand_tasks";

    void Start()
    {
        // Подписываемся на события выбора строки
        if (listItems != null)
        {
            listItems.OnRowSelected += OnRowSelected;
        }

        if (treeView != null)
        {
            treeView.OnRowSelected += OnTreeViewRowSelected;
        }
    }

    /// <summary>
    /// Обработчик выбора строки в обычной таблице
    /// </summary>
    private void OnRowSelected(Dictionary<string, object> row)
    {
        selectedRow = row;
    }

    /// <summary>
    /// Обработчик выбора строки в древовидном представлении
    /// </summary>
    private void OnTreeViewRowSelected(Dictionary<string, object> row)
    {
        selectedRow = row;
    }

    /// <summary>
    /// Взять задачу в работу
    /// </summary>
    public void TakeTask(Dictionary<string, object> taskRow)
    {
        if (taskRow == null)
        {
            Debug.LogWarning("Выбранная строка равна null");
            return;
        }

        // Работаем только с current_tasks
        if (!taskRow.ContainsKey("nodeType") || taskRow["nodeType"].ToString() != CURRENT_TASKS_NODE)
        {
            Debug.LogWarning("Невозможно взять задачу: выбранная строка не является current_tasks");
            return;
        }

        if (!taskRow.ContainsKey("id"))
        {
            Debug.LogWarning("У задачи отсутствует ID");
            return;
        }

        string taskId = taskRow["id"].ToString();
        StartCoroutine(TakeTaskRequest(taskId));
    }

    /// <summary>
    /// Завершить задачу
    /// </summary>
    public void CompleteTask(Dictionary<string, object> taskRow)
    {
        if (taskRow == null)
        {
            Debug.LogWarning("Выбранная строка равна null");
            return;
        }

        // Работаем только с current_tasks
        if (!taskRow.ContainsKey("nodeType") || taskRow["nodeType"].ToString() != CURRENT_TASKS_NODE)
        {
            Debug.LogWarning("Невозможно завершить задачу: выбранная строка не является current_tasks");
            return;
        }

        if (!taskRow.ContainsKey("id"))
        {
            Debug.LogWarning("У задачи отсутствует ID");
            return;
        }

        // Проверяем, что задача еще не завершена
        bool isCompleted = taskRow.ContainsKey("isCompleted") &&
                          (taskRow["isCompleted"].ToString() == "1" ||
                           taskRow["isCompleted"].ToString().ToLower() == "true");

        if (isCompleted)
        {
            Debug.LogWarning("Задача уже завершена");
            return;
        }

        string taskId = taskRow["id"].ToString();
        Debug.Log($"Пытаемся завершить задачу с ID: {taskId}");
        StartCoroutine(CompleteTaskRequest(taskId));
    }

    /// <summary>
    /// Получает информацию о текущей задаче из выбранной строки
    /// </summary>
    private Dictionary<string, object> GetCurrentTaskFromRow(Dictionary<string, object> row)
    {
        if (row == null || !row.ContainsKey("nodeType"))
            return null;

        string nodeType = row["nodeType"].ToString();

        if (nodeType == CURRENT_TASKS_NODE)
        {
            return row;
        }
        else if (nodeType == STAND_TASKS_NODE && row.ContainsKey("_parent"))
        {
            return GetParentTaskRow(row["_parent"]);
        }

        return null;
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
    /// Выполняет запрос на взятие задачи
    /// </summary>
    private IEnumerator TakeTaskRequest(string taskId)
    {
        string url = $"/current_tasks/start/{taskId}";

        using (var www = UnityEngine.Networking.UnityWebRequest.PostWwwForm(apiService.apiUrl + url, ""))
        {
            string token = PlayerPrefs.GetString("token", "");
            if (!string.IsNullOrEmpty(token))
                www.SetRequestHeader("Authorization", "Bearer " + token);

            yield return www.SendWebRequest();

            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                Debug.Log("Задача успешно взята в работу");
                RefreshCurrentView();
            }
            else
            {
                Debug.LogError($"Ошибка при взятии задачи: {www.error}");
                Debug.LogError($"Response: {www.downloadHandler?.text}");
            }
        }
    }

    /// <summary>
    /// Выполняет запрос на завершение задачи
    /// </summary>
    private IEnumerator CompleteTaskRequest(string taskId)
    {
        string url = $"/current_tasks/complete/{taskId}";

        // Сохраняем текущий путь в дереве для восстановления после обновления
        List<string> selectedPath = null;
        Core.TreeView.Controllers.TreeViewController controller = null;

        if (isTreeViewMode && treeView != null)
        {
            controller = GetTreeViewController();
            if (controller != null)
            {
                int? selectedId = controller.GetSelectedNodeId();
                if (selectedId.HasValue)
                {
                    selectedPath = controller.GetPathToSelectedNodeByName(selectedId.Value);
                }
            }
        }

        using (var www = UnityEngine.Networking.UnityWebRequest.PostWwwForm(apiService.apiUrl + url, ""))
        {
            string token = PlayerPrefs.GetString("token", "");
            if (!string.IsNullOrEmpty(token))
                www.SetRequestHeader("Authorization", "Bearer " + token);

            yield return www.SendWebRequest();

            if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
            {
                string responseText = www.downloadHandler.text;
                Debug.Log($"Задача успешно завершена. Response: {responseText}");

                if (isTreeViewMode && treeView != null && controller != null && selectedPath != null)
                {
                    // В режиме дерева восстанавливаем путь после обновления
                    RestoreTreeViewPath(selectedPath);
                }
                else
                {
                    RefreshCurrentView();
                }
            }
            else
            {
                Debug.LogError($"Ошибка при завершении задачи: {www.error}");
                Debug.LogError($"Response: {www.downloadHandler?.text}");
            }
        }
    }

    /// <summary>
    /// Получает контроллер дерева
    /// </summary>
    private Core.TreeView.Controllers.TreeViewController GetTreeViewController()
    {
        if (treeView == null) return null;

        var controllerField = treeView.GetType().GetField("treeViewController",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        return controllerField?.GetValue(treeView) as Core.TreeView.Controllers.TreeViewController;
    }

    /// <summary>
    /// Восстанавливает путь в дереве после обновления
    /// </summary>
    private void RestoreTreeViewPath(List<string> path)
    {
        if (treeView == null || path == null) return;

        var controller = GetTreeViewController();
        if (controller == null) return;

        var providerField = treeView.GetType().GetField("treeViewProvider",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var provider = providerField?.GetValue(treeView);

        if (provider != null)
        {
            var eventInfo = provider?.GetType().GetEvent("OnDataLoaded");
            if (eventInfo != null)
            {
                System.Action handler = null;
                handler = () =>
                {
                    eventInfo.RemoveEventHandler(provider, handler);
                    controller.ExpandPathByNames(path);
                };
                eventInfo.AddEventHandler(provider, handler);
            }
        }

        treeView.ShowTableTree("current_tasks");
    }

    /// <summary>
    /// Обновляет текущее представление (таблицу или дерево)
    /// </summary>
    private void RefreshCurrentView()
    {
        if (isTreeViewMode && treeView != null)
        {
            treeView.ShowTableTree("current_tasks");
        }
        else if (listItems != null)
        {
            StartCoroutine(listItems.ShowTableData("current_tasks"));
        }

        // Обновляем UI кнопок
        if (taskUI != null)
        {
            taskUI.RefreshUI();
        }
    }

    /// <summary>
    /// Устанавливает режим древовидного представления
    /// </summary>
    public void SetTreeViewMode(DatabaseTreeView treeView, bool enabled)
    {
        this.treeView = treeView;
        isTreeViewMode = enabled;
    }

    /// <summary>
    /// Принудительное обновление UI (вызывается извне)
    /// </summary>
    public void RefreshUI()
    {
        RefreshCurrentView();
    }

    void OnDestroy()
    {
        // Отписываемся от событий
        if (listItems != null)
        {
            listItems.OnRowSelected -= OnRowSelected;
        }

        if (treeView != null)
        {
            treeView.OnRowSelected -= OnTreeViewRowSelected;
        }
    }
}