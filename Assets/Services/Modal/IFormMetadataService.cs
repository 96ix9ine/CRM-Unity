using System.Threading.Tasks;

/// <summary>
/// Сервис для получения метаданных формы
/// </summary>
public interface IFormMetadataService
{
    Task<FormMetadata> GetFormMetadataAsync(string tableName);
}
