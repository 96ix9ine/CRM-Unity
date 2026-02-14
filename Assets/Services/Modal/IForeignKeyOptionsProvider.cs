using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Сервис для получения опций для foreign key полей
/// </summary>
public interface IForeignKeyOptionsProvider
{
    Task<Dictionary<string, string>> GetOptionsAsync(string tableName, string columnName);
}
