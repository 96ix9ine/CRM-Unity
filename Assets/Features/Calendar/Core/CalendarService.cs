using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Сервис для управления календарями
/// 
/// ПАТТЕРН SINGLETON + SERVICE LOCATOR:
/// - Единая точка доступа к календарям
/// - Управление жизненным циклом календарей
/// - Кэширование и переиспользование компонентов
/// 
/// АРХИТЕКТУРА:
/// - Централизованное управление календарями
/// - Поддержка множественных календарей
/// - Автоматическое освобождение ресурсов
/// 
/// ИСПОЛЬЗОВАНИЕ:
/// - Создание и управление календарями
/// - Интеграция с UI компонентами
/// - Оптимизация производительности
/// </summary>
public class CalendarService : MonoBehaviour
{
    private static CalendarService _instance;
    public static CalendarService Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<CalendarService>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("CalendarService");
                    _instance = go.AddComponent<CalendarService>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    [Header("Calendar Settings")]
    [SerializeField] private GameObject datePickerPrefab;
    [SerializeField] private int maxCalendars = 5;

    private Dictionary<string, ICalendarManager> activeCalendars = new Dictionary<string, ICalendarManager>();
    private Queue<ICalendarManager> calendarPool = new Queue<ICalendarManager>();

    /// <summary>
    /// Событие создания нового календаря
    /// </summary>
    public event Action<ICalendarManager> OnCalendarCreated;

    /// <summary>
    /// Событие удаления календаря
    /// </summary>
    public event Action<ICalendarManager> OnCalendarDestroyed;

    private DateInputField activeDateField;

    private void Awake()
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
    /// Создает новый календарь
    /// </summary>
    /// <param name="calendarId">Уникальный идентификатор календаря</param>
    /// <param name="parent">Родительский объект</param>
    /// <returns>Созданный календарь</returns>
    public ICalendarManager CreateCalendar(string calendarId, Transform parent = null)
    {
        if (activeCalendars.ContainsKey(calendarId))
        {
            Debug.LogWarning($"Calendar with ID '{calendarId}' already exists");
            return activeCalendars[calendarId];
        }

        // Проверяем пул календарей
        ICalendarManager calendar = null;
        if (calendarPool.Count > 0)
        {
            calendar = calendarPool.Dequeue();
        }
        else
        {
            // Создаем новый календарь
            calendar = CreateNewCalendar(parent);
        }

        if (calendar != null)
        {
            activeCalendars[calendarId] = calendar;
            OnCalendarCreated?.Invoke(calendar);
        }

        return calendar;
    }

    /// <summary>
    /// Получает календарь по идентификатору
    /// </summary>
    /// <param name="calendarId">Идентификатор календаря</param>
    /// <returns>Календарь или null, если не найден</returns>
    public ICalendarManager GetCalendar(string calendarId)
    {
        activeCalendars.TryGetValue(calendarId, out ICalendarManager calendar);
        return calendar;
    }

    /// <summary>
    /// Удаляет календарь
    /// </summary>
    /// <param name="calendarId">Идентификатор календаря</param>
    public void DestroyCalendar(string calendarId)
    {
        if (activeCalendars.TryGetValue(calendarId, out ICalendarManager calendar))
        {
            // Возвращаем в пул, если есть место
            if (calendarPool.Count < maxCalendars)
            {
                calendarPool.Enqueue(calendar);
            }
            else
            {
                // Уничтожаем календарь
                if (calendar is MonoBehaviour mb)
                {
                    Destroy(mb.gameObject);
                }
            }

            activeCalendars.Remove(calendarId);
            OnCalendarDestroyed?.Invoke(calendar);
        }
    }

    /// <summary>
    /// Закрывает все активные календари
    /// </summary>
    public void CloseAllCalendars()
    {
        foreach (var calendar in activeCalendars.Values)
        {
            calendar.CloseCalendar();
        }
    }

    /// <summary>
    /// Очищает все календари
    /// </summary>
    public void ClearAllCalendars()
    {
        foreach (var calendar in activeCalendars.Values)
        {
            if (calendar is MonoBehaviour mb)
            {
                Destroy(mb.gameObject);
            }
        }

        activeCalendars.Clear();
        calendarPool.Clear();
    }

    /// <summary>
    /// Создает новый календарь
    /// </summary>
    /// <param name="parent">Родительский объект</param>
    /// <returns>Созданный календарь</returns>
    private ICalendarManager CreateNewCalendar(Transform parent)
    {
        if (datePickerPrefab == null)
        {
            Debug.LogError("DatePicker prefab is not assigned to CalendarService");
            return null;
        }

        Transform targetParent = parent ?? transform;
        GameObject calendarObject = Instantiate(datePickerPrefab, targetParent);
        ICalendarManager calendar = calendarObject.GetComponent<ICalendarManager>();

        if (calendar == null)
        {
            Debug.LogError("DatePicker prefab does not implement ICalendarManager");
            Destroy(calendarObject);
            return null;
        }

        return calendar;
    }

    /// <summary>
    /// Получает количество активных календарей
    /// </summary>
    /// <returns>Количество активных календарей</returns>
    public int GetActiveCalendarsCount()
    {
        return activeCalendars.Count;
    }

    /// <summary>
    /// Получает количество календарей в пуле
    /// </summary>
    /// <returns>Количество календарей в пуле</returns>
    public int GetPooledCalendarsCount()
    {
        return calendarPool.Count;
    }

    /// <summary>
    /// Проверяет, существует ли календарь
    /// </summary>
    /// <param name="calendarId">Идентификатор календаря</param>
    /// <returns>true, если календарь существует</returns>
    public bool HasCalendar(string calendarId)
    {
        return activeCalendars.ContainsKey(calendarId);
    }

    /// <summary>
    /// Открывает календарь для конкретного поля DateInputField (только один календарь на сцене)
    /// </summary>
    /// <param name="field">Поле, для которого открывается календарь</param>
    /// <param name="initialDate">Начальная дата (или null для сегодня)</param>
    public void OpenCalendarForField(DateInputField field, DateTime? initialDate)
    {
        activeDateField = field;
        // Ищем DatePicker у поля или на сцене
        DatePicker picker = null;
        picker = field.GetComponentInChildren<DatePicker>(true);
        if (picker == null)
        {
            picker = GameObject.FindObjectOfType<DatePicker>(true);
        }
        if (picker != null)
        {
            picker.OnDateSelected -= OnDateSelectedFromPicker;
            picker.OnDateSelected += OnDateSelectedFromPicker;
            picker.OnCalendarClosed -= OnCalendarClosedFromPicker;
            picker.OnCalendarClosed += OnCalendarClosedFromPicker;
            picker.OpenCalendar(initialDate);
        }
        else
        {
            Debug.LogError("DatePicker instance not found in scene!");
        }
    }

    private void OnDateSelectedFromPicker(string date)
    {
        if (activeDateField != null)
        {
            activeDateField.SetDate(date);
        }
    }

    private void OnCalendarClosedFromPicker()
    {
        activeDateField = null;
    }

    /// <summary>
    /// Закрывает активный календарь и сбрасывает активное поле
    /// </summary>
    public void CloseActiveCalendar()
    {
        DatePicker picker = GameObject.FindObjectOfType<DatePicker>(true);
        if (picker != null)
            picker.CloseCalendar();
        activeDateField = null;
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            ClearAllCalendars();
        }
    }
} 