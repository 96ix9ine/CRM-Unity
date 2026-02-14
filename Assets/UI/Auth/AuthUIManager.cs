using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using System.Text.RegularExpressions;

/// <summary>
/// Класс для управления UI авторизации и регистрации пользователя.
/// Вся логика взаимодействия с пользователем и вызовы AuthService вынесены сюда.
/// </summary>
public class AuthUIManager : MonoBehaviour
{
    // --- UI Panels ---
    [Header("UI Panels")]
    public GameObject authPanel; // контейнер для login/register
    public GameObject mainPanel; // основной UI после входа
    public GameObject loginPanel;
    public GameObject registerPanel;

    // --- Main Menu (User Info) ---
    [Header("Main Menu User Info")]
    public Text userFirstName;
    public Text userLastName;
    public Text userProfession;
    public Text userDateTimeInfo;
    public Button logoutButton;

    // --- Login Fields ---
    [Header("Login Fields")]
    public InputField loginEmail;
    public InputField loginPassword;
    public Button loginButton;

    // --- Register Fields ---
    [Header("Register Fields")]
    public InputField regEmail;
    public InputField regUserName;
    public InputField regPassword;
    public InputField regPasswordSalt;
    public InputField regPhone;
    public InputField regBirthDate;
    public InputField regFirstName;
    public InputField regLastName;
    public InputField regMiddleName;
    public Button registerButton;


    [Header("Validation")]
    private Text regEmailError;
    private Text regUserNameError;
    private Text regPasswordError;
    private Text regPasswordSaltError;
    private Text regPhoneError;
    private Text regBirthDateError;
    private Text regFirstNameError;
    private Text regLastNameError;

    private const string EMAIL_PATTERN = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
    private const string PHONE_PATTERN = @"^\+7[0-9]{10}$";
    private const int MIN_PASSWORD_LENGTH = 8;

    // Сервис для работы с API аутентификации
    private AuthService authService;

    private bool isUpdatingPhone = false;
    private bool isUpdatingBirthDate = false;

    /// <summary>
    /// Инициализация UI, установка обработчиков событий и сброс полей.
    /// </summary>
    void Start()
    {
        authService = GetComponent<AuthService>();
        if (authService == null)
        {
            Debug.LogError("AuthService component not found!");
            return;
        }

        InitializeUI();
        SetupEventListeners();
        SetupTabNavigation();
        ResetFields();

        // Инициализация главного меню (поля пользователя, logout)
        InitializeMainMenuBindings();
        
        // Проверяем существование пользователей в системе
        StartCoroutine(CheckUsersAndShowAppropriatePanel());
    }

    /// <summary>
    /// Проверяет существование пользователей и показывает соответствующую панель.
    /// </summary>
    private IEnumerator CheckUsersAndShowAppropriatePanel()
    {
        yield return StartCoroutine(authService.CheckUsersExist((usersExist) =>
        {
            if (!usersExist)
            {
                // Если пользователей нет, показываем панель регистрации
                ShowRegister();
            }
            else
            {
                // Если пользователи есть, показываем панель логина
                ShowLogin();
            }
        }));
    }

    /// <summary>
    /// Первичная настройка UI: скрытие попапа, установка типов полей паролей и т.д.
    /// </summary>
    private void InitializeUI()
    {
        if (authPanel != null) authPanel.SetActive(true);
        if (mainPanel != null) mainPanel.SetActive(false);

        loginPassword.contentType = InputField.ContentType.Password;
        regPassword.contentType = InputField.ContentType.Password;
        regPasswordSalt.contentType = InputField.ContentType.Password;

        // Инициализация текстовых полей для ошибок
        regEmailError = regEmail.transform.Find("ErrorText")?.GetComponent<Text>();
        regUserNameError = regUserName.transform.Find("ErrorText")?.GetComponent<Text>();
        regPasswordError = regPassword.transform.Find("ErrorText")?.GetComponent<Text>();
        regPasswordSaltError = regPasswordSalt.transform.Find("ErrorText")?.GetComponent<Text>();
        regPhoneError = regPhone.transform.Find("ErrorText")?.GetComponent<Text>();
        regBirthDateError = regBirthDate.transform.Find("ErrorText")?.GetComponent<Text>();
        regFirstNameError = regFirstName.transform.Find("ErrorText")?.GetComponent<Text>();
        regLastNameError = regLastName.transform.Find("ErrorText")?.GetComponent<Text>();

        // Настройка поля телефона
        regPhone.onValueChanged.AddListener(OnPhoneValueChanged);
        isUpdatingPhone = true;
        if (!regPhone.text.StartsWith("+7"))
        {
            regPhone.text = "+7" + regPhone.text;
        }
        isUpdatingPhone = false;
        regBirthDate.onValueChanged.AddListener(OnBirthDateValueChanged);
    }

    /// <summary>
    /// Инициализация элементов главного меню и восстановление данных пользователя.
    /// </summary>
    private void InitializeMainMenuBindings()
    {
        if (logoutButton != null)
        {
            logoutButton.onClick.RemoveListener(OnLogout);
            logoutButton.onClick.AddListener(OnLogout);
        }
        
        // Восстановление отображаемых данных пользователя
        UpdateUserInfoFromPrefs();

        // Восстановляем userId в NotificationManager (если доступен)
        string userId = PlayerPrefs.GetString("userId", "");
        if (!string.IsNullOrEmpty(userId) && NotificationManager.Instance != null)
        {
            if (string.IsNullOrEmpty(NotificationManager.Instance.currentUserId))
            {
                NotificationManager.Instance.SetUser(userId);
                Debug.Log("Восстановлен пользователь из PlayerPrefs");
            }
        }

        // Показать накопленные уведомления
        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.ShowPendingToasts();
        }

        // Приветствие один раз за сессию
        if (PlayerPrefs.GetInt("greeted", 0) == 0 && NotificationManager.Instance != null)
        {
            string greeting = "Здравствуйте, " + (userFirstName != null ? userFirstName.text : "") + " " + (userLastName != null ? userLastName.text : "") + "!";
            NotificationManager.Instance.AddNotification(greeting, NotificationType.Success);
            PlayerPrefs.SetInt("greeted", 1);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Обновляет UI ФИО и профессии из PlayerPrefs.
    /// </summary>
    private void UpdateUserInfoFromPrefs()
    {
        if (userFirstName != null) userFirstName.text = PlayerPrefs.GetString("firstName", "");
        if (userLastName != null) userLastName.text = PlayerPrefs.GetString("lastName", "");
        if (userProfession != null) userProfession.text = PlayerPrefs.GetString("profession", "Без профессии");
    }

    /// <summary>
    /// Установка обработчиков событий для всех полей и кнопок.
    /// </summary>
    private void SetupEventListeners()
    {
        // Слушатели для полей логина
        loginEmail.onValueChanged.AddListener(_ => ValidateLoginFields());
        loginPassword.onValueChanged.AddListener(_ => ValidateLoginFields());
        loginEmail.onEndEdit.AddListener(OnLoginFieldEndEdit);
        loginPassword.onEndEdit.AddListener(OnLoginFieldEndEdit);

        // Слушатели для полей регистрации
        regEmail.onValueChanged.AddListener(_ => ValidateRegisterFields());
        regPassword.onValueChanged.AddListener(_ => ValidateRegisterFields());
        regPasswordSalt.onValueChanged.AddListener(_ => ValidateRegisterFields());
        regPhone.onValueChanged.AddListener(_ => ValidateRegisterFields());
        regBirthDate.onValueChanged.AddListener(_ => ValidateRegisterFields());
        regUserName.onValueChanged.AddListener(_ => ValidateRegisterFields());
        regFirstName.onValueChanged.AddListener(_ => ValidateRegisterFields());
        regLastName.onValueChanged.AddListener(_ => ValidateRegisterFields());
        regMiddleName.onValueChanged.AddListener(_ => ValidateRegisterFields());

        // Слушатели окончания ввода для полей регистрации
        regEmail.onEndEdit.AddListener(OnRegisterFieldEndEdit);
        regUserName.onEndEdit.AddListener(OnRegisterFieldEndEdit);
        regPassword.onEndEdit.AddListener(OnRegisterFieldEndEdit);
        regPasswordSalt.onEndEdit.AddListener(OnRegisterFieldEndEdit);
        regPhone.onEndEdit.AddListener(OnRegisterFieldEndEdit);
        regBirthDate.onEndEdit.AddListener(OnRegisterFieldEndEdit);
        regFirstName.onEndEdit.AddListener(OnRegisterFieldEndEdit);
        regLastName.onEndEdit.AddListener(OnRegisterFieldEndEdit);
        regMiddleName.onEndEdit.AddListener(OnRegisterFieldEndEdit);
    }

    /// <summary>
    /// Сброс всех полей формы в начальное состояние.
    /// </summary>
    private void ResetFields()
    {
        loginEmail.text = "";
        loginPassword.text = "";
        regEmail.text = "";
        regPassword.text = "";
        regPasswordSalt.text = "";
        regUserName.text = "";
        regPhone.text = "";
        regBirthDate.text = "";
        regFirstName.text = "";
        regLastName.text = "";
        regMiddleName.text = "";
        ValidateLoginFields();
    }

    /// <summary>
    /// Валидация полей логина для активации кнопки входа.
    /// </summary>
    void ValidateLoginFields()
    {
        loginButton.interactable = !string.IsNullOrEmpty(loginEmail.text) && !string.IsNullOrEmpty(loginPassword.text);
        if (!loginButton.interactable && EventSystem.current.currentSelectedGameObject == loginButton.gameObject)
            EventSystem.current.SetSelectedGameObject(loginEmail.gameObject);
    }

    /// <summary>
    /// Валидация полей регистрации для активации кнопки регистрации.
    /// </summary>
    void ValidateRegisterFields()
    {
        bool isValid = ValidateEmail(regEmail.text) &&
                      ValidateUserName(regUserName.text) &&
                      ValidatePassword(regPassword.text) &&
                      ValidatePasswordSalt(regPasswordSalt.text) &&
                      ValidatePhone(regPhone.text) &&
                      ValidateBirthDate(regBirthDate.text) &&
                      ValidateFirstName(regFirstName.text) &&
                      ValidateLastName(regLastName.text);
        registerButton.interactable = isValid;
        if (!registerButton.interactable && EventSystem.current.currentSelectedGameObject == registerButton.gameObject)
        {
            EventSystem.current.SetSelectedGameObject(regEmail.gameObject);
        }
    }

    /// <summary>
    /// Настройка навигации по Tab между полями ввода.
    /// </summary>
    private void SetupTabNavigation()
    {
        // Навигация для формы логина
        Navigation loginNav = new Navigation();
        loginNav.mode = Navigation.Mode.Explicit;
        loginNav.selectOnDown = loginPassword;
        loginNav.selectOnUp = loginPassword;
        loginEmail.navigation = loginNav;

        Navigation loginPassNav = new Navigation();
        loginPassNav.mode = Navigation.Mode.Explicit;
        loginPassNav.selectOnDown = loginEmail;
        loginPassNav.selectOnUp = loginEmail;
        loginPassword.navigation = loginPassNav;

        // Навигация для формы регистрации
        InputField[] regFields = new InputField[] {
            regEmail, regUserName, regPassword, regPasswordSalt, regPhone,
            regBirthDate, regFirstName, regLastName, regMiddleName
        };
        for (int i = 0; i < regFields.Length; i++)
        {
            Navigation nav = new Navigation();
            nav.mode = Navigation.Mode.Explicit;
            nav.selectOnDown = regFields[(i + 1) % regFields.Length];
            nav.selectOnUp = regFields[(i - 1 + regFields.Length) % regFields.Length];
            regFields[i].navigation = nav;
        }
    }

    /// <summary>
    /// Обработка нажатия Enter в полях логина.
    /// </summary>
    private void OnLoginFieldEndEdit(string value)
    {
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            loginButton.onClick.Invoke();
        }
    }

    /// <summary>
    /// Обработка нажатия Enter в полях регистрации.
    /// </summary>
    private void OnRegisterFieldEndEdit(string value)
    {
        if ((Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter)) && 
            (regEmail.isFocused || regUserName.isFocused || regPassword.isFocused || 
             regPasswordSalt.isFocused || regPhone.isFocused || regBirthDate.isFocused || regFirstName.isFocused || regLastName.isFocused || 
             regMiddleName.isFocused ))
        {
            if(registerButton.interactable)
                registerButton.onClick.Invoke();
        }
    }

    /// <summary>
    /// Переключение на панель логина.
    /// </summary>
    public void ShowLogin()
    {
        if (authPanel != null) authPanel.SetActive(true);
        if (mainPanel != null) mainPanel.SetActive(false);
        loginPanel.SetActive(true);
        registerPanel.SetActive(false);
        loginEmail.text = "";
        loginPassword.text = "";
        ValidateLoginFields();

        EventSystem.current.SetSelectedGameObject(loginEmail.gameObject);
        loginEmail.Select();
    }

    /// <summary>
    /// Переключение на панель регистрации.
    /// </summary>
    public void ShowRegister()
    {
        if (authPanel != null) authPanel.SetActive(true);
        if (mainPanel != null) mainPanel.SetActive(false);
        loginPanel.SetActive(false);
        registerPanel.SetActive(true);
        regEmail.text = "";
        regPassword.text = "";
        regPasswordSalt.text = "";
        regPhone.text = "";
        regBirthDate.text = "";
        regUserName.text = "";
        regFirstName.text = "";
        regLastName.text = "";
        regMiddleName.text = "";
        EventSystem.current.SetSelectedGameObject(regEmail.gameObject);
        regEmail.Select();
    }

    /// <summary>
    /// Обработка нажатия кнопки входа.
    /// </summary>
    public void OnLogin()
    {
        StartCoroutine(LoginRequest());
    }

    /// <summary>
    /// Корутина для отправки запроса на вход через AuthService.
    /// </summary>
    private IEnumerator LoginRequest()
    {
        yield return StartCoroutine(authService.Login(loginEmail.text, loginPassword.text, (success, message) =>
        {
            if (!string.IsNullOrEmpty(message))
            {
                NotificationManager.Instance.AddNotification(message, success ? NotificationType.Success : NotificationType.Error);
            }
            if (success)
            {
                SwitchToMainPanel();
            }
        }));
    }

    private void SwitchToMainPanel()
    {
        if (authPanel != null) authPanel.SetActive(false);
        if (mainPanel != null) mainPanel.SetActive(true);
        // после успешного входа обновляем отображаемые данные пользователя
        UpdateUserInfoFromPrefs();
        EventSystem.current.SetSelectedGameObject(null);
    }

    /// <summary>
    /// Обработка нажатия кнопки регистрации.
    /// </summary>
    public void OnRegister()
    {
        StartCoroutine(RegisterRequest());
    }

    /// <summary>
    /// Корутина для отправки запроса на регистрацию через AuthService.
    /// </summary>
    private IEnumerator RegisterRequest()
    {
        if (!ValidateEmail(regEmail.text) ||
            !ValidateUserName(regUserName.text) ||
            !ValidatePassword(regPassword.text) ||
            !ValidatePasswordSalt(regPasswordSalt.text) ||
            !ValidatePhone(regPhone.text) ||
            !ValidateBirthDate(regBirthDate.text) ||
            !ValidateFirstName(regFirstName.text) ||
            !ValidateLastName(regLastName.text))
        {
            NotificationManager.Instance.AddNotification("Пожалуйста, исправьте ошибки в форме", NotificationType.Warning);
            yield break;
        }

        yield return StartCoroutine(authService.Register(
            regEmail.text,
            regPassword.text,
            regUserName.text,
            regPasswordSalt.text,
            regPhone.text,
            regBirthDate.text,
            regFirstName.text,
            regLastName.text,
            regMiddleName.text,
            (success, message) =>
            {
                if (!string.IsNullOrEmpty(message))
                    NotificationManager.Instance.AddNotification(message, success ? NotificationType.Success : NotificationType.Error);
                if (success)
                {
                    StartCoroutine(HandleSuccessfulRegistration());
                }
            }
        ));
    }

    /// <summary>
    /// Действия после успешной регистрации (автоматический вход).
    /// </summary>
    private IEnumerator HandleSuccessfulRegistration()
    {
        // Автоматически входим в систему под зарегистрированными данными
        StartCoroutine(AutoLoginAfterRegistration());
        yield break;
    }

    /// <summary>
    /// Автоматический вход после успешной регистрации.
    /// </summary>
    private IEnumerator AutoLoginAfterRegistration()
    {
        yield return StartCoroutine(authService.Login(regUserName.text, regPassword.text, (success, message) =>
        {
            if (!string.IsNullOrEmpty(message))
                NotificationManager.Instance.AddNotification(message, success ? NotificationType.Success : NotificationType.Error);
            if (success)
            {
                SwitchToMainPanel();
            }
        }));
    }

    /// <summary>
    /// Главный Update: обработка ввода, фокуса и навигации.
    /// </summary>
    void Update()
    {
        HandleFieldFocus();
        HandleTabNavigation();

        // Обновление отображения текущей даты/времени
        if (userDateTimeInfo != null)
        {
            userDateTimeInfo.text = System.DateTime.Now.ToString("yyyy-MM-dd / HH:mm:ss");
        }
    }

    /// <summary>
    /// Автоматический возврат фокуса на поля, если ни одно не выбрано.
    /// </summary>
    private void HandleFieldFocus()
    {
        // Если сейчас выбран любой InputField вне authPanel (например, в модалке) — не трогаем фокус
        if (IsSelectedOutsideAuthInput())
        {
            return;
        }
        if (!loginEmail.isFocused && !loginPassword.isFocused && 
            !regEmail.isFocused && !regUserName.isFocused && !regPassword.isFocused && 
            !regPasswordSalt.isFocused && !regPhone.isFocused && !regBirthDate.isFocused && !regFirstName.isFocused && !regLastName.isFocused && 
            !regMiddleName.isFocused)
        {
            if (loginPanel.activeSelf)
            {
                EventSystem.current.SetSelectedGameObject(loginEmail.gameObject);
                loginEmail.Select();
            }
            else if (registerPanel.activeSelf)
            {
                EventSystem.current.SetSelectedGameObject(regEmail.gameObject);
                regEmail.Select();
            }
        }
    }

    /// <summary>
    /// Обработка Tab для перехода между полями.
    /// </summary>
    private void HandleTabNavigation()
    {
        // Если сейчас выбран любой InputField вне authPanel (например, в модалке) — не перехватываем Tab
        if (IsSelectedOutsideAuthInput())
        {
            return;
        }
        if (loginPanel.activeSelf && Input.GetKeyDown(KeyCode.Tab))
        {
            if (loginEmail.isFocused)
            {
                loginPassword.Select();
            }
            else if (loginPassword.isFocused)
            {
                loginEmail.Select();
            }
        }
        if (registerPanel.activeSelf && Input.GetKeyDown(KeyCode.Tab))
        {
            InputField[] regFields = new InputField[] {
                regEmail, regUserName, regPassword, regPasswordSalt, regPhone,
                regBirthDate, regFirstName, regLastName, regMiddleName, 
            };
            for (int i = 0; i < regFields.Length; i++)
            {
                if (regFields[i].isFocused)
                {
                    regFields[(i + 1) % regFields.Length].Select();
                    break;
                }
            }
        }
    }

    /// <summary>
    /// Возвращает true, если текущий выбранный объект — это InputField, который НЕ находится внутри authPanel.
    /// То есть фокус сейчас во внешнем UI (например, модалка), и AuthUIManager не должен вмешиваться.
    /// </summary>
    private bool IsSelectedOutsideAuthInput()
    {
        var selected = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        if (selected == null)
            return false;

        // Интересует только случай, когда выбран именно InputField
        var selectedInput = selected.GetComponent<InputField>();
        if (selectedInput == null)
            return false;

        // Если по какой-то причине нет ссылки на authPanel — подстрахуемся и не будем вмешиваться
        if (authPanel == null)
            return true;

        // true, если выбранный InputField не является дочерним элементом authPanel
        return !selected.transform.IsChildOf(authPanel.transform);
    }

    /// <summary>
    /// Выход из аккаунта, очистка PlayerPrefs и возврат на Login панель.
    /// </summary>
    private void OnLogout()
    {
        if (NotificationManager.Instance != null)
        {
            NotificationManager.Instance.Logout();
        }

        string[] keysToDelete = new string[] {
            "token",
            "firstName",
            "lastName",
            "middleName",
            "profession",
            "professionId",
            "userId",
            "userName",
            "email",
            "phone",
            "comment",
            "greeted"
        };

        foreach (string key in keysToDelete)
        {
            PlayerPrefs.DeleteKey(key);
        }
        PlayerPrefs.Save();

        if (mainPanel != null) mainPanel.SetActive(false);
        if (authPanel != null) authPanel.SetActive(true);
        ShowLogin();
    }

    private void OnPhoneValueChanged(string value)
    {
        if (isUpdatingPhone) return;
        
        if (!value.StartsWith("+7"))
        {
            isUpdatingPhone = true;
            regPhone.text = "+7" + value;
            regPhone.caretPosition = regPhone.text.Length;
            isUpdatingPhone = false;
        }
    }

    private void OnBirthDateValueChanged(string value)
    {
        if (isUpdatingBirthDate) return;
        isUpdatingBirthDate = true;
        // Удаляем все, кроме цифр
        string digits = System.Text.RegularExpressions.Regex.Replace(value, "[^0-9]", "");
        string result = "";
        if (digits.Length > 0)
            result += digits.Substring(0, Math.Min(4, digits.Length));
        if (digits.Length > 4)
            result += "-" + digits.Substring(4, Math.Min(2, digits.Length - 4));
        if (digits.Length > 6)
            result += "-" + digits.Substring(6, Math.Min(2, digits.Length - 6));
        if (result.Length > 10)
            result = result.Substring(0, 10);
        regBirthDate.text = result;
        regBirthDate.caretPosition = regBirthDate.text.Length;
        isUpdatingBirthDate = false;
    }

    private bool ValidateEmail(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            if (regEmailError != null) regEmailError.text = "Email обязателен";
            return false;
        }
        if (!Regex.IsMatch(email, EMAIL_PATTERN))
        {
            if (regEmailError != null) regEmailError.text = "Неверный формат email";
            return false;
        }
        if (regEmailError != null) regEmailError.text = "";
        return true;
    }

    private bool ValidateUserName(string userName)
    {
        if (string.IsNullOrEmpty(userName))
        {
            if (regUserNameError != null) regUserNameError.text = "Имя пользователя обязательно";
            return false;
        }
        if (regUserNameError != null) regUserNameError.text = "";
        return true;
    }

    private bool ValidatePassword(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            if (regPasswordError != null) regPasswordError.text = "Пароль обязателен";
            return false;
        }
        if (password.Length < MIN_PASSWORD_LENGTH)
        {
            if (regPasswordError != null) regPasswordError.text = $"Пароль должен быть не менее {MIN_PASSWORD_LENGTH} символов";
            return false;
        }
        if (regPasswordError != null) regPasswordError.text = "";
        return true;
    }

    private bool ValidatePasswordSalt(string passwordSalt)
    {
        if (string.IsNullOrEmpty(passwordSalt))
        {
            if (regPasswordSaltError != null) regPasswordSaltError.text = "Подтверждение пароля обязательно";
            return false;
        }
        if (passwordSalt != regPassword.text)
        {
            if (regPasswordSaltError != null) regPasswordSaltError.text = "Пароли не совпадают";
            return false;
        }
        if (regPasswordSaltError != null) regPasswordSaltError.text = "";
        return true;
    }

    private bool ValidatePhone(string phone)
    {
        if (string.IsNullOrEmpty(phone))
        {
            if (regPhoneError != null) regPhoneError.text = "Телефон обязателен";
            return false;
        }
        if (!Regex.IsMatch(phone, PHONE_PATTERN))
        {
            if (regPhoneError != null) regPhoneError.text = "Неверный формат телефона";
            return false;
        }
        if (regPhoneError != null) regPhoneError.text = "";
        return true;
    }

    
    private bool ValidateBirthDate(string date)
    {
        if (string.IsNullOrEmpty(date))
        {
            if (regBirthDateError != null) regBirthDateError.text = "Дата рождения обязательна";
            return false;
        }
        // Проверка формата yyyy-mm-dd
        if (!System.Text.RegularExpressions.Regex.IsMatch(date, @"^\d{4}-\d{2}-\d{2}$"))
        {
            if (regBirthDateError != null) regBirthDateError.text = "Формат: гггг-мм-дд";
            return false;
        }
        // Проверка существования даты
        DateTime dt;
        if (!DateTime.TryParseExact(date, "yyyy-MM-dd", null, System.Globalization.DateTimeStyles.None, out dt))
        {
            if (regBirthDateError != null) regBirthDateError.text = "Некорректная дата";
            return false;
        }
        if (regBirthDateError != null) regBirthDateError.text = "";
        return true;
    }

    private bool ValidateFirstName(string firstName)
    {
        if (string.IsNullOrEmpty(firstName))
        {
            if (regFirstNameError != null) regFirstNameError.text = "Имя обязательно";
            return false;
        }
        if (regFirstNameError != null) regFirstNameError.text = "";
        return true;
    }

    private bool ValidateLastName(string lastName)
    {
        if (string.IsNullOrEmpty(lastName))
        {
            if (regLastNameError != null) regLastNameError.text = "Фамилия обязательна";
            return false;
        }
        if (regLastNameError != null) regLastNameError.text = "";
        return true;
    }
}