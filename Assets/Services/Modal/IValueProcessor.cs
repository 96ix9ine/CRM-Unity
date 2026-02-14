/// <summary>
/// Сервис для обработки значений полей
/// </summary>
public interface IValueProcessor
{
    object Process(string fieldName, string value);
}
