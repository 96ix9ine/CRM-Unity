using Auth.Models;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

/// <summary>
/// Сервис для работы с аутентификацией и регистрацией пользователей через API.
/// Вся бизнес-логика вынесена сюда для разделения ответственности.
/// </summary>
public class AuthService : MonoBehaviour
{
    // Используем TableApiService как единственный источник базового URL
    [SerializeField] private TableApiService tableApiService;
    
    // Делегаты для колбэков успешной/неуспешной авторизации и регистрации
    public delegate void AuthCallback(bool success, string message);
    public delegate void RegisterCallback(bool success, string message);
    public delegate void CheckUsersCallback(bool usersExist);

    /// <summary>
    /// Отправляет запрос на авторизацию пользователя.
    /// </summary>
    /// <param name="userName">Логин пользователя</param>
    /// <param name="password">Пароль пользователя</param>
    /// <param name="callback">Колбэк с результатом</param>
    public IEnumerator Login(string userName, string password, AuthCallback callback)
    {
        if (tableApiService == null) tableApiService = FindObjectOfType<TableApiService>();
        if (tableApiService == null)
        {
            callback?.Invoke(false, "TableApiService не найден на сцене");
            yield break;
        }
        var request = new AuthRequest
        {
            userName = userName,
            password = password
        };

        string json = JsonConvert.SerializeObject(request);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        using (UnityWebRequest www = new UnityWebRequest($"{tableApiService.apiUrl}/auth/login", "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            // Debug.Log($"Sending request to: {API_URL}/auth/login");
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"Login successful. Response: {www.downloadHandler.text}");
                var response = JsonConvert.DeserializeObject<AuthResponse>(www.downloadHandler.text);
                SaveUserData(response); // Сохраняем данные пользователя в PlayerPrefs
                // Привязываем NotificationManager к user.id
                if (response != null && response.user != null && !string.IsNullOrEmpty(response.user.id.ToString())) {
                    NotificationManager.Instance.SetUser(response.user.id.ToString());
                    // WebSocket подключение происходит в NotificationManager.SetUser()
                    Debug.Log("NotificationManager настроен для пользователя");
                }

                // Загружаем права доступа
                yield return RolePermissionsService.Instance.LoadPermissionsFromBackend();
                
                // Устанавливаем профессию пользователя (если есть в ответе)
                if (response != null && response.user != null && response.user.employeeProfession != null && response.user.employeeProfession.professions != null && !string.IsNullOrEmpty(response.user.employeeProfession.professions.title)) {
                    RolePermissionsService.Instance.SetCurrentProfession(response.user.employeeProfession.professions.title);
                }

                // Сообщаем об успешной авторизации, чтобы UI переключился на MainPanel
                callback?.Invoke(true, "Вы успешно вошли в систему");
            }
            else
            {
                var error = JsonConvert.DeserializeObject<ErrorResponse>(www.downloadHandler.text);
                callback?.Invoke(false, $"Ошибка входа: {error.message}");
            }
        }
    }

    /// <summary>
    /// Отправляет запрос на регистрацию нового пользователя.
    /// </summary>
    public IEnumerator Register(string email, string password, string userName, string passwordSalt, 
        string phone, string birthDate, string firstName, string lastName, string middleName, RegisterCallback callback)
    {
        if (tableApiService == null) tableApiService = FindObjectOfType<TableApiService>();
        if (tableApiService == null)
        {
            callback?.Invoke(false, "TableApiService не найден на сцене");
            yield break;
        }
        var request = new RegisterRequest
        {
            email = email,
            password = password,
            userName = userName,
            passwordSalt = passwordSalt,
            phone = phone,
            birthDate = birthDate,
            firstName = firstName,
            lastName = lastName,
            middleName = middleName,
        };

        string json = JsonConvert.SerializeObject(request);
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);
        // Debug.Log("Register JSON: " + json);

        using (UnityWebRequest www = new UnityWebRequest($"{tableApiService.apiUrl}/auth/register", "POST"))
        {
            www.uploadHandler = new UploadHandlerRaw(bodyRaw);
            www.downloadHandler = new DownloadHandlerBuffer();
            www.SetRequestHeader("Content-Type", "application/json");

            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                callback?.Invoke(true, "Вы успешно зарегистрировались в системе");
            }
            else
            {
                var error = JsonConvert.DeserializeObject<ErrorResponse>(www.downloadHandler.text);
                callback?.Invoke(false, $"Ошибка регистрации: {error.message}");
            }
        }
    }

    /// <summary>
    /// Проверяет существование пользователей в системе.
    /// </summary>
    /// <param name="callback">Колбэк с результатом проверки</param>
    public IEnumerator CheckUsersExist(CheckUsersCallback callback)
    {
        if (tableApiService == null) tableApiService = FindObjectOfType<TableApiService>();
        if (tableApiService == null)
        {
            callback?.Invoke(false);
            yield break;
        }
        using (UnityWebRequest www = UnityWebRequest.Get($"{tableApiService.apiUrl}/user/get"))
        {
            yield return www.SendWebRequest();

            if (www.result == UnityWebRequest.Result.Success)
            {
                var users = JsonConvert.DeserializeObject<object[]>(www.downloadHandler.text);
                bool usersExist = users != null && users.Length > 0;
                callback?.Invoke(usersExist);
            }
            else
            {
                Debug.LogError($"Ошибка проверки пользователей: {www.error}");
                callback?.Invoke(false); // В случае ошибки считаем что пользователей нет
            }
        }
    }

    /// <summary>
    /// Сохраняет данные пользователя в PlayerPrefs после успешной авторизации.
    /// </summary>
    /// <param name="response">Ответ от сервера с данными пользователя</param>
    private void SaveUserData(AuthResponse response)
    {
        PlayerPrefs.SetString("token", response.token);
        PlayerPrefs.SetString("userId", response.user.id.ToString());

        if (response.user != null && response.user.employees != null)
        {
            var employee = response.user.employees;

            // Имя и фамилия из peoples
            if (employee.peoples != null)
            {
                PlayerPrefs.SetString("firstName", employee.peoples.firstName);
                PlayerPrefs.SetString("lastName", employee.peoples.lastName);
            }
            else
            {
                PlayerPrefs.SetString("firstName", "");
                PlayerPrefs.SetString("lastName", "");
            }

            // Профессия из employeeProfession
            if (response.user.employeeProfession != null && response.user.employeeProfession.professions != null)
            {
                PlayerPrefs.SetString("profession", response.user.employeeProfession.professions.title);
            }
            else
            {
                PlayerPrefs.SetString("profession", "Без профессии");
            }
        }
        else
        {
            PlayerPrefs.SetString("firstName", "");
            PlayerPrefs.SetString("lastName", "");
            PlayerPrefs.SetString("profession", "Без профессии");
        }
    }
}