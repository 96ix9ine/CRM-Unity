using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Globalization;

/// <summary>
/// Компонент календаря для выбора дат
/// Архитектура MVC:
/// - Model: CalendarData - данные календаря
/// - View: CalendarUI - отображение календаря
/// - Controller: DatePicker - управление логикой
/// 
/// ФУНКЦИОНАЛЬНОСТЬ:
/// - Навигация по месяцам и годам
/// - Выбор даты с визуальным выделением
/// - Форматирование даты в yyyy-MM-dd
/// - События для уведомления о выборе даты
/// - Поддержка локализации
/// 
/// ИСПОЛЬЗОВАНИЕ:
/// - Подключить к полю ввода даты
/// - Подписаться на событие OnDateSelected
/// - Получить отформатированную дату
/// </summary>
public class DatePicker : MonoBehaviour, ICalendarManager
{
    [Header("UI References")]
    [SerializeField] private GameObject calendarPanel;
    [SerializeField] private Button prevMonthButton;
    [SerializeField] private Button nextMonthButton;
    [SerializeField] private TextMeshProUGUI monthYearText;
    [SerializeField] private Transform daysContainer;
    [SerializeField] private Button dayButtonPrefab;
    [SerializeField] private Button todayButton;
    [SerializeField] private Button clearButton;
    [SerializeField] private Button closeButton;

    [Header("Calendar Settings")]
    [SerializeField] private Color selectedDayColor = Color.blue;
    [SerializeField] private Color todayColor = Color.green;
    [SerializeField] private Color normalDayColor = Color.white;
    [SerializeField] private Color otherMonthColor = Color.gray;

    /// <summary>
    /// Событие выбора даты
    /// Параметр: отформатированная дата в формате yyyy-MM-dd
    /// </summary>
    public event Action<string> OnDateSelected;

    /// <summary>
    /// Событие закрытия календаря
    /// </summary>
    public event Action OnCalendarClosed;

    private DateTime currentDate;
    private DateTime selectedDate;
    private DateTime? initialDate;
    private List<Button> dayButtons = new List<Button>();
    private bool isInitialized = false;

    /// <summary>
    /// Инициализация компонента
    /// </summary>
    private void Start()
    {
        InitializeCalendar();
        SetupEventHandlers();
    }

    /// <summary>
    /// Инициализация календаря
    /// Создает кнопки дней и устанавливает начальные значения
    /// </summary>
    private void InitializeCalendar()
    {
        if (isInitialized) return;

        // Скрываем статические префабы (CurrentDayButton, DayButton)
        foreach (Transform child in daysContainer)
            Destroy(child.gameObject);

        dayButtons.Clear();

        // Создаем 35 кнопок для сетки календаря
        for (int i = 0; i < 35; i++)
        {
            var dayButton = Instantiate(dayButtonPrefab, daysContainer);
            dayButtons.Add(dayButton);
        }

        currentDate = DateTime.Today;
        selectedDate = DateTime.Today;
        isInitialized = true;
    }

    /// <summary>
    /// Настройка обработчиков событий
    /// </summary>
    private void SetupEventHandlers()
    {
        prevMonthButton.onClick.AddListener(PreviousMonth);
        nextMonthButton.onClick.AddListener(NextMonth);
        todayButton.onClick.AddListener(SetToday);
        clearButton.onClick.AddListener(ClearSelection);
        closeButton.onClick.AddListener(CloseCalendar);
    }

    /// <summary>
    /// Открывает календарь с опциональной начальной датой
    /// </summary>
    /// <param name="initialDate">Начальная дата (может быть null)</param>
    public void OpenCalendar(DateTime? initialDate = null)
    {
        InitializeCalendar();
        if (initialDate.HasValue)
        {
            currentDate = new DateTime(initialDate.Value.Year, initialDate.Value.Month, 1);
            selectedDate = initialDate.Value;
        }
        else
        {
            currentDate = new DateTime(DateTime.Today.Year, DateTime.Today.Month, 1);
            selectedDate = DateTime.Today;
        }
        UpdateCalendarDisplay();
        calendarPanel.SetActive(true);
    }

    /// <summary>
    /// Закрывает календарь
    /// </summary>
    public void CloseCalendar()
    {
        calendarPanel.SetActive(false);
        OnCalendarClosed?.Invoke();
    }

    /// <summary>
    /// Обновляет отображение календаря
    /// Заполняет сетку дней и обновляет заголовок
    /// </summary>
    private void UpdateCalendarDisplay()
    {
        UpdateMonthYearText();
        PopulateDaysGrid();
    }

    /// <summary>
    /// Обновляет текст месяца и года в заголовке
    /// </summary>
    private void UpdateMonthYearText()
    {
        var culture = CultureInfo.CurrentCulture;
        string monthName = culture.DateTimeFormat.GetMonthName(currentDate.Month);
        monthYearText.text = $"{monthName} {currentDate.Year}";
    }

    /// <summary>
    /// Заполняет сетку дней календаря
    /// </summary>
    private void PopulateDaysGrid()
    {
        int totalDays = 35;
        DateTime firstDayOfMonth = new DateTime(currentDate.Year, currentDate.Month, 1);
        int firstDayOfWeek = ((int)firstDayOfMonth.DayOfWeek + 6) % 7;
        DateTime gridStartDate = firstDayOfMonth.AddDays(-firstDayOfWeek);

        for (int i = 0; i < totalDays; i++)
        {
            var button = dayButtons[i];
            var buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
            var buttonImage = button.GetComponent<Image>();
            DateTime buttonDate = gridStartDate.AddDays(i);
            buttonText.text = buttonDate.Day.ToString();

            if (buttonDate.Month == currentDate.Month)
            {
                buttonText.color = Color.black;
                buttonImage.color = normalDayColor;
                button.interactable = true;
                if (buttonDate.Date == selectedDate.Date)
                    buttonImage.color = selectedDayColor;
                else if (buttonDate.Date == DateTime.Today.Date)
                    buttonImage.color = todayColor;
            }
            else
            {
                buttonText.color = Color.gray;
                buttonImage.color = otherMonthColor;
                button.interactable = true; // разрешаем выбор соседних месяцев
            }
            // Сохраняем дату в обработчик клика
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnDayButtonClicked(buttonDate));
        }
    }

    /// <summary>
    /// Обработчик клика по кнопке дня
    /// </summary>
    /// <param name="buttonDate">Дата, соответствующая кнопке</param>
    private void OnDayButtonClicked(DateTime buttonDate)
    {
        selectedDate = buttonDate;
        // Если выбран день из другого месяца — обновляем currentDate
        if (buttonDate.Month != currentDate.Month)
            currentDate = new DateTime(buttonDate.Year, buttonDate.Month, 1);

        UpdateCalendarDisplay();
        string formattedDate = selectedDate.ToString("yyyy-MM-dd");
        OnDateSelected?.Invoke(formattedDate);
        CloseCalendar();
    }

    /// <summary>
    /// Переход к предыдущему месяцу
    /// </summary>
    private void PreviousMonth()
    {
        currentDate = currentDate.AddMonths(-1);
        UpdateCalendarDisplay();
    }

    /// <summary>
    /// Переход к следующему месяцу
    /// </summary>
    private void NextMonth()
    {
        currentDate = currentDate.AddMonths(1);
        UpdateCalendarDisplay();
    }

    /// <summary>
    /// Устанавливает сегодняшнюю дату
    /// </summary>
    public void SetToday()
    {
        selectedDate = DateTime.Today;
        currentDate = DateTime.Today;
        UpdateCalendarDisplay();
        
        string formattedDate = selectedDate.ToString("yyyy-MM-dd");
        OnDateSelected?.Invoke(formattedDate);
        CloseCalendar();
    }

    /// <summary>
    /// Очищает выбранную дату
    /// </summary>
    public void ClearSelection()
    {
        selectedDate = DateTime.Today;
        UpdateCalendarDisplay();
        OnDateSelected?.Invoke("");
    }

    /// <summary>
    /// Получает текущую выбранную дату в формате yyyy-MM-dd
    /// </summary>
    /// <returns>Отформатированная дата или пустая строка, если дата не выбрана</returns>
    public string GetSelectedDate()
    {
        return selectedDate.ToString("yyyy-MM-dd");
    }

    /// <summary>
    /// Устанавливает выбранную дату из строки
    /// </summary>
    /// <param name="dateString">Дата в формате yyyy-MM-dd</param>
    public void SetSelectedDate(string dateString)
    {
        if (DateTime.TryParseExact(dateString, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime date))
        {
            selectedDate = date;
            currentDate = date;
            UpdateCalendarDisplay();
        }
    }

    /// <summary>
    /// Проверяет, открыт ли календарь
    /// </summary>
    /// <returns>true, если календарь открыт</returns>
    public bool IsOpen()
    {
        return calendarPanel.activeSelf;
    }
} 