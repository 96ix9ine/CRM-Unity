using System.Collections.Generic;

/// <summary>
/// Сервис для валидации записей
/// </summary>
public interface IRecordValidationPolicy
{
    bool Validate(string tableName, Dictionary<string, object> data, out string errorMessage);
}
