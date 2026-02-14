using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using Newtonsoft.Json;

/// <summary>
/// Сервис для работы с правами доступа пользователей
/// Получает права доступа с backend вместо дублирования логики
/// </summary>
public class RolePermissionsService : MonoBehaviour
{
    public static RolePermissionsService Instance { get; private set; }
    [SerializeField] private TableApiService tableApiService;

    [System.Serializable]
    public class RolePermission
    {
        public bool read;
        public bool write;
        public bool writeSelf; // RWs - чтение/запись только своих
    }

    [System.Serializable]
    public class EntityPermissions
    {
        public Dictionary<string, RolePermission> permissions;
    }

    [System.Serializable]
    public class PermissionMatrix
    {
        public Dictionary<string, EntityPermissions> entities;
    }

    private PermissionMatrix permissionMatrix;
    private string currentProfession;
    private bool isLoaded = false;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Загружает матрицу прав доступа с backend
    /// </summary>
    public IEnumerator LoadPermissionsFromBackend()
    {
        if (tableApiService == null) tableApiService = FindObjectOfType<TableApiService>();
        string url = $"{tableApiService.apiUrl}/database/permissions";
        
        using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
        {
            yield return webRequest.SendWebRequest();
            
            if (webRequest.result == UnityWebRequest.Result.Success)
            {
                try
                {
                    permissionMatrix = JsonConvert.DeserializeObject<PermissionMatrix>(webRequest.downloadHandler.text);
                    isLoaded = true;
                    Debug.Log("RolePermissionsService: Права доступа загружены с backend");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"RolePermissionsService: Ошибка парсинга прав доступа: {ex.Message}");
                    LoadFallbackPermissions();
                }
            }
            else
            {
                Debug.LogWarning($"RolePermissionsService: Не удалось загрузить права с backend: {webRequest.error}. Используем fallback.");
                LoadFallbackPermissions();
            }
        }
    }

    /// <summary>
    /// Fallback права доступа (если backend недоступен)
    /// </summary>
    private void LoadFallbackPermissions()
    {
        permissionMatrix = new PermissionMatrix
        {
            entities = new Dictionary<string, EntityPermissions>()
        };

        // Добавляем права для всех сущностей
        AddEntityPermissions("arrival_invoices", new Dictionary<string, RolePermission>
        {
            { "Администратор", new RolePermission { read = true, write = true, writeSelf = false } },
            { "Директор", new RolePermission { read = true, write = true, writeSelf = false } },
            { "Бухгалтер", new RolePermission { read = true, write = false, writeSelf = false } },
            { "Зав. производством", new RolePermission { read = true, write = true, writeSelf = false } },
            { "Снабженец", new RolePermission { read = true, write = true, writeSelf = false } },
            { "Программист", new RolePermission { read = false, write = false, writeSelf = false } },
            { "Дизайнер", new RolePermission { read = false, write = false, writeSelf = false } },
            { "Конструктор", new RolePermission { read = false, write = false, writeSelf = false } },
            { "Инженер", new RolePermission { read = false, write = false, writeSelf = false } },
            { "Монтажник", new RolePermission { read = false, write = false, writeSelf = false } },
            { "Слесарь", new RolePermission { read = false, write = false, writeSelf = false } },
            { "Токарь", new RolePermission { read = false, write = false, writeSelf = false } },
            { "Фрезеровщик", new RolePermission { read = false, write = false, writeSelf = false } },
            { "Сварщик", new RolePermission { read = false, write = false, writeSelf = false } },
            { "Столяр", new RolePermission { read = false, write = false, writeSelf = false } },
            { "Технолог ПП", new RolePermission { read = false, write = false, writeSelf = false } },
            { "Покрасчик", new RolePermission { read = false, write = false, writeSelf = false } }
        });

        // Добавляем остальные сущности...
        // (для краткости добавляем только основные)
        AddEntityPermissions("components", new Dictionary<string, RolePermission>
        {
            { "Администратор", new RolePermission { read = true, write = true, writeSelf = false } },
            { "Директор", new RolePermission { read = true, write = false, writeSelf = false } },
            { "Бухгалтер", new RolePermission { read = true, write = false, writeSelf = false } },
            { "Зав. производством", new RolePermission { read = true, write = true, writeSelf = false } },
            { "Снабженец", new RolePermission { read = true, write = true, writeSelf = false } },
            { "Конструктор", new RolePermission { read = true, write = true, writeSelf = false } },
            { "Инженер", new RolePermission { read = true, write = true, writeSelf = false } },
            { "Монтажник", new RolePermission { read = true, write = false, writeSelf = false } },
            { "Слесарь", new RolePermission { read = true, write = false, writeSelf = false } },
            { "Токарь", new RolePermission { read = true, write = false, writeSelf = false } },
            { "Фрезеровщик", new RolePermission { read = true, write = false, writeSelf = false } },
            { "Сварщик", new RolePermission { read = true, write = false, writeSelf = false } },
            { "Столяр", new RolePermission { read = true, write = false, writeSelf = false } },
            { "Технолог ПП", new RolePermission { read = true, write = false, writeSelf = false } },
            { "Покрасчик", new RolePermission { read = true, write = false, writeSelf = false } },
            { "Монтажник", new RolePermission { read = true, write = false, writeSelf = false } }
        });

        AddEntityPermissions("current_tasks", new Dictionary<string, RolePermission>
        {
            { "Администратор", new RolePermission { read = true, write = true, writeSelf = false } },
            { "Директор", new RolePermission { read = true, write = false, writeSelf = false } },
            { "Зав. производством", new RolePermission { read = true, write = true, writeSelf = false } },
            { "Снабженец", new RolePermission { read = true, write = true, writeSelf = false } },
            { "Программист", new RolePermission { read = true, write = true, writeSelf = false } },
            { "Конструктор", new RolePermission { read = true, write = true, writeSelf = false } },
            { "Инженер", new RolePermission { read = true, write = true, writeSelf = false } },
            { "Монтажник", new RolePermission { read = true, write = false, writeSelf = true } },
            { "Слесарь", new RolePermission { read = true, write = false, writeSelf = true } },
            { "Токарь", new RolePermission { read = true, write = false, writeSelf = true } },
            { "Фрезеровщик", new RolePermission { read = true, write = false, writeSelf = true } },
            { "Сварщик", new RolePermission { read = true, write = false, writeSelf = true } },
            { "Столяр", new RolePermission { read = true, write = false, writeSelf = true } },
            { "Технолог ПП", new RolePermission { read = true, write = false, writeSelf = true } },
            { "Покрасчик", new RolePermission { read = true, write = false, writeSelf = true } }
        });

        AddEntityPermissions("current_task_states", new Dictionary<string, RolePermission>
        {
            { "Администратор", new RolePermission { read = true, write = true, writeSelf = false } },
            { "Директор", new RolePermission { read = false, write = false, writeSelf = false } },
            { "Бухгалтер", new RolePermission { read = false, write = false, writeSelf = false } },
            { "Зав. производством", new RolePermission { read = false, write = false, writeSelf = false } },
            { "Снабженец", new RolePermission { read = false, write = false, writeSelf = false } },
            { "Программист", new RolePermission { read = false, write = false, writeSelf = false } },
            { "Дизайнер", new RolePermission { read = false, write = false, writeSelf = false } },
            { "Конструктор", new RolePermission { read = false, write = false, writeSelf = false } },
            { "Инженер", new RolePermission { read = false, write = false, writeSelf = false } },
            { "Монтажник", new RolePermission { read = false, write = false, writeSelf = false } },
            { "Слесарь", new RolePermission { read = false, write = false, writeSelf = false } },
            { "Токарь", new RolePermission { read = false, write = false, writeSelf = false } },
            { "Фрезеровщик", new RolePermission { read = false, write = false, writeSelf = false } },
            { "Сварщик", new RolePermission { read = false, write = false, writeSelf = false } },
            { "Столяр", new RolePermission { read = false, write = false, writeSelf = false } },
            { "Технолог ПП", new RolePermission { read = false, write = false, writeSelf = false } },
            { "Покрасчик", new RolePermission { read = false, write = false, writeSelf = false } }
        });

        AddEntityPermissions("current_task_states_log", new Dictionary<string, RolePermission>
        {
            { "Администратор", new RolePermission { read = true, write = true, writeSelf = false } },
            { "Директор", new RolePermission { read = false, write = false, writeSelf = false } },
            { "Бухгалтер", new RolePermission { read = false, write = false, writeSelf = false } },
            { "Зав. производством", new RolePermission { read = false, write = false, writeSelf = false } },
            { "Снабженец", new RolePermission { read = false, write = false, writeSelf = false } },
            { "Программист", new RolePermission { read = false, write = false, writeSelf = false } },
            { "Дизайнер", new RolePermission { read = false, write = false, writeSelf = false } },
            { "Конструктор", new RolePermission { read = false, write = false, writeSelf = false } },
            { "Инженер", new RolePermission { read = false, write = false, writeSelf = false } },
            { "Монтажник", new RolePermission { read = false, write = false, writeSelf = false } },
            { "Слесарь", new RolePermission { read = false, write = false, writeSelf = false } },
            { "Токарь", new RolePermission { read = false, write = false, writeSelf = false } },
            { "Фрезеровщик", new RolePermission { read = false, write = false, writeSelf = false } },
            { "Сварщик", new RolePermission { read = false, write = false, writeSelf = false } },
            { "Столяр", new RolePermission { read = false, write = false, writeSelf = false } },
            { "Технолог ПП", new RolePermission { read = false, write = false, writeSelf = false } },
            { "Покрасчик", new RolePermission { read = false, write = false, writeSelf = false } }
        });

        AddEntityPermissions("employees", new Dictionary<string, RolePermission>
        {
            { "Администратор", new RolePermission { read = true, write = true, writeSelf = false } },
            { "Директор", new RolePermission { read = true, write = true, writeSelf = false } },
            { "Бухгалтер", new RolePermission { read = true, write = false, writeSelf = false } },
            { "Зав. производством", new RolePermission { read = true, write = true, writeSelf = false } },
            { "Конструктор", new RolePermission { read = true, write = false, writeSelf = false } },
            { "Инженер", new RolePermission { read = true, write = false, writeSelf = false } }
        });

        AddEntityPermissions("organizations", new Dictionary<string, RolePermission>
        {
            { "Администратор", new RolePermission { read = true, write = true, writeSelf = false } },
            { "Директор", new RolePermission { read = true, write = true, writeSelf = false } },
            { "Бухгалтер", new RolePermission { read = true, write = false, writeSelf = false } },
            { "Зав. производством", new RolePermission { read = true, write = true, writeSelf = false } },
            { "Снабженец", new RolePermission { read = true, write = true, writeSelf = false } },
            { "Программист", new RolePermission { read = true, write = false, writeSelf = false } },
            { "Конструктор", new RolePermission { read = true, write = false, writeSelf = false } },
            { "Инженер", new RolePermission { read = true, write = false, writeSelf = false } }
        });

        AddEntityPermissions("peoples", new Dictionary<string, RolePermission>
        {
            { "Администратор", new RolePermission { read = true, write = true, writeSelf = false } },
            { "Директор", new RolePermission { read = true, write = true, writeSelf = false } },
            { "Зав. производством", new RolePermission { read = true, write = true, writeSelf = false } },
            { "Снабженец", new RolePermission { read = true, write = true, writeSelf = false } },
            { "Программист", new RolePermission { read = true, write = true, writeSelf = false } },
            { "Конструктор", new RolePermission { read = true, write = false, writeSelf = false } },
            { "Инженер", new RolePermission { read = true, write = false, writeSelf = false } }
        });

        AddEntityPermissions("professions", new Dictionary<string, RolePermission>
        {
            { "Администратор", new RolePermission { read = true, write = true, writeSelf = false } },
            { "Директор", new RolePermission { read = true, write = true, writeSelf = false } },
            { "Бухгалтер", new RolePermission { read = true, write = false, writeSelf = false } },
            { "Зав. производством", new RolePermission { read = true, write = true, writeSelf = false } },
            { "Снабженец", new RolePermission { read = true, write = false, writeSelf = false } },
            { "Программист", new RolePermission { read = true, write = false, writeSelf = false } },
            { "Конструктор", new RolePermission { read = true, write = false, writeSelf = false } },
            { "Инженер", new RolePermission { read = true, write = false, writeSelf = false } }
        });

        AddEntityPermissions("user", new Dictionary<string, RolePermission>
        {
            { "Администратор", new RolePermission { read = true, write = true, writeSelf = false } },
            { "Директор", new RolePermission { read = true, write = true, writeSelf = false } },
            { "Зав. производством", new RolePermission { read = true, write = true, writeSelf = false } }
        });
        isLoaded = true;
        Debug.Log("RolePermissionsService: Загружены fallback права доступа");
    }

    private void AddEntityPermissions(string entityName, Dictionary<string, RolePermission> permissions)
    {
        permissionMatrix.entities[entityName] = new EntityPermissions
        {
            permissions = permissions
        };
    }

    /// <summary>
    /// Устанавливает текущую профессию пользователя
    /// </summary>
    public void SetCurrentProfession(string profession)
    {
        currentProfession = profession;
        Debug.Log($"RolePermissionsService: Установлена профессия: {profession}");
    }

    /// <summary>
    /// Получает текущую профессию пользователя
    /// </summary>
    public string GetCurrentProfession()
    {
        return currentProfession;
    }

    /// <summary>
    /// Получить права доступа для текущей профессии и сущности
    /// </summary>
    public RolePermission GetRolePermissions(string entity)
    {
        if (!isLoaded)
        {
            Debug.LogWarning("RolePermissionsService: Права доступа еще не загружены");
            return new RolePermission { read = false, write = false, writeSelf = false };
        }

        if (string.IsNullOrEmpty(currentProfession))
        {
            Debug.LogWarning("RolePermissionsService: Профессия не установлена");
            return new RolePermission { read = false, write = false, writeSelf = false };
        }

        if (!permissionMatrix.entities.ContainsKey(entity))
        {
            Debug.LogWarning($"RolePermissionsService: Сущность {entity} не найдена в матрице прав");
            return new RolePermission { read = false, write = false, writeSelf = false };
        }

        var entityPermissions = permissionMatrix.entities[entity];
        if (!entityPermissions.permissions.ContainsKey(currentProfession))
        {
            Debug.LogWarning($"RolePermissionsService: Профессия {currentProfession} не найдена для сущности {entity}");
            return new RolePermission { read = false, write = false, writeSelf = false };
        }

        return entityPermissions.permissions[currentProfession];
    }

    /// <summary>
    /// Проверить, может ли текущая профессия читать сущность
    /// </summary>
    public bool CanRead(string entity)
    {
        return GetRolePermissions(entity).read;
    }

    /// <summary>
    /// Проверить, может ли текущая профессия записывать в сущность
    /// </summary>
    public bool CanWrite(string entity)
    {
        var permissions = GetRolePermissions(entity);
        return permissions.write || permissions.writeSelf;
    }

    /// <summary>
    /// Проверить, может ли текущая профессия записывать только свои записи
    /// </summary>
    public bool CanWriteSelf(string entity)
    {
        return GetRolePermissions(entity).writeSelf;
    }

    /// <summary>
    /// Получить список доступных сущностей для текущей профессии
    /// </summary>
    public List<string> GetAvailableEntities()
    {
        if (!isLoaded)
        {
            Debug.LogWarning("RolePermissionsService: Права доступа еще не загружены");
            return new List<string>();
        }

        var availableEntities = new List<string>();
        
        foreach (var entity in permissionMatrix.entities.Keys)
        {
            if (CanRead(entity))
            {
                availableEntities.Add(entity);
            }
        }

        return availableEntities;
    }

    /// <summary>
    /// Получить список сущностей для TreeView с учетом прав доступа
    /// </summary>
    public List<string> GetTreeViewEntities()
    {
        if (!isLoaded)
        {
            Debug.LogWarning("RolePermissionsService: Права доступа еще не загружены");
            return new List<string>();
        }

        // Список сущностей, которые должны отображаться в TreeView
        var treeViewEntities = new List<string>
        {
            "arrival_invoices",
            "bills_for_pay", 
            "components",
            "current_tasks",
            "current_task_states",
            "current_task_states_log",
            "employees",
            "inventarization",
            "license",
            "order_requests",
            "organizations",
            "pcb_orders",
            "pcbs",
            "peoples",
            "professions",
            "shipments",
            "stand_tasks",
            "stands",
            "user",
            "writeoff"
        };

        return treeViewEntities.FindAll(entity => CanRead(entity));
    }

    /// <summary>
    /// Проверить, загружены ли права доступа
    /// </summary>
    public bool IsLoaded()
    {
        return isLoaded;
    }
} 