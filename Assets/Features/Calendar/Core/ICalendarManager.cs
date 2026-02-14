using System;

/// <summary>
/// Интерфейс для управления календарем
/// 
/// ПАТТЕРН STRATEGY:
/// - Определяет контракт для различных стратегий работы с календарем
/// - Позволяет легко заменить реализацию календаря
/// - Обеспечивает единообразный интерфейс для работы с датами
/// 
/// АРХИТЕКТУРА:
/// - Абстракция для работы с календарем
/// - Поддержка различных форматов дат
/// - События для уведомления об изменениях
/// 
/// ИСПОЛЬЗОВАНИЕ:
/// - Реализация различных типов календарей
/// - Интеграция с UI компонентами
/// - Управление состоянием календаря
/// </summary>
public interface ICalendarManager
{
    /// <summary>
    /// Событие выбора даты
    /// </summary>
    event Action<string> OnDateSelected;

    /// <summary>
    /// Событие закрытия календаря
    /// </summary>
    event Action OnCalendarClosed;

    /// <summary>
    /// Открывает календарь
    /// </summary>
    /// <param name="initialDate">Начальная дата (опционально)</param>
    void OpenCalendar(DateTime? initialDate = null);

    /// <summary>
    /// Закрывает календарь
    /// </summary>
    void CloseCalendar();

    /// <summary>
    /// Получает выбранную дату
    /// </summary>
    /// <returns>Дата в формате yyyy-MM-dd</returns>
    string GetSelectedDate();

    /// <summary>
    /// Устанавливает выбранную дату
    /// </summary>
    /// <param name="dateString">Дата в формате yyyy-MM-dd</param>
    void SetSelectedDate(string dateString);

    /// <summary>
    /// Проверяет, открыт ли календарь
    /// </summary>
    /// <returns>true, если календарь открыт</returns>
    bool IsOpen();

    /// <summary>
    /// Очищает выбранную дату
    /// </summary>
    void ClearSelection();

    /// <summary>
    /// Устанавливает сегодняшнюю дату
    /// </summary>
    void SetToday();
} 