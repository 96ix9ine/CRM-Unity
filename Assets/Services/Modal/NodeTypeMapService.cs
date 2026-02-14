using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections;

/// <summary>
/// Реализация сервиса для получения соответствия nodeType → tableName
/// </summary>
public class NodeTypeMapService : MonoBehaviour, INodeTypeMapService
{
    [SerializeField] private TableApiService m_ApiService;

    private void Awake()
    {
        if (m_ApiService == null)
            m_ApiService = FindAnyObjectByType<TableApiService>();
    }

    public async Task<Dictionary<string, string>> GetNodeTypeToTableMapAsync()
    {
        if (m_ApiService == null)
        {
            Debug.LogError("NodeTypeMapService: TableApiService не найден");
            return new Dictionary<string, string>();
        }

        Dictionary<string, string> map = null;
        bool done = false;
        
        StartCoroutine(m_ApiService.GetNodeTypesMapCached(m => { map = m; done = true; }));
        
        while (!done) await Task.Yield();
        
        return map ?? new Dictionary<string, string>();
    }
}
