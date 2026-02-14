using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Collections;

/// <summary>
/// Реестр провайдеров опций для foreign key полей
/// </summary>
public sealed class ForeignKeyOptionsRegistry : MonoBehaviour, IForeignKeyOptionsProvider
{
    [SerializeField] private TableApiService m_ApiService;

    private readonly Dictionary<(string table, string column), Func<Task<Dictionary<string, string>>>> m_Providers =
        new();

    private void Awake()
    {
        if (m_ApiService == null)
            m_ApiService = FindAnyObjectByType<TableApiService>();
            
        RegisterCustomProviders();
    }

    private void RegisterCustomProviders()
    {
        // Shipments: licenseId, factoryId, transporterId, clientId
        Register("shipments", "licenseId", LoadLicenses);
        Register("shipments", "factoryId", () => LoadOrganizations("Выберите фабрику"));
        Register("shipments", "transporterId", () => LoadOrganizations("Выберите перевозчика"));
        Register("shipments", "clientId", () => LoadOrganizations("Выберите клиента"));

        // Arrival Invoices: supplierId, factoryId
        Register("arrival_invoices", "supplierId", () => LoadOrganizations("Выберите поставщика"));
        Register("arrival_invoices", "factoryId", () => LoadOrganizations("Выберите фабрику"));

        // Stands: parentId → только категории
        Register("stands", "parentId", LoadStandCategories);

        // Current tasks: standTaskId → корни stand_tasks
        Register("current_tasks", "standTaskId", LoadRootStandTasks);
        
        // Stand tasks: parentId → корни stand_tasks
        Register("stand_tasks", "parentId", LoadRootStandTasks);

        // Employees: выбор профессии в стратегии модалки
        Register("employees", "professionId", LoadProfessions);

        // Shipments strategy: выбор стенда
        Register("", "standId", LoadStands);

        // Глобальный fallback по одному лишь названию поля (FieldRenderer зовёт с пустой таблицей)
        Register("", "professionId", LoadProfessions);
    }

    public void Register(string table, string column, Func<Task<Dictionary<string, string>>> loader)
        => m_Providers[(table, column)] = loader;

    public async Task<Dictionary<string, string>> GetOptionsAsync(string tableName, string columnName)
    {
        if (m_Providers.TryGetValue((tableName, columnName), out var loader))
            return await loader();
            
        return new Dictionary<string, string>();
    }

    #region Private Loaders

    private async Task<Dictionary<string, string>> LoadLicenses()
    {
        List<Dictionary<string, object>> rows = null;
        bool done = false;
        
        StartCoroutine(m_ApiService.GetJson<List<Dictionary<string, object>>>("/database/license", 
            r => { rows = r; done = true; }));
        
        while (!done) await Task.Yield();
        
        var dict = new Dictionary<string, string>();
        if (rows != null)
        {
            foreach (var row in rows)
            {
                if (row.ContainsKey("id"))
                {
                    string id = row["id"].ToString();
                    string code = row.TryGetValue("licenseCode", out var codeVal) 
                        ? codeVal?.ToString() 
                        : $"Лицензия #{id}";
                    dict[id] = code;
                }
            }
        }
        
        return dict;
    }

    private async Task<Dictionary<string, string>> LoadOrganizations(string placeholder)
    {
        List<Dictionary<string, object>> rows = null;
        bool done = false;
        
        StartCoroutine(m_ApiService.GetJson<List<Dictionary<string, object>>>("/database/organizations", 
            r => { rows = r; done = true; }));
        
        while (!done) await Task.Yield();
        
        var dict = new Dictionary<string, string>();
        if (rows != null)
        {
            foreach (var row in rows)
            {
                if (row.ContainsKey("id"))
                {
                    string id = row["id"].ToString();
                    string title = row.TryGetValue("shortName", out var s) ? s?.ToString() :
                                 row.TryGetValue("fullName", out var f) ? f?.ToString() : 
                                 $"Организация #{id}";
                    dict[id] = title;
                }
            }
        }
        
        return dict;
    }

    private async Task<Dictionary<string, string>> LoadStandCategories()
    {
        List<Dictionary<string, object>> rows = null;
        bool done = false;
        
        StartCoroutine(m_ApiService.GetJson<List<Dictionary<string, object>>>("/stands/categories", 
            r => { rows = r; done = true; }));
        
        while (!done) await Task.Yield();
        
        var dict = new Dictionary<string, string>();
        if (rows != null)
        {
            foreach (var row in rows)
            {
                if (row.ContainsKey("id") && row.ContainsKey("title"))
                {
                    dict[row["id"].ToString()] = row["title"].ToString();
                }
            }
        }
        
        return dict;
    }

    private async Task<Dictionary<string, string>> LoadRootStandTasks()
    {
        List<Dictionary<string, object>> rows = null;
        bool done = false;
        
        StartCoroutine(m_ApiService.GetJson<List<Dictionary<string, object>>>("/stand_tasks/byParent/null", 
            r => { rows = r; done = true; }));
        
        while (!done) await Task.Yield();
        
        var dict = new Dictionary<string, string>();
        if (rows != null)
        {
            foreach (var row in rows)
            {
                if (row.ContainsKey("id") && row.ContainsKey("title"))
                {
                    dict[row["id"].ToString()] = row["title"].ToString();
                }
            }
        }
        
        return dict;
    }

    private async Task<Dictionary<string, string>> LoadProfessions()
    {
        // Источник данных: employees_professions/getDataForAdditional -> { employees:[], professions:[] }
        Dictionary<string, object> payload = null;
        bool done = false;

        StartCoroutine(m_ApiService.GetJson<Dictionary<string, object>>("/employees_professions/getDataForAdditional",
            r => { payload = r; done = true; }));

        while (!done) await Task.Yield();

        var dict = new Dictionary<string, string>();
        try
        {
            if (payload != null && payload.TryGetValue("professions", out var professionsObj))
            {
                // Поддержка как JArray, так и List<Dictionary<string,object>>
                if (professionsObj is Newtonsoft.Json.Linq.JArray jarr)
                {
                    foreach (var token in jarr)
                    {
                        var id = token["id"]?.ToString();
                        if (!string.IsNullOrEmpty(id))
                        {
                            var title = token["title"]?.ToString();
                            dict[id] = string.IsNullOrEmpty(title) ? $"Профессия #{id}" : title;
                        }
                    }
                }
                else if (professionsObj is System.Collections.IEnumerable list)
                {
                    foreach (var item in list)
                    {
                        if (item is Dictionary<string, object> row && row.ContainsKey("id"))
                        {
                            string id = row["id"].ToString();
                            string title = row.TryGetValue("title", out var t) ? t?.ToString() : $"Профессия #{id}";
                            dict[id] = title;
                        }
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"LoadProfessions parse error: {e.Message}");
        }

        return dict;
    }

    private async Task<Dictionary<string, string>> LoadStands()
    {
        List<Dictionary<string, object>> rows = null;
        bool done = false;

        StartCoroutine(m_ApiService.GetJson<List<Dictionary<string, object>>>("/database/stands",
            r => { rows = r; done = true; }));

        while (!done) await Task.Yield();

        var dict = new Dictionary<string, string>();
        if (rows != null)
        {
            foreach (var row in rows)
            {
                if (row.ContainsKey("id"))
                {
                    string id = row["id"].ToString();
                    string title = row.TryGetValue("title", out var t) ? t?.ToString() :
                                 row.TryGetValue("name", out var n) ? n?.ToString() :
                                 $"Стенд #{id}";
                    dict[id] = title;
                }
            }
        }

        return dict;
    }

    #endregion
}
