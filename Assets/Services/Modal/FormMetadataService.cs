using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections;

/// <summary>
/// Реализация сервиса для получения метаданных формы
/// </summary>
public class FormMetadataService : MonoBehaviour, IFormMetadataService
{
    [SerializeField] private TableApiService m_ApiService;

    private void Awake()
    {
        if (m_ApiService == null)
            m_ApiService = FindAnyObjectByType<TableApiService>();
    }

    public async Task<FormMetadata> GetFormMetadataAsync(string tableName)
    {
        Debug.Log($"[FormMetadataService] Загрузка метаданных для таблицы '{tableName}'");

        if (m_ApiService == null)
        {
            Debug.LogError("FormMetadataService: TableApiService не найден");
            return new FormMetadata();
        }

        Dictionary<string, object> raw = null;
        bool done = false;

        StartCoroutine(m_ApiService.GetJson<Dictionary<string, object>>($"/database/getFormMetaData/{tableName}",
            md => { raw = md; done = true; }));

        while (!done) await Task.Yield();

        Debug.Log($"[FormMetadataService] Получены сырые метаданные для '{tableName}': {(raw != null ? raw.Count : 0)} полей");
        if (raw != null)
        {
            Debug.Log($"[FormMetadataService] Сырые метаданные: {Newtonsoft.Json.JsonConvert.SerializeObject(raw)}");
        }

        var result = FormMetadata.FromRaw(raw);
        Debug.Log($"[FormMetadataService] Обработанные метаданные для '{tableName}': {result.FieldsByName?.Count ?? 0} полей");
        if (result.FieldsByName != null)
        {
            foreach (var kvp in result.FieldsByName)
            {
                var field = kvp.Value;
                Debug.Log($"[FormMetadataService] Поле: {field.Name} -> Type: {field.Type}, Options: {field.Options?.Count ?? 0}");
            }
        }

        return result;
    }
}
