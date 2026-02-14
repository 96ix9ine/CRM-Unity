using System.Collections.Generic;
using System.Threading.Tasks;

/// <summary>
/// Сервис для получения соответствия nodeType → tableName
/// </summary>
public interface INodeTypeMapService
{
    Task<Dictionary<string, string>> GetNodeTypeToTableMapAsync();
}
