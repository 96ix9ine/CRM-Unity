using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.EventSystems;
using Validation.Models;
using Validation.Services;

/// <summary>
/// Рендерер полей для модального окна
/// Отвечает за создание и настройку различных типов полей ввода
/// </summary>
public class FieldRenderer : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private TableEditView m_View;
    [SerializeField] private IFieldHintsService m_FieldHintsService;
    [SerializeField] private IForeignKeyOptionsProvider m_ForeignKeyOptionsProvider;

    // Словари для хранения созданных полей
    private Dictionary<string, InputField> m_InputFields = new Dictionary<string, InputField>();
    private Dictionary<string, TMP_Dropdown> m_DropdownFields = new Dictionary<string, TMP_Dropdown>();
    private Dictionary<string, Dictionary<string, string>> m_DropdownOptions = new Dictionary<string, Dictionary<string, string>>();
    private Dictionary<string, DateInputField> m_DateFields = new Dictionary<string, DateInputField>();
    private List<string> m_FieldsOrder = new List<string>();
    private Dictionary<string, string> m_ImageFieldValues = new Dictionary<string, string>();
    private Dictionary<string, Text> m_ImageFieldTexts = new Dictionary<string, Text>();
    private Dictionary<string, TMP_Text> m_ImageFieldTMPTexts = new Dictionary<string, TMP_Text>();
    private Dictionary<string, RawImage> m_ImageFieldPreviews = new Dictionary<string, RawImage>();

    // Словари для хранения ErrorText компонентов для разных типов полей
    private Dictionary<string, TextMeshProUGUI> m_InputFieldErrorTexts = new Dictionary<string, TextMeshProUGUI>();
    private Dictionary<string, TextMeshProUGUI> m_DropdownErrorTexts = new Dictionary<string, TextMeshProUGUI>();
    private Dictionary<string, TextMeshProUGUI> m_CalendarErrorTexts = new Dictionary<string, TextMeshProUGUI>();
    private Dictionary<string, TextMeshProUGUI> m_UploadErrorTexts = new Dictionary<string, TextMeshProUGUI>();
#if UNITY_EDITOR
    // В редакторе храним путь к выбранному локальному файлу для последующей загрузки внешним кодом
    private Dictionary<string, string> m_PendingLocalImagePaths = new Dictionary<string, string>();
#endif

    #region Public Properties

    public Dictionary<string, InputField> InputFields => m_InputFields;
    public Dictionary<string, TMP_Dropdown> DropdownFields => m_DropdownFields;
    public Dictionary<string, Dictionary<string, string>> DropdownOptions => m_DropdownOptions;
    public Dictionary<string, DateInputField> DateFields => m_DateFields;
    public IReadOnlyList<string> FieldsOrder => m_FieldsOrder;
    public IReadOnlyDictionary<string, string> ImageFieldValues => m_ImageFieldValues;

    // Публичный доступ к ErrorText компонентам для валидации
    public IReadOnlyDictionary<string, TextMeshProUGUI> InputFieldErrorTexts => m_InputFieldErrorTexts;
    public IReadOnlyDictionary<string, TextMeshProUGUI> DropdownErrorTexts => m_DropdownErrorTexts;
    public IReadOnlyDictionary<string, TextMeshProUGUI> CalendarErrorTexts => m_CalendarErrorTexts;
    public IReadOnlyDictionary<string, TextMeshProUGUI> UploadErrorTexts => m_UploadErrorTexts;

    /// <summary>
    /// Получает ErrorText компонент для поля независимо от его типа
    /// </summary>
    public TextMeshProUGUI GetErrorTextForField(string fieldName)
    {
        if (m_InputFieldErrorTexts.TryGetValue(fieldName, out var inputError))
            return inputError;
        if (m_DropdownErrorTexts.TryGetValue(fieldName, out var dropdownError))
            return dropdownError;
        if (m_CalendarErrorTexts.TryGetValue(fieldName, out var calendarError))
            return calendarError;
        if (m_UploadErrorTexts.TryGetValue(fieldName, out var uploadError))
            return uploadError;
        return null;
    }

    #endregion

    #region Initialization

    private void Awake()
    {
        if (m_View == null)
            m_View = GetComponent<TableEditView>();
            
        if (m_FieldHintsService == null)
            m_FieldHintsService = FindAnyObjectByType<FieldHintsService>();
            
        if (m_ForeignKeyOptionsProvider == null)
            m_ForeignKeyOptionsProvider = FindAnyObjectByType<ForeignKeyOptionsRegistry>();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Очищает все созданные поля
    /// </summary>
    public void ClearFields()
    {
        m_InputFields.Clear();
        m_DropdownFields.Clear();
        m_DropdownOptions.Clear();
        m_DateFields.Clear();
        m_FieldsOrder.Clear();
        m_ImageFieldValues.Clear();
        m_ImageFieldTexts.Clear();
        m_ImageFieldTMPTexts.Clear();
        m_ImageFieldPreviews.Clear();

        // Очищаем ErrorText компоненты
        m_InputFieldErrorTexts.Clear();
        m_DropdownErrorTexts.Clear();
        m_CalendarErrorTexts.Clear();
        m_UploadErrorTexts.Clear();

#if UNITY_EDITOR
        m_PendingLocalImagePaths.Clear();
#endif
    }

    /// <summary>
    /// Создает текстовое поле
    /// </summary>
    public void RenderTextField(string fieldName, string displayName, Transform container, string value = "")
    {
        if (string.Equals(fieldName, "id", System.StringComparison.OrdinalIgnoreCase))
            return;
        // Специальная обработка изображений
        if (IsImageField(fieldName))
        {
            RenderImageUploadField(fieldName, displayName, container, value);
            return;
        }
        var fieldObj = m_View.CreateFieldItem(container);
        if (fieldObj == null) return;

        var label = fieldObj.transform.Find("ValueName").GetComponent<Text>();
        var input = fieldObj.transform.Find("ValueInputField").GetComponent<InputField>();
        var dropdown = fieldObj.transform.Find("ValueDropdown").GetComponent<TMP_Dropdown>();
        var valueCalendar = fieldObj.transform.Find("ValueCalendar")?.GetComponent<DateInputField>();

        // Настраиваем лейбл
        if (label != null)
            label.text = displayName;

        // Активируем только текстовое поле
        if (input != null)
        {
            input.gameObject.SetActive(true);
            input.text = value ?? "";

            // Автоматически настраиваем contentType на основе типа поля
            SetupInputFieldType(fieldName, input);

            // Находим существующий ErrorText компонент
            FindAndStoreErrorText(fieldObj, fieldName);

            m_InputFields[fieldName] = input;
        }

        // Деактивируем остальные поля
        if (dropdown != null)
            dropdown.gameObject.SetActive(false);
        if (valueCalendar != null)
            valueCalendar.gameObject.SetActive(false);
        var uploadPanel = fieldObj.transform.Find("ValueUpload");
        if (uploadPanel != null) uploadPanel.gameObject.SetActive(false);

        // Добавляем подсказку
        SetupFieldHint(fieldObj, fieldName);

        // Порядок
        m_FieldsOrder.Add(fieldName);
    }

    /// <summary>
    /// Создает поле выбора (dropdown)
    /// </summary>
    public async Task RenderDropdownField(string fieldName, string displayName, Transform container, 
        Dictionary<string, string> options, string selectedValue = "")
    {
        if (string.Equals(fieldName, "id", System.StringComparison.OrdinalIgnoreCase))
            return;
        var fieldObj = m_View.CreateFieldItem(container);
        if (fieldObj == null) return;

        var label = fieldObj.transform.Find("ValueName").GetComponent<Text>();
        var input = fieldObj.transform.Find("ValueInputField").GetComponent<InputField>();
        var dropdown = fieldObj.transform.Find("ValueDropdown").GetComponent<TMP_Dropdown>();
        var valueCalendar = fieldObj.transform.Find("ValueCalendar")?.GetComponent<DateInputField>();

        // Настраиваем лейбл
        if (label != null)
            label.text = displayName;

        // Активируем только dropdown
        if (dropdown != null)
        {
            dropdown.gameObject.SetActive(true);
            dropdown.ClearOptions();
            dropdown.AddOptions(new List<string>(options.Values));
            m_DropdownOptions[fieldName] = options;
            m_DropdownFields[fieldName] = dropdown;

            // Находим существующий ErrorText компонент
            FindAndStoreErrorText(fieldObj, fieldName);

            // Устанавливаем выбранное значение
            if (!string.IsNullOrEmpty(selectedValue) && options.ContainsKey(selectedValue))
            {
                var keys = new List<string>(options.Keys);
                dropdown.value = keys.IndexOf(selectedValue);
            }
            else
            {
                dropdown.value = 0; // "Не выбрано" по умолчанию
            }
        }

        // Деактивируем остальные поля
        if (input != null)
            input.gameObject.SetActive(false);
        if (valueCalendar != null)
            valueCalendar.gameObject.SetActive(false);
        var uploadPanel = fieldObj.transform.Find("ValueUpload");
        if (uploadPanel != null) uploadPanel.gameObject.SetActive(false);

        // Добавляем подсказку
        SetupFieldHint(fieldObj, fieldName);

        // Порядок
        m_FieldsOrder.Add(fieldName);
    }

    /// <summary>
    /// Создает поле даты
    /// </summary>
    public void RenderDateField(string fieldName, string displayName, Transform container, string value = "")
    {
        if (string.Equals(fieldName, "id", System.StringComparison.OrdinalIgnoreCase))
            return;
        var fieldObj = m_View.CreateFieldItem(container);
        if (fieldObj == null) return;

        var label = fieldObj.transform.Find("ValueName").GetComponent<Text>();
        var input = fieldObj.transform.Find("ValueInputField").GetComponent<InputField>();
        var dropdown = fieldObj.transform.Find("ValueDropdown").GetComponent<TMP_Dropdown>();
        var valueCalendar = fieldObj.transform.Find("ValueCalendar")?.GetComponent<DateInputField>();

        // Настраиваем лейбл
        if (label != null)
            label.text = displayName;

        // Активируем только поле даты
        if (valueCalendar != null)
        {
            valueCalendar.gameObject.SetActive(true);
            valueCalendar.SetDate(value ?? "");
            m_DateFields[fieldName] = valueCalendar;

            // Находим существующий ErrorText компонент
            FindAndStoreErrorText(fieldObj, fieldName);
        }

        // Деактивируем остальные поля
        if (input != null)
            input.gameObject.SetActive(false);
        if (dropdown != null)
            dropdown.gameObject.SetActive(false);
        var uploadPanel = fieldObj.transform.Find("ValueUpload");
        if (uploadPanel != null) uploadPanel.gameObject.SetActive(false);

        // Добавляем подсказку
        SetupFieldHint(fieldObj, fieldName);

        // Порядок
        m_FieldsOrder.Add(fieldName);
    }

    /// <summary>
    /// Ищет существующий ErrorText компонент внутри активного поля (Input, Dropdown, Calendar, Upload) и сохраняет его
    /// </summary>
    private void FindAndStoreErrorText(GameObject fieldObj, string fieldName)
    {
        if (fieldObj == null) return;

        Debug.Log($"🔍 Поиск ErrorText для поля '{fieldName}' в '{fieldObj.name}'");

        // Ищем ErrorText внутри всех возможных типов полей по порядку

        // 1. Ищем внутри ValueInputField
        var inputField = fieldObj.transform.Find("ValueInputField");
        if (inputField != null && inputField.gameObject.activeSelf)
        {
            var errorTextTMP = inputField.Find("ErrorText")?.GetComponent<TextMeshProUGUI>();
            var errorText = inputField.Find("ErrorText")?.GetComponent<Text>();

            if (errorTextTMP != null || errorText != null)
            {
                Debug.Log($"✅ Найден и сохранен ErrorText для поля '{fieldName}' внутри ValueInputField");
                if (errorTextTMP != null) m_InputFieldErrorTexts[fieldName] = errorTextTMP;
                HideErrorText(errorTextTMP, errorText);
                return;
            }
        }

        // 2. Ищем внутри ValueDropdown
        var dropdown = fieldObj.transform.Find("ValueDropdown");
        if (dropdown != null && dropdown.gameObject.activeSelf)
        {
            var errorTextTMP = dropdown.Find("ErrorText")?.GetComponent<TextMeshProUGUI>();
            var errorText = dropdown.Find("ErrorText")?.GetComponent<Text>();

            if (errorTextTMP != null || errorText != null)
            {
                Debug.Log($"✅ Найден и сохранен ErrorText для поля '{fieldName}' внутри ValueDropdown");
                if (errorTextTMP != null) m_DropdownErrorTexts[fieldName] = errorTextTMP;
                HideErrorText(errorTextTMP, errorText);
                return;
            }
        }

        // 3. Ищем внутри ValueCalendar
        var calendar = fieldObj.transform.Find("ValueCalendar");
        if (calendar != null && calendar.gameObject.activeSelf)
        {
            var errorTextTMP = calendar.Find("ErrorText")?.GetComponent<TextMeshProUGUI>();
            var errorText = calendar.Find("ErrorText")?.GetComponent<Text>();

            if (errorTextTMP != null || errorText != null)
            {
                Debug.Log($"✅ Найден и сохранен ErrorText для поля '{fieldName}' внутри ValueCalendar");
                if (errorTextTMP != null) m_CalendarErrorTexts[fieldName] = errorTextTMP;
                HideErrorText(errorTextTMP, errorText);
                return;
            }
        }

        // 4. Ищем внутри ValueUpload
        var upload = fieldObj.transform.Find("ValueUpload");
        if (upload != null && upload.gameObject.activeSelf)
        {
            var errorTextTMP = upload.Find("ErrorText")?.GetComponent<TextMeshProUGUI>();
            var errorText = upload.Find("ErrorText")?.GetComponent<Text>();

            if (errorTextTMP != null || errorText != null)
            {
                Debug.Log($"✅ Найден и сохранен ErrorText для поля '{fieldName}' внутри ValueUpload");
                if (errorTextTMP != null) m_UploadErrorTexts[fieldName] = errorTextTMP;
                HideErrorText(errorTextTMP, errorText);
                return;
            }
        }

        // 5. Ищем ErrorText в других местах (fallback) - рекурсивный поиск
        var errorTextTMP_Fallback = FindErrorTextRecursive(fieldObj.transform);
        var errorText_Fallback = errorTextTMP_Fallback == null ? FindErrorTextLegacy(fieldObj.transform) : null;

        if (errorTextTMP_Fallback != null || errorText_Fallback != null)
        {
            Debug.Log($"✅ Найден и сохранен ErrorText для поля '{fieldName}' рекурсивным поиском");
            if (errorTextTMP_Fallback != null)
            {
                // Определяем тип поля по активному компоненту и сохраняем в соответствующий словарь
                if (inputField != null && inputField.gameObject.activeSelf)
                    m_InputFieldErrorTexts[fieldName] = errorTextTMP_Fallback;
                else if (dropdown != null && dropdown.gameObject.activeSelf)
                    m_DropdownErrorTexts[fieldName] = errorTextTMP_Fallback;
                else if (calendar != null && calendar.gameObject.activeSelf)
                    m_CalendarErrorTexts[fieldName] = errorTextTMP_Fallback;
                else if (upload != null && upload.gameObject.activeSelf)
                    m_UploadErrorTexts[fieldName] = errorTextTMP_Fallback;
                else
                    m_InputFieldErrorTexts[fieldName] = errorTextTMP_Fallback; // по умолчанию
            }
            HideErrorText(errorTextTMP_Fallback, errorText_Fallback);
        }
        else
        {
            Debug.LogWarning($"⚠️ ErrorText для поля '{fieldName}' не найден ни в одном из полей");
        }
    }

    /// <summary>
    /// Скрывает ErrorText компоненты
    /// </summary>
    private void HideErrorText(TextMeshProUGUI errorTextTMP, Text errorText)
    {
        if (errorTextTMP != null)
        {
            errorTextTMP.gameObject.SetActive(false);
        }
        else if (errorText != null)
        {
            errorText.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// Рекурсивно ищет ErrorText компонент в иерархии
    /// </summary>
    private TextMeshProUGUI FindErrorTextRecursive(Transform parent)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);

            // Проверяем сам объект
            if (child.name.Contains("ErrorText") || child.name.Contains("error"))
            {
                var errorTextTMP = child.GetComponent<TextMeshProUGUI>();
                if (errorTextTMP != null)
                {
                    return errorTextTMP;
                }
            }

            // Проверяем прямые дочерние элементы
            var directErrorText = child.Find("ErrorText")?.GetComponent<TextMeshProUGUI>();
            if (directErrorText != null)
            {
                return directErrorText;
            }

            // Рекурсивный поиск в дочерних элементах
            var recursiveErrorText = FindErrorTextRecursive(child);
            if (recursiveErrorText != null)
            {
                return recursiveErrorText;
            }
        }

        return null;
    }

    /// <summary>
    /// Поиск ErrorText для старых Text компонентов (legacy)
    /// </summary>
    private Text FindErrorTextLegacy(Transform parent)
    {
        for (int i = 0; i < parent.childCount; i++)
        {
            var child = parent.GetChild(i);

            // Проверяем сам объект
            if (child.name.Contains("ErrorText") || child.name.Contains("error"))
            {
                var errorText = child.GetComponent<Text>();
                if (errorText != null)
                {
                    return errorText;
                }
            }

            // Проверяем прямые дочерние элементы
            var directErrorText = child.Find("ErrorText")?.GetComponent<Text>();
            if (directErrorText != null)
            {
                return directErrorText;
            }

            // Рекурсивный поиск в дочерних элементах
            var recursiveErrorText = FindErrorTextLegacy(child);
            if (recursiveErrorText != null)
            {
                return recursiveErrorText;
            }
        }

        return null;
    }

    /// <summary>
    /// Создает поле загрузки/выбора изображения: показывает панель ValueUpload с кнопками
    /// </summary>
    public void RenderImageUploadField(string fieldName, string displayName, Transform container, string value = "")
    {
        var fieldObj = m_View.CreateFieldItem(container);
        if (fieldObj == null) return;

        var label = fieldObj.transform.Find("ValueName")?.GetComponent<Text>();
        var input = fieldObj.transform.Find("ValueInputField")?.GetComponent<InputField>();
        var dropdown = fieldObj.transform.Find("ValueDropdown")?.GetComponent<TMP_Dropdown>();
        var valueCalendar = fieldObj.transform.Find("ValueCalendar")?.GetComponent<DateInputField>();

        if (label != null) label.text = displayName;

        // Деактивируем стандартные инпуты
        if (input != null) input.gameObject.SetActive(false);
        if (dropdown != null) dropdown.gameObject.SetActive(false);
        if (valueCalendar != null) valueCalendar.gameObject.SetActive(false);

        // Активируем панель загрузки
        var uploadPanel = fieldObj.transform.Find("ValueUpload");
        if (uploadPanel == null)
        {
            Debug.LogWarning("Field prefab не содержит ValueUpload панели для image-поля");
            return;
        }
        uploadPanel.gameObject.SetActive(true);

        var selectButton = uploadPanel.Find("ValueSelectButton")?.GetComponent<Button>();
        var uploadButton = uploadPanel.Find("ValueUploadButton")?.GetComponent<Button>();
        var valueTextTMP = uploadPanel.Find("ValueText")?.GetComponent<TMP_Text>();
        var valueText = uploadPanel.Find("ValueText")?.GetComponent<Text>();
        var preview = uploadPanel.Find("ValuePreview")?.GetComponent<RawImage>();
        if (preview != null)
        {
            preview.texture = null;
            preview.color = new Color(1,1,1,0); // скрыть, пока нет текстуры
            m_ImageFieldPreviews[fieldName] = preview;
        }

        if (valueTextTMP != null)
        {
            valueTextTMP.gameObject.SetActive(false);
            m_ImageFieldTMPTexts[fieldName] = valueTextTMP;
        }
        else if (valueText != null)
        {
            valueText.gameObject.SetActive(false);
            m_ImageFieldTexts[fieldName] = valueText;
        }

        // Инициализируем значение, если пришло
        if (!string.IsNullOrEmpty(value))
        {
            m_ImageFieldValues[fieldName] = value;
            ShowImageLabel(fieldName, value);
        }

        // Попробуем подтянуть текущее изображение для записи (по table/id)
        TryLoadCurrentImagePreview(fieldName);

        if (selectButton != null)
        {
            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() =>
            {
                // Открываем модалку выбора фото и подписываемся на подтверждение
                var picker = m_View != null ? m_View.FindPhotoPicker() : null;
                if (picker != null)
                {
                    // Сначала отписка, потом подписка (чтобы не накапливать обработчики)
                    picker.OnPhotosConfirmed -= OnPhotosConfirmedHandler;
                    OnPhotosConfirmedHandler = (list) =>
                    {
                        if (list != null && list.Count > 0)
                        {
                            ApplyPickedImageFromBackend(fieldName, list[0].fileName, list[0].preview);
                        }
                        // одноразовый хэндлер
                        picker.OnPhotosConfirmed -= OnPhotosConfirmedHandler;
                        OnPhotosConfirmedHandler = null;
                    };
                    picker.OnPhotosConfirmed += OnPhotosConfirmedHandler;

                    // определим контекст targetType/targetId из контроллера таблиц, чтобы загрузить картинки привязанные к записи
                    var tableCtrl = GetComponent<TableEditController>();
                    string tgtType = tableCtrl != null ? tableCtrl.GetCurrentTableName() : null;
                    string tgtId = null;
                    if (tableCtrl != null)
                    {
                        var init = tableCtrl.GetInitialData();
                        if (init != null && init.ContainsKey("id") && init["id"] != null)
                            tgtId = init["id"].ToString();
                    }
                    picker.Open(tgtType, tgtId);
                }
                else
                {
                    OnSelectImageRequested?.Invoke(fieldName);
                }
            });
        }

        if (uploadButton != null)
        {
            uploadButton.onClick.RemoveAllListeners();
            uploadButton.onClick.AddListener(() => HandleLocalImagePick(fieldName));
        }

        // Подключим кнопку удаления, если есть
        var deleteButton = uploadPanel.Find("ValueDeleteButton")?.GetComponent<Button>();
        if (deleteButton != null)
        {
            deleteButton.onClick.RemoveAllListeners();
            deleteButton.onClick.AddListener(() => {
                ApplyPickedImageFromBackend(fieldName, null, null);
                // Сброс значения в форме при сохранении приведёт к null в таблице; images не трогаем глобально
                StartCoroutine(ClearImageFieldAndLinks(fieldName));
            });
        }

        // Находим существующий ErrorText компонент
        FindAndStoreErrorText(fieldObj, fieldName);

        // Подсказки
        SetupFieldHint(fieldObj, fieldName);

        m_FieldsOrder.Add(fieldName);
    }

    /// <summary>
    /// Строит Tab-навигацию по полям: Tab — вниз, Shift+Tab — вверх.
    /// Работает по порядку добавления полей.
    /// </summary>
    public void BuildTabNavigation()
    {
        var widgets = new List<Selectable>();
        foreach (var name in m_FieldsOrder)
        {
            if (m_InputFields.TryGetValue(name, out var input) && input != null)
                widgets.Add(input);
            else if (m_DropdownFields.TryGetValue(name, out var dd) && dd != null)
                widgets.Add(dd);
            else if (m_DateFields.TryGetValue(name, out var date) && date != null)
                widgets.Add(date.GetComponent<Selectable>());
        }

        for (int i = 0; i < widgets.Count; i++)
        {
            var nav = new Navigation { mode = Navigation.Mode.Explicit };
            nav.selectOnDown = widgets[(i + 1) % widgets.Count];
            nav.selectOnUp = widgets[(i - 1 + widgets.Count) % widgets.Count];
            widgets[i].navigation = nav;
        }
    }

    /// <summary>
    /// Возвращает текущий порядок Selectable-элементов для таб-навигации.
    /// </summary>
    public List<Selectable> GetTabOrder()
    {
        var widgets = new List<Selectable>();
        foreach (var name in m_FieldsOrder)
        {
            if (m_InputFields.TryGetValue(name, out var input) && input != null)
                widgets.Add(input);
            else if (m_DropdownFields.TryGetValue(name, out var dd) && dd != null)
                widgets.Add(dd);
            else if (m_DateFields.TryGetValue(name, out var date) && date != null)
            {
                var sel = date.GetComponent<Selectable>();
                if (sel != null) widgets.Add(sel);
            }
        }
        return widgets;
    }

    /// <summary>
    /// Создает dropdown поле с универсальной поддержкой опции "Не выбрано"
    /// </summary>
    public async Task CreateDropdownField(string fieldName, string displayName, Transform container, 
        bool isEdit = false, Dictionary<string, object> data = null, 
        System.Func<Task<Dictionary<string, string>>> optionsProvider = null)
    {
        // Загружаем опции
        Dictionary<string, string> options;
        if (optionsProvider != null)
        {
            options = await optionsProvider();
        }
        else if (m_ForeignKeyOptionsProvider != null)
        {
            // Используем универсальный метод загрузки FK опций
            options = await m_ForeignKeyOptionsProvider.GetOptionsAsync("", fieldName);
        }
        else
        {
            options = new Dictionary<string, string>();
        }

        // Добавляем опцию "Не выбрано"
        options = AddNoParentOption(options);

        // Устанавливаем выбранное значение
        string selectedId = null;
        if (isEdit && data != null && data.ContainsKey(fieldName))
            selectedId = data[fieldName]?.ToString();

        await RenderDropdownField(fieldName, displayName, container, options, selectedId);
    }

    #endregion

    #region Server-side image clearing
    private IEnumerator ClearImageFieldAndLinks(string fieldName)
    {
        // Пытаемся получить контекст текущей записи (таблица и id)
        var tableCtrl = GetComponent<TableEditController>();
        if (tableCtrl == null) yield break;
        string tableName = tableCtrl.GetCurrentTableName();
        var init = tableCtrl.GetInitialData();
        if (string.IsNullOrEmpty(tableName) || init == null || !init.ContainsKey("id") || init["id"] == null)
            yield break; // нет контекста — только UI очистка

        string id = init["id"].ToString();
        var api = FindAnyObjectByType<TableApiService>();
        if (api == null) yield break;

        // 1) Обновляем поле записи до null
        var payload = new Dictionary<string, object> { { fieldName, null } };
        bool updateDone = false; bool updateOk = false;
        yield return StartCoroutine(api.UpdateRecord(tableName, id, payload, (ok) => { updateOk = ok; updateDone = true; }));

        // 2) Получаем список изображений по target и для каждого вызываем PATCH updateImage с targetId=0
        List<Dictionary<string, object>> images = null;
        yield return StartCoroutine(api.GetJson<List<Dictionary<string, object>>>($"/images/byTarget/{tableName}/{id}", (list) => images = list));
        if (images != null)
        {
            foreach (var row in images)
            {
                if (row != null && row.ContainsKey("id") && row["id"] != null)
                {
                    var imageId = row["id"].ToString();
                    var form = new WWWForm();
                    form.AddField("targetType", tableName);
                    form.AddField("targetId", "0");
                    bool ok = false; string err = null;
                    yield return StartCoroutine(api.PatchForm($"/images/{imageId}", form, (succ, e) => { ok = succ; err = e; }));
                    if (!ok) Debug.LogWarning($"Patch image detach failed for {imageId}: {err}");
                }
            }
        }
    }
    #endregion

    #region Private Methods

    /// <summary>
    /// Настраивает подсказку для поля
    /// </summary>
    private async void SetupFieldHint(GameObject fieldObj, string fieldName)
    {
        if (fieldObj == null || fieldObj.Equals(null)) return; // обработка MissingReference

        var valueNameTr = fieldObj.transform.Find("ValueName");
        var valueName = valueNameTr != null ? valueNameTr.GetComponent<Text>() : null;
        var infoButtonTr = fieldObj.transform.Find("ValueName/NameInfoButton");
        var infoButton = infoButtonTr != null ? infoButtonTr.gameObject : null;
        var infoImageTr = infoButtonTr != null ? infoButtonTr.Find("Image") : null;
        var infoImage = infoImageTr != null ? infoImageTr.gameObject : null;
        var infoTextTr = infoImageTr != null ? infoImageTr.Find("NameInfoText") : null;
        var infoText = infoTextTr != null ? infoTextTr.GetComponent<Text>() : null;

        if (infoButton != null && !infoButton.Equals(null) && infoImage != null && !infoImage.Equals(null) && infoText != null && !infoText.gameObject.Equals(null))
        {
            infoButton.SetActive(false);
            infoImage.SetActive(false);
            infoText.gameObject.SetActive(false);

            if (m_FieldHintsService != null)
            {
                var fieldHints = await m_FieldHintsService.GetFieldHintsAsync();
                // После await могли уничтожить UI — повторная проверка
                if (fieldObj == null || fieldObj.Equals(null)) return;
                if (infoButton == null || infoButton.Equals(null) || infoImage == null || infoImage.Equals(null) || infoText == null || infoText.gameObject == null || infoText.gameObject.Equals(null)) return;
                if (fieldHints != null && fieldHints.ContainsKey(fieldName))
                {
                    infoButton.SetActive(true);
                    infoText.text = fieldHints[fieldName];

                    // Добавляем EventTrigger для hover/click
                    var trigger = infoButton.GetComponent<UnityEngine.EventSystems.EventTrigger>();
                    if (trigger == null)
                    {
                        // Проверим ещё раз, что объект живой
                        if (infoButton == null || infoButton.Equals(null)) return;
                        trigger = infoButton.AddComponent<UnityEngine.EventSystems.EventTrigger>();
                        if (trigger == null) return;
                    }
                    trigger.triggers.Clear();

                    // Наведение мыши — показать
                    var entryEnter = new UnityEngine.EventSystems.EventTrigger.Entry { 
                        eventID = UnityEngine.EventSystems.EventTriggerType.PointerEnter };
                    entryEnter.callback.AddListener((eventData) => {
                        if (infoImage != null && !infoImage.Equals(null)) infoImage.SetActive(true);
                        if (infoText != null && infoText.gameObject != null && !infoText.gameObject.Equals(null)) infoText.gameObject.SetActive(true);
                    });
                    trigger.triggers.Add(entryEnter);

                    // Уход мыши — скрыть
                    var entryExit = new UnityEngine.EventSystems.EventTrigger.Entry { 
                        eventID = UnityEngine.EventSystems.EventTriggerType.PointerExit };
                    entryExit.callback.AddListener((eventData) => {
                        if (infoImage != null && !infoImage.Equals(null)) infoImage.SetActive(false);
                        if (infoText != null && infoText.gameObject != null && !infoText.gameObject.Equals(null)) infoText.gameObject.SetActive(false);
                    });
                    trigger.triggers.Add(entryExit);
                }
            }
        }
    }

    /// <summary>
    /// Добавляет опцию "Не выбрано" в начало словаря опций
    /// </summary>
    private Dictionary<string, string> AddNoParentOption(Dictionary<string, string> options)
    {
        var result = new Dictionary<string, string>();
        result[""] = "Не выбрано";

        foreach (var kvp in options)
        {
            result[kvp.Key] = kvp.Value;
        }

        return result;
    }

    /// <summary>
    /// Настраивает тип контента InputField на основе названия поля
    /// </summary>
    private void SetupInputFieldType(string fieldName, InputField inputField)
    {
        if (inputField == null) return;

        var fieldType = FieldTypeResolver.GetFieldType(fieldName);
        var contentType = GetContentTypeForFieldType(fieldType);

        inputField.contentType = contentType;

        // Для большинства полей устанавливаем однострочный режим
        if (fieldType != FieldType.String || !IsMultilineField(fieldName))
        {
            inputField.lineType = InputField.LineType.SingleLine;
        }
        else
        {
            inputField.lineType = InputField.LineType.MultiLineNewline;
        }
    }

    /// <summary>
    /// Получает тип контента InputField для типа поля
    /// </summary>
    private InputField.ContentType GetContentTypeForFieldType(FieldType fieldType)
    {
        switch (fieldType)
        {
            case FieldType.Integer:
                return InputField.ContentType.IntegerNumber;
            case FieldType.Float:
            case FieldType.Decimal:
                return InputField.ContentType.DecimalNumber;
            case FieldType.Email:
                return InputField.ContentType.EmailAddress;
            case FieldType.Phone:
                return InputField.ContentType.Name; // Для телефонов используем alphanumeric
            case FieldType.Inn:
            case FieldType.Kpp:
            case FieldType.Ogrn:
                return InputField.ContentType.IntegerNumber;
            case FieldType.Url:
                return InputField.ContentType.Name;
            case FieldType.Date:
            case FieldType.Time:
            case FieldType.DateTime:
                return InputField.ContentType.Alphanumeric; // Разрешаем цифры и разделители
            default:
                return InputField.ContentType.Standard;
        }
    }

    /// <summary>
    /// Проверяет, должно ли поле быть многострочным
    /// </summary>
    private bool IsMultilineField(string fieldName)
    {
        if (string.IsNullOrEmpty(fieldName)) return false;

        var lowerName = fieldName.ToLower();
        return lowerName.Contains("comment") ||
               lowerName.Contains("description") ||
               lowerName.Contains("address") ||
               lowerName.Contains("note");
    }

    private bool IsImageField(string fieldName)
    {
        if (string.IsNullOrEmpty(fieldName)) return false;
        var lower = fieldName.ToLower();
        return lower == "photo" || lower == "icon" || lower == "image" || lower == "specificationImage";
    }

    private void HandleLocalImagePick(string fieldName)
    {
#if CT_FILEBROWSER
        var filter = new Crosstales.FB.ExtensionFilter("Images", new string[] {"png","jpg","jpeg"});
        string path = Crosstales.FB.FileBrowser.Instance.OpenSingleFile("Выберите изображение", string.Empty, "png", new Crosstales.FB.ExtensionFilter[] { filter });
        if (!string.IsNullOrEmpty(path))
        {
            ApplyPickedLocalImage(fieldName, path);
        }
#elif UNITY_EDITOR
        string path = UnityEditor.EditorUtility.OpenFilePanel("Выберите изображение", "", "png,jpg,jpeg");
        if (!string.IsNullOrEmpty(path))
        {
            ApplyPickedLocalImage(fieldName, path);
        }
#else
        Debug.Log("Файловый диалог не подключён. Добавьте CT_FILEBROWSER (Crosstales) или используйте EditorUtility в редакторе.");
#endif
    }


    private void ApplyPickedLocalImage(string fieldName, string path)
    {
        if (string.IsNullOrEmpty(path)) return;
        string fileName = System.IO.Path.GetFileName(path);
        m_ImageFieldValues[fieldName] = fileName;
        ShowImageLabel(fieldName, fileName);
#if UNITY_EDITOR
        m_PendingLocalImagePaths[fieldName] = path;
#endif
        // Предпросмотр: попробуем загрузить текстуру из файла
        TryShowPreview(fieldName, path);
        OnLocalImagePicked?.Invoke(fieldName, path, fileName);
    }

    private void ShowImageLabel(string fieldName, string label)
    {
        if (m_ImageFieldTMPTexts.TryGetValue(fieldName, out var tmp) && tmp != null)
        {
            tmp.text = label;
            tmp.gameObject.SetActive(true);
            return;
        }
        if (m_ImageFieldTexts.TryGetValue(fieldName, out var txt) && txt != null)
        {
            txt.text = label;
            txt.gameObject.SetActive(true);
        }
    }

    private async void TryShowPreview(string fieldName, string localPath)
    {
        try
        {
            if (string.IsNullOrEmpty(localPath) || !System.IO.File.Exists(localPath)) return;
            byte[] bytes = System.IO.File.ReadAllBytes(localPath);
            if (bytes == null || bytes.Length == 0) return;
            var tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (tex.LoadImage(bytes))
            {
                if (m_ImageFieldPreviews.TryGetValue(fieldName, out var preview) && preview != null)
                {
                    preview.texture = tex;
                    var c = preview.color; c.a = 1f; preview.color = c;
                }
            }
        }
        catch { }
    }

    #endregion

    #region Events & External API
    public event System.Action<string> OnSelectImageRequested;
    public event System.Action<string, string, string> OnLocalImagePicked; // fieldName, fullPath, fileName
    private System.Action<System.Collections.Generic.List<(string fileName, Texture2D preview)>> OnPhotosConfirmedHandler;

    public bool TryGetImageFieldValue(string fieldName, out string fileName)
        => m_ImageFieldValues.TryGetValue(fieldName, out fileName);

#if UNITY_EDITOR
    public bool TryGetPendingLocalImagePath(string fieldName, out string fullPath)
        => m_PendingLocalImagePaths.TryGetValue(fieldName, out fullPath);
#endif
    #endregion

    #region External helpers for integration
    public void ApplyPickedImageFromBackend(string fieldName, string fileName, Texture2D previewTexture)
    {
        if (string.IsNullOrEmpty(fieldName)) return;
        if (string.IsNullOrEmpty(fileName))
        {
            // очистка
            m_ImageFieldValues.Remove(fieldName);
            ShowImageLabel(fieldName, "");
            if (m_ImageFieldPreviews.TryGetValue(fieldName, out var prev0) && prev0 != null)
            {
                prev0.texture = null; var c0 = prev0.color; c0.a = 0f; prev0.color = c0;
            }
            return;
        }
        m_ImageFieldValues[fieldName] = fileName;
        ShowImageLabel(fieldName, fileName);
        if (previewTexture != null && m_ImageFieldPreviews.TryGetValue(fieldName, out var preview) && preview != null)
        {
            preview.texture = previewTexture;
            var c = preview.color; c.a = 1f; preview.color = c;
        }
    }
    
    private void TryLoadCurrentImagePreview(string fieldName)
    {
        var tableCtrl = GetComponent<TableEditController>();
        if (tableCtrl == null) return;
        var init = tableCtrl.GetInitialData();
        string tgtType = tableCtrl.GetCurrentTableName();
        if (init == null || !init.ContainsKey("id") || init["id"] == null || string.IsNullOrEmpty(tgtType)) return;
        string tgtId = init["id"].ToString();
        StartCoroutine(LoadFirstImagePreview(fieldName, tgtType, tgtId));
    }

    private IEnumerator LoadFirstImagePreview(string fieldName, string targetType, string targetId)
    {
        var api = FindAnyObjectByType<TableApiService>();
        if (api == null) yield break;
        List<Dictionary<string, object>> images = null;
        yield return api.GetJson<List<Dictionary<string, object>>>($"/images/byTarget/{targetType}/{targetId}", (list) => images = list);
        if (images == null || images.Count == 0) yield break;
        var first = images[0];
        if (first == null) yield break;
        string id = first.ContainsKey("id") && first["id"] != null ? first["id"].ToString() : null;
        byte[] bytes = null;
        if (!string.IsNullOrEmpty(id))
        {
            yield return api.GetBytes($"/images/{id}", (b) => bytes = b);
        }
        if (bytes == null || bytes.Length == 0) yield break;
        var tex = new Texture2D(2,2,TextureFormat.RGBA32,false);
        if (tex.LoadImage(bytes))
        {
            if (m_ImageFieldPreviews.TryGetValue(fieldName, out var preview) && preview != null)
            {
                preview.texture = tex; var c = preview.color; c.a = 1f; preview.color = c;
            }
        }
    }
    #endregion
}
