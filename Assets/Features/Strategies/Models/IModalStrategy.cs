using System.Collections.Generic;

/// <summary>
/// Интерфейс для стратегий модальных окон
/// Позволяет создавать кастомные модальные окна с полями из разных таблиц
/// </summary>
public interface IModalStrategy
{
    /// <summary>
    /// Название стратегии
    /// </summary>
    string StrategyName { get; }
    
    /// <summary>
    /// Создает поля для модального окна
    /// </summary>
    /// <param name="modal">Модальное окно</param>
    /// <param name="isEdit">Режим редактирования</param>
    /// <param name="data">Данные для редактирования</param>
    void CreateFields(TableEditController modal, bool isEdit, Dictionary<string, object> data = null);
    
    /// <summary>
    /// Обрабатывает сохранение данных
    /// </summary>
    /// <param name="modal">Модальное окно</param>
    /// <param name="data">Собранные данные</param>
    /// <param name="onSuccess">Callback при успешном сохранении</param>
    void HandleSave(TableEditController modal, Dictionary<string, object> data, System.Action onSuccess);
}
