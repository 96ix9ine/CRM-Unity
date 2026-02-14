using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Сервис для получения подсказок полей
/// </summary>
public interface IFieldHintsService
{
    Task<Dictionary<string, string>> GetFieldHintsAsync();
}
