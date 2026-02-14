using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Фабрика для создания полей ввода даты с календарем
/// 
/// ПАТТЕРН FACTORY:
/// - Инкапсулирует логику создания полей ввода даты
/// - Предоставляет единый интерфейс для создания различных типов полей
/// - Упрощает управление зависимостями и конфигурацией
/// 
/// АРХИТЕКТУРА:
/// - Создает DateInputField с интегрированным календарем
/// - Настраивает UI элементы и обработчики событий
/// - Предоставляет методы для создания полей с различными настройками
/// 
/// ИСПОЛЬЗОВАНИЕ:
/// - Создание полей ввода даты для форм
/// - Интеграция с существующими UI компонентами
/// - Управление жизненным циклом полей ввода
/// </summary>
public static class DateFieldFactory
{
    /// <summary>
    /// Создает поле ввода даты с календарем
    /// </summary>
    /// <param name="parent">Родительский объект</param>
    /// <param name="fieldPrefab">Префаб поля ввода</param>
    /// <param name="datePickerPrefab">Префаб календаря</param>
    /// <param name="labelText">Текст метки поля</param>
    /// <returns>Созданный компонент DateInputField</returns>
    public static DateInputField CreateDateField(Transform parent, GameObject fieldPrefab, GameObject datePickerPrefab, string labelText = "")
    {
        // Создаем поле ввода
        GameObject fieldObject = Object.Instantiate(fieldPrefab, parent);
        DateInputField dateInputField = fieldObject.GetComponent<DateInputField>();
        
        if (dateInputField == null)
        {
            dateInputField = fieldObject.AddComponent<DateInputField>();
        }

        // Настраиваем метку поля
        if (!string.IsNullOrEmpty(labelText))
        {
            var label = fieldObject.transform.Find("ValueName")?.GetComponent<Text>();
            if (label != null)
            {
                label.text = labelText;
            }
        }

        // Создаем и настраиваем календарь
        SetupCalendar(dateInputField, datePickerPrefab, parent);

        return dateInputField;
    }

    /// <summary>
    /// Создает поле ввода даты с предустановленными настройками
    /// </summary>
    /// <param name="parent">Родительский объект</param>
    /// <param name="fieldPrefab">Префаб поля ввода</param>
    /// <param name="datePickerPrefab">Префаб календаря</param>
    /// <param name="config">Конфигурация поля</param>
    /// <returns>Созданный компонент DateInputField</returns>
    public static DateInputField CreateDateFieldWithConfig(Transform parent, GameObject fieldPrefab, GameObject datePickerPrefab, DateFieldConfig config)
    {
        DateInputField dateInputField = CreateDateField(parent, fieldPrefab, datePickerPrefab, config.LabelText);
        
        // Применяем конфигурацию
        if (config != null)
        {
            ApplyConfiguration(dateInputField, config);
        }

        return dateInputField;
    }

    /// <summary>
    /// Настраивает календарь для поля ввода даты
    /// </summary>
    /// <param name="dateInputField">Компонент поля ввода даты</param>
    /// <param name="datePickerPrefab">Префаб календаря</param>
    /// <param name="parent">Родительский объект</param>
    private static void SetupCalendar(DateInputField dateInputField, GameObject datePickerPrefab, Transform parent)
    {
        if (datePickerPrefab == null) return;

        // Создаем календарь
        GameObject calendarObject = Object.Instantiate(datePickerPrefab, parent);
        DatePicker datePicker = calendarObject.GetComponent<DatePicker>();
        
        if (datePicker == null)
        {
            datePicker = calendarObject.AddComponent<DatePicker>();
        }

        // Настраиваем ссылки в DateInputField
        var inputField = dateInputField.transform.Find("ValueInputField")?.GetComponent<InputField>();
        var calendarButton = dateInputField.transform.Find("CalendarButton")?.GetComponent<Button>();
        var calendarIcon = dateInputField.transform.Find("CalendarIcon")?.GetComponent<Image>();

        // Устанавливаем ссылки через рефлексию или публичные свойства
        SetFieldReference(dateInputField, "dateInputField", inputField);
        SetFieldReference(dateInputField, "calendarButton", calendarButton);
        SetFieldReference(dateInputField, "datePicker", datePicker);
        SetFieldReference(dateInputField, "calendarIcon", calendarIcon);

        // Настраиваем позиционирование календаря
        SetupCalendarPosition(calendarObject, dateInputField.transform);
    }

    /// <summary>
    /// Настраивает позиционирование календаря относительно поля ввода
    /// </summary>
    /// <param name="calendarObject">Объект календаря</param>
    /// <param name="inputFieldTransform">Трансформ поля ввода</param>
    private static void SetupCalendarPosition(GameObject calendarObject, Transform inputFieldTransform)
    {
        RectTransform calendarRect = calendarObject.GetComponent<RectTransform>();
        if (calendarRect != null)
        {
            // Позиционируем календарь под полем ввода
            calendarRect.anchorMin = new Vector2(0, 0);
            calendarRect.anchorMax = new Vector2(1, 0);
            calendarRect.pivot = new Vector2(0.5f, 1);
            
            // Устанавливаем позицию
            Vector3 inputPosition = inputFieldTransform.position;
            calendarRect.position = new Vector3(inputPosition.x, inputPosition.y - 50, inputPosition.z);
            
            // Скрываем календарь по умолчанию
            calendarObject.SetActive(false);
        }
    }

    /// <summary>
    /// Применяет конфигурацию к полю ввода даты
    /// </summary>
    /// <param name="dateInputField">Компонент поля ввода даты</param>
    /// <param name="config">Конфигурация</param>
    private static void ApplyConfiguration(DateInputField dateInputField, DateFieldConfig config)
    {
        // Применяем настройки через рефлексию или публичные свойства
        if (config.InitialDate != null)
        {
            dateInputField.SetDate(config.InitialDate);
        }

        if (config.ReadOnly.HasValue)
        {
            dateInputField.SetReadOnly(config.ReadOnly.Value);
        }

        if (config.Interactable.HasValue)
        {
            dateInputField.SetInteractable(config.Interactable.Value);
        }
    }

    /// <summary>
    /// Устанавливает ссылку на поле через рефлексию
    /// </summary>
    /// <param name="target">Целевой объект</param>
    /// <param name="fieldName">Имя поля</param>
    /// <param name="value">Значение</param>
    private static void SetFieldReference(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(fieldName, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            field.SetValue(target, value);
        }
    }
}

/// <summary>
/// Конфигурация поля ввода даты
/// </summary>
public class DateFieldConfig
{
    public string LabelText { get; set; } = "";
    public string InitialDate { get; set; } = "";
    public bool? ReadOnly { get; set; } = null;
    public bool? Interactable { get; set; } = null;
    public string DateFormat { get; set; } = "yyyy-MM-dd";
    public bool AllowManualInput { get; set; } = true;
    public bool AutoCloseCalendar { get; set; } = true;
} 