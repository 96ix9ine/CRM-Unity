using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using Newtonsoft.Json;
using System.Collections.Generic;

/// <summary>
/// Сервис для работы с API задач
/// Предоставляет методы для взятия, завершения и получения информации о задачах
/// </summary>
public class TaskApiService : MonoBehaviour
{
    [Header("API Settings")]
    public string apiUrl = "http://localhost:3000";

    // Синглтон для доступа из других компонентов
    private static TaskApiService _instance;
    public static TaskApiService Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<TaskApiService>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("TaskApiService");
                    _instance = go.AddComponent<TaskApiService>();
                }
            }
            return _instance;
        }
    }

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Взять задачу в работу
    /// </summary>
    /// <param name="taskId">ID задачи</param>
    /// <param name="onSuccess">Callback при успешном выполнении</param>
    /// <param name="onError">Callback при ошибке</param>
    public void TakeTask(string taskId, System.Action onSuccess = null, System.Action<string> onError = null)
    {
        StartCoroutine(TakeTaskRequest(taskId, onSuccess, onError));
    }

    /// <summary>
    /// Завершить подзадачу (stand_task)
    /// </summary>
    /// <param name="taskId">ID подзадачи</param>
    /// <param name="onSuccess">Callback при успешном выполнении</param>
    /// <param name="onError">Callback при ошибке</param>
    public void CompleteStandTask(string taskId, System.Action onSuccess = null, System.Action<string> onError = null)
    {
        StartCoroutine(CompleteStandTaskRequest(taskId, onSuccess, onError));
    }

    /// <summary>
    /// Завершить текущую задачу
    /// </summary>
    /// <param name="taskId">ID задачи</param>
    /// <param name="onSuccess">Callback при успешном выполнении</param>
    /// <param name="onError">Callback при ошибке</param>
    public void CompleteCurrentTask(string taskId, System.Action onSuccess = null, System.Action<string> onError = null)
    {
        StartCoroutine(CompleteCurrentTaskRequest(taskId, onSuccess, onError));
    }

    /// <summary>
    /// Получить информацию о задаче по ID
    /// </summary>
    /// <param name="taskId">ID задачи</param>
    /// <param name="onSuccess">Callback при успешном получении данных</param>
    /// <param name="onError">Callback при ошибке</param>
    public void GetTaskById(string taskId, System.Action<Dictionary<string, object>> onSuccess = null, System.Action<string> onError = null)
    {
        StartCoroutine(GetTaskByIdRequest(taskId, onSuccess, onError));
    }

    /// <summary>
    /// Получить дерево текущих задач для сотрудника
    /// </summary>
    /// <param name="onSuccess">Callback при успешном получении данных</param>
    /// <param name="onError">Callback при ошибке</param>
    public void GetMyTasksTree(System.Action<List<Dictionary<string, object>>> onSuccess = null, System.Action<string> onError = null)
    {
        StartCoroutine(GetMyTasksTreeRequest(onSuccess, onError));
    }

    /// <summary>
    /// Получить дерево всех задач (для админов)
    /// </summary>
    /// <param name="onSuccess">Callback при успешном получении данных</param>
    /// <param name="onError">Callback при ошибке</param>
    public void GetAllTasksTree(System.Action<List<Dictionary<string, object>>> onSuccess = null, System.Action<string> onError = null)
    {
        StartCoroutine(GetAllTasksTreeRequest(onSuccess, onError));
    }

    #region Coroutine Implementations

    private IEnumerator TakeTaskRequest(string taskId, System.Action onSuccess, System.Action<string> onError)
    {
        string url = $"{apiUrl}/current_tasks/start/{taskId}";

        using (UnityWebRequest www = UnityWebRequest.PostWwwForm(url, ""))
        {
            SetAuthorizationHeader(www);

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"Задача {taskId} успешно взята в работу");
                onSuccess?.Invoke();
            }
            else
            {
                string errorMessage = $"Ошибка при взятии задачи: {www.error}";
                if (www.downloadHandler != null && !string.IsNullOrEmpty(www.downloadHandler.text))
                {
                    errorMessage += $"\nResponse: {www.downloadHandler.text}";
                }
                Debug.LogError(errorMessage);
                onError?.Invoke(errorMessage);
            }
        }
    }

    private IEnumerator CompleteStandTaskRequest(string taskId, System.Action onSuccess, System.Action<string> onError)
    {
        string url = $"{apiUrl}/stand_tasks/complete/{taskId}";

        using (UnityWebRequest www = UnityWebRequest.PostWwwForm(url, ""))
        {
            SetAuthorizationHeader(www);

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"Подзадача {taskId} успешно завершена");
                onSuccess?.Invoke();
            }
            else
            {
                string errorMessage = $"Ошибка при завершении подзадачи: {www.error}";
                if (www.downloadHandler != null && !string.IsNullOrEmpty(www.downloadHandler.text))
                {
                    errorMessage += $"\nResponse: {www.downloadHandler.text}";
                }
                Debug.LogError(errorMessage);
                onError?.Invoke(errorMessage);
            }
        }
    }

    private IEnumerator CompleteCurrentTaskRequest(string taskId, System.Action onSuccess, System.Action<string> onError)
    {
        string url = $"{apiUrl}/current_tasks/complete/{taskId}";

        using (UnityWebRequest www = UnityWebRequest.PostWwwForm(url, ""))
        {
            SetAuthorizationHeader(www);

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"Задача {taskId} успешно завершена");
                onSuccess?.Invoke();
            }
            else
            {
                string errorMessage = $"Ошибка при завершении задачи: {www.error}";
                if (www.downloadHandler != null && !string.IsNullOrEmpty(www.downloadHandler.text))
                {
                    errorMessage += $"\nResponse: {www.downloadHandler.text}";
                }
                Debug.LogError(errorMessage);
                onError?.Invoke(errorMessage);
            }
        }
    }

    private IEnumerator GetTaskByIdRequest(string taskId, System.Action<Dictionary<string, object>> onSuccess, System.Action<string> onError)
    {
        string url = $"{apiUrl}/current_tasks/get/{taskId}";

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            SetAuthorizationHeader(www);

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var taskData = JsonConvert.DeserializeObject<Dictionary<string, object>>(www.downloadHandler.text);
                    onSuccess?.Invoke(taskData);
                }
                catch (System.Exception e)
                {
                    string errorMessage = $"Ошибка парсинга JSON: {e.Message}";
                    Debug.LogError(errorMessage);
                    onError?.Invoke(errorMessage);
                }
            }
            else
            {
                string errorMessage = $"Ошибка при получении задачи: {www.error}";
                Debug.LogError(errorMessage);
                onError?.Invoke(errorMessage);
            }
        }
    }

    private IEnumerator GetMyTasksTreeRequest(System.Action<List<Dictionary<string, object>>> onSuccess, System.Action<string> onError)
    {
        string url = $"{apiUrl}/current_tasks/myTree";

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            SetAuthorizationHeader(www);

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var tasksData = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(www.downloadHandler.text);
                    onSuccess?.Invoke(tasksData);
                }
                catch (System.Exception e)
                {
                    string errorMessage = $"Ошибка парсинга JSON: {e.Message}";
                    Debug.LogError(errorMessage);
                    onError?.Invoke(errorMessage);
                }
            }
            else
            {
                string errorMessage = $"Ошибка при получении дерева задач: {www.error}";
                Debug.LogError(errorMessage);
                onError?.Invoke(errorMessage);
            }
        }
    }

    private IEnumerator GetAllTasksTreeRequest(System.Action<List<Dictionary<string, object>>> onSuccess, System.Action<string> onError)
    {
        string url = $"{apiUrl}/current_tasks/tree";

        using (UnityWebRequest www = UnityWebRequest.Get(url))
        {
            SetAuthorizationHeader(www);

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    var tasksData = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(www.downloadHandler.text);
                    onSuccess?.Invoke(tasksData);
                }
                catch (System.Exception e)
                {
                    string errorMessage = $"Ошибка парсинга JSON: {e.Message}";
                    Debug.LogError(errorMessage);
                    onError?.Invoke(errorMessage);
                }
            }
            else
            {
                string errorMessage = $"Ошибка при получении дерева всех задач: {www.error}";
                Debug.LogError(errorMessage);
                onError?.Invoke(errorMessage);
            }
        }
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// Устанавливает заголовок авторизации для запроса
    /// </summary>
    private void SetAuthorizationHeader(UnityWebRequest request)
    {
        string token = PlayerPrefs.GetString("token", "");
        if (!string.IsNullOrEmpty(token))
        {
            request.SetRequestHeader("Authorization", "Bearer " + token);
        }
    }

    /// <summary>
    /// Проверяет наличие токена авторизации
    /// </summary>
    public bool HasAuthToken()
    {
        string token = PlayerPrefs.GetString("token", "");
        return !string.IsNullOrEmpty(token);
    }

    /// <summary>
    /// Получает текущую роль пользователя
    /// </summary>
    public string GetUserRole()
    {
        return PlayerPrefs.GetString("profession", "");
    }

    /// <summary>
    /// Проверяет, может ли пользователь брать задачи (не директор/админ)
    /// </summary>
    public bool CanUserTakeTasks()
    {
        string role = GetUserRole();
        return !role.Contains("Директор") &&
               !role.Contains("Администратор") &&
               !role.Contains("Test");
    }

    #endregion
}