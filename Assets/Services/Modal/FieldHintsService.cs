using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections;

/// <summary>
/// Реализация сервиса для получения подсказок полей
/// </summary>
public class FieldHintsService : MonoBehaviour, IFieldHintsService
{
    [SerializeField] private TableApiService m_ApiService;

    private void Awake()
    {
        if (m_ApiService == null)
            m_ApiService = FindAnyObjectByType<TableApiService>();
    }

    public async Task<Dictionary<string, string>> GetFieldHintsAsync()
    {
        if (m_ApiService == null)
        {
            Debug.LogError("FieldHintsService: TableApiService не найден");
            return new Dictionary<string, string>();
        }

        Dictionary<string, string> hints = null;
        bool done = false;
        
        StartCoroutine(m_ApiService.GetFieldHints(h => { hints = h; done = true; }));
        
        while (!done) await Task.Yield();
        
        return hints ?? new Dictionary<string, string>();
    }
}
