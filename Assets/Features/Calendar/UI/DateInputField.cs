using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Globalization;

/// <summary>
/// Компонент поля ввода даты с интегрированным календарем
/// 
/// АРХИТЕКТУРА:
/// - Композитный компонент, объединяющий InputField и DatePicker
/// - Делегирует логику календаря компоненту DatePicker
/// - Предоставляет единый интерфейс для работы с датами
/// 
/// ФУНКЦИОНАЛЬНОСТЬ:
/// - Поле ввода с кнопкой календаря
/// - Валидация формата даты
/// - Автоматическое форматирование в yyyy-MM-dd
/// - События для уведомления об изменении даты
/// - Поддержка ручного ввода и выбора через календарь
/// 
/// ИСПОЛЬЗОВАНИЕ:
/// - Подключить к префабу поля ввода
/// - Подписаться на событие OnDateChanged
/// - Получить значение через GetDateValue()
/// </summary>
public class DateInputField : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private InputField dateInputField;
    [SerializeField] private Button calendarButton;
    [SerializeField] private DatePicker datePicker;
    [SerializeField] private Image calendarIcon;

    [Header("Settings")]
    [SerializeField] private string dateFormat = "yyyy-MM-dd";
    [SerializeField] private bool allowManualInput = true;
    [SerializeField] private bool autoCloseCalendar = true;

    /// <summary>
    /// Событие изменения даты
    /// Параметр: отформатированная дата в формате yyyy-MM-dd
    /// </summary>
    public event Action<string> OnDateChanged;

    /// <summary>
    /// Событие валидации даты
    /// Параметр: true, если дата валидна
    /// </summary>
    public event Action<bool> OnDateValidationChanged;

    private bool isInitialized = false;
    private string currentDateValue = "";

    /// <summary>
    /// Инициализация компонента
    /// </summary>
    private void Start()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Инициализация компонента
    /// Настраивает обработчики событий и валидацию
    /// </summary>
    private void InitializeComponent()
    {
        if (isInitialized) return;

        // Настраиваем обработчики событий
        if (dateInputField != null)
        {
            dateInputField.onValueChanged.AddListener(OnInputFieldChanged);
            dateInputField.onEndEdit.AddListener(OnInputFieldEndEdit);
        }

        if (calendarButton != null)
        {
            calendarButton.onClick.AddListener(OpenCalendar);
        }

        // Убираем подписку на события datePicker, теперь всё через CalendarService
        // if (datePicker != null)
        // {
        //     datePicker.OnDateSelected += OnDatePickerSelected;
        //     datePicker.OnCalendarClosed += OnCalendarClosed;
        // }

        isInitialized = true;
    }

    /// <summary>
    /// Обработчик изменения значения в поле ввода
    /// </summary>
    /// <param name="value">Новое значение поля</param>
    private void OnInputFieldChanged(string value)
    {
        if (!allowManualInput) return;

        // Проверяем валидность введенной даты
        bool isValid = IsValidDate(value);
        OnDateValidationChanged?.Invoke(isValid);

        // Обновляем визуальное состояние
        UpdateVisualState(isValid);
    }

    /// <summary>
    /// Обработчик завершения редактирования поля ввода
    /// </summary>
    /// <param name="value">Финальное значение поля</param>
    private void OnInputFieldEndEdit(string value)
    {
        if (!allowManualInput) return;

        // Форматируем дату, если она валидна
        if (IsValidDate(value))
        {
            string formattedDate = FormatDate(value);
            dateInputField.text = formattedDate;
            currentDateValue = formattedDate;
            OnDateChanged?.Invoke(formattedDate);
        }
        else
        {
            // Возвращаем предыдущее значение, если введенная дата невалидна
            dateInputField.text = currentDateValue;
        }
    }

    /// <summary>
    /// Обработчик выбора даты в календаре
    /// </summary>
    /// <param name="selectedDate">Выбранная дата в формате yyyy-MM-dd</param>
    private void OnDatePickerSelected(string selectedDate)
    {
        dateInputField.text = selectedDate;
        currentDateValue = selectedDate;
        OnDateChanged?.Invoke(selectedDate);
        OnDateValidationChanged?.Invoke(true);
        if (autoCloseCalendar && datePicker != null)
            datePicker.CloseCalendar();
    }

    /// <summary>
    /// Обработчик закрытия календаря
    /// </summary>
    private void OnCalendarClosed()
    {
        // Можно добавить дополнительную логику при закрытии календаря
    }

    /// <summary>
    /// Открывает календарь для выбора даты
    /// </summary>
    public void OpenCalendar()
    {
        // Используем CalendarService для открытия календаря только для этого поля
        DateTime? currentDate = null;
        if (!string.IsNullOrEmpty(currentDateValue))
        {
            if (DateTime.TryParseExact(currentDateValue, dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
            {
                currentDate = parsedDate;
            }
        }
        // Если даты нет, currentDate останется null, календарь откроется на сегодня
        CalendarService.Instance.OpenCalendarForField(this, currentDate);
    }

    /// <summary>
    /// Устанавливает значение даты
    /// </summary>
    /// <param name="dateString">Дата в формате yyyy-MM-dd</param>
    public void SetDate(string dateString)
    {
        if (IsValidDate(dateString))
        {
            string formattedDate = FormatDate(dateString);
            dateInputField.text = formattedDate;
            currentDateValue = formattedDate;
            OnDateChanged?.Invoke(formattedDate);
            OnDateValidationChanged?.Invoke(true);
        }
        else if (!string.IsNullOrEmpty(dateString))
        {
            // Если дата невалидная, но не пустая - всё равно сохраняем значение для последующей обработки
            currentDateValue = dateString;
            dateInputField.text = dateString;
            OnDateValidationChanged?.Invoke(false);
            Debug.LogWarning($"<color=orange>⚠️ Установлена невалидная дата: '{dateString}'</color>");
        }
        else
        {
            // Очищаем значение если передана пустая строка
            currentDateValue = "";
            dateInputField.text = "";
            OnDateChanged?.Invoke("");
            OnDateValidationChanged?.Invoke(false);
        }
    }

    /// <summary>
    /// Получает текущее значение даты
    /// </summary>
    /// <returns>Дата в формате yyyy-MM-dd или пустая строка</returns>
    public string GetDateValue()
    {
        // Приоритет: сначала проверяем значение из поля ввода, затем внутреннее значение
        if (dateInputField != null && !string.IsNullOrEmpty(dateInputField.text))
        {
            // Если текст в поле ввода валиден и отличается от внутреннего значения - обновляем внутреннее значение
            if (IsValidDate(dateInputField.text))
            {
                string formattedDate = FormatDate(dateInputField.text);
                if (formattedDate != currentDateValue)
                {
                    currentDateValue = formattedDate;
                    OnDateChanged?.Invoke(formattedDate);
                }
                return formattedDate;
            }
        }

        // Если поле ввода пусто или невалидно, возвращаем внутреннее значение
        return currentDateValue ?? "";
    }

    /// <summary>
    /// Очищает поле ввода даты
    /// </summary>
    public void Clear()
    {
        dateInputField.text = "";
        currentDateValue = "";
        OnDateChanged?.Invoke("");
        OnDateValidationChanged?.Invoke(false);
    }

    /// <summary>
    /// Проверяет валидность даты
    /// </summary>
    /// <param name="dateString">Строка с датой для проверки</param>
    /// <returns>true, если дата валидна</returns>
    private bool IsValidDate(string dateString)
    {
        if (string.IsNullOrEmpty(dateString)) return false;

        return DateTime.TryParseExact(dateString, dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);
    }

    /// <summary>
    /// Форматирует дату в стандартный формат
    /// </summary>
    /// <param name="dateString">Строка с датой</param>
    /// <returns>Отформатированная дата</returns>
    private string FormatDate(string dateString)
    {
        if (DateTime.TryParseExact(dateString, dateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
        {
            return date.ToString(dateFormat);
        }
        return dateString;
    }

    /// <summary>
    /// Обновляет визуальное состояние поля ввода
    /// </summary>
    /// <param name="isValid">Валидна ли дата</param>
    private void UpdateVisualState(bool isValid)
    {
        var inputFieldImage = dateInputField.GetComponent<Image>();
        if (inputFieldImage != null)
        {
            inputFieldImage.color = isValid ? Color.white : Color.red;
        }
    }

    /// <summary>
    /// Устанавливает доступность поля ввода
    /// </summary>
    /// <param name="interactable">Доступно ли поле</param>
    public void SetInteractable(bool interactable)
    {
        dateInputField.interactable = interactable;
        calendarButton.interactable = interactable;
    }

    /// <summary>
    /// Устанавливает режим только для чтения
    /// </summary>
    /// <param name="readOnly">Только для чтения</param>
    public void SetReadOnly(bool readOnly)
    {
        dateInputField.readOnly = readOnly;
        calendarButton.interactable = !readOnly;
    }

    /// <summary>
    /// Получает компонент InputField для совместимости
    /// </summary>
    /// <returns>Компонент InputField</returns>
    public InputField GetInputField()
    {
        return dateInputField;
    }
} 