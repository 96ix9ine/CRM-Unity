using System.Collections.Generic;
using System.Linq;
using Core.Database.Models;
using Core.TreeView.Interfaces;
using Core.TreeView.Models;
using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Core.Database.Providers
{
    /// <summary>
    /// Провайдер данных для дерева базы данных. Поддерживает загрузку, фильтрацию, поиск, пагинацию и работу с TreeView.
    /// Загружает и управляет данными о таблицах и их полях
    /// 
    /// СВЯЗИ С ДРУГИМИ КОМПОНЕНТАМИ:
    /// - TreeViewController: предоставляет данные для отображения
    /// - DatabaseTreeView: управляет отображением иерархических таблиц
    /// - FiltersListItems: применяет фильтры к иерархическим таблицам
    /// - SearchField: выполняет поиск по иерархическим таблицам
    /// - PaginationListItems: синхронизирует пагинацию полей
    /// 
    /// ЛОГИКА РАБОТЫ:
    /// - Загружает данные с сервера через REST API
    /// - Поддерживает обычные и иерархические таблицы
    /// - Применяет фильтры и поиск к данным
    /// - Управляет состоянием развернутых узлов
    /// - Синхронизируется с UI компонентами
    /// </summary>
    public class DatabaseTreeViewProvider : MonoBehaviour, ITreeViewProvider
    {
        [SerializeField] private TableApiService tableApiService;
        
        private DatabaseTreeNode _root;                     // Корневой узел дерева
        private List<ITreeNode> _visibleNodes;              // Список видимых узлов
        private HashSet<int> _expandedNodes;                // Множество развернутых узлов
        private int? _selectedNodeId;                       // ID выбранного узла

        // Данные для поиска и фильтрации
        private List<Dictionary<string, object>> currentRows = new List<Dictionary<string, object>>();
        private List<string> currentColumns = new List<string>();
        private List<string> activeFilterColumns = null;    // Активные фильтры колонок
        private string activeSearchQuery = null;            // Активный поисковый запрос

        private string lastTableName = null;                // Последняя загруженная таблица
        public string LastTableName => lastTableName;

        // Полная структура данных
        private List<string> allColumns = new List<string>();
        private List<Dictionary<string, object>> allRows = new List<Dictionary<string, object>>();

        // Локализация
        private Dictionary<string, string> localizedFieldNames = new Dictionary<string, string>();
        private string localizedTableName = null;
        
        /// <summary>
        /// Исходное иерархическое дерево для восстановления после фильтрации/поиска
        /// Сохраняется при первой загрузке иерархической таблицы
        /// </summary>
        public JObject originalHierarchyData;

        /// <summary>
        /// Отфильтрованное дерево после применения фильтров (null если фильтры не применялись)
        /// </summary>
        private JObject filteredHierarchyData = null;

        /// <summary>
        /// Текущие выбранные фильтры для иерархических таблиц
        /// Сохраняется при применении фильтров и используется при сбросе поиска
        /// </summary>
        private List<string> currentHierarchyFilter = null;

        /// <summary>
        /// Структура данных узла из бэкенда
        /// Используется для десериализации JSON данных с сервера
        /// </summary>
        [System.Serializable]
        private class BackendTreeNodeData
        {
            public string name;
            public List<BackendTreeNodeData> children;
        }

        /// <summary>
        /// Корневой узел дерева
        /// Точка входа в иерархию данных
        /// </summary>
        public ITreeNode Root => _root;

        /// <summary>
        /// Событие, вызываемое после загрузки данных
        /// Используется для уведомления UI о готовности данных
        /// </summary>
        public event System.Action OnDataLoaded;

        /// <summary>
        /// Инициализация компонента
        /// Создает корневой узел и инициализирует коллекции
        /// </summary>
        private void Awake()
        {
            _root = DatabaseTreeNode.CreateRoot();
            _visibleNodes = new List<ITreeNode>();
            _expandedNodes = new HashSet<int>();
        }

        /// <summary>
        /// Список таблиц, доступных для текущей роли пользователя
        /// Используется для фильтрации видимых узлов
        /// </summary>
        private HashSet<string> allowedTablesForRole = new HashSet<string>();

        /// <summary>
        /// Устанавливает список таблиц, доступных для текущей роли
        /// Вызывается из DatabaseTreeView при инициализации
        /// 
        /// ПАРАМЕТРЫ:
        /// - tables: множество названий доступных таблиц
        /// </summary>
        public void SetAllowedTablesForRole(HashSet<string> tables)
        {
            allowedTablesForRole = tables;
        }

        /// <summary>
        /// Показывает дерево только для выбранной таблицы
        /// Основной метод для загрузки и отображения данных таблицы
        /// 
        /// ПАРАМЕТРЫ:
        /// - tableName: название таблицы для загрузки
        /// 
        /// ЛОГИКА:
        /// - Сбрасывает все состояния (фильтры, поиск, данные)
        /// - Очищает фильтры иерархии при переходе к новой таблице
        /// - Запускает асинхронную загрузку данных
        /// </summary>
        public void ShowTableTree(string tableName)
        {
            // Debug.LogError($"[TreeView] ShowTableTree: {tableName}");
            lastTableName = tableName;
            activeFilterColumns = null; // Сброс фильтра
            activeSearchQuery = null;   // Сброс поиска
            currentRows.Clear();
            allRows.Clear();
            allColumns.Clear();
            // Сбрасываем фильтры иерархии при переходе к новой таблице
            currentHierarchyFilter = null;
            originalHierarchyData = null;
            StartCoroutine(LoadTableTreeAndRows(tableName));
        }

        /// <summary>
        /// Асинхронно загружает данные таблицы и строит дерево
        /// Основной метод загрузки данных с сервера
        /// 
        /// ПАРАМЕТРЫ:
        /// - tableName: название таблицы для загрузки
        /// 
        /// ЛОГИКА:
        /// - Загружает локализацию таблицы и полей
        /// - Пытается получить иерархические данные
        /// - Если иерархических данных нет, загружает обычные данные
        /// - Строит дерево из полученных данных
        /// - Уведомляет о завершении загрузки
        /// </summary>
        private IEnumerator LoadTableTreeAndRows(string tableName)
        {
            // Проверяем, что GameObject не уничтожен
            if (this == null || !this.gameObject || !this.gameObject.activeInHierarchy)
            {
                Debug.LogWarning("DatabaseTreeViewProvider: GameObject is destroyed or inactive, stopping LoadTableTreeAndRows");
                yield break;
            }
            
            _root = DatabaseTreeNode.CreateRoot();
            _visibleNodes.Clear();
            _expandedNodes.Clear();
            _selectedNodeId = null;
            allColumns.Clear();
            allRows.Clear();
            localizedFieldNames.Clear();
            localizedTableName = null;

            // --- Локализация таблицы ---
            if (tableApiService == null) tableApiService = FindObjectOfType<TableApiService>();
            if (tableApiService == null)
            {
                Debug.LogError("DatabaseTreeViewProvider: TableApiService не найден на сцене");
                yield break;
            }
            using (UnityWebRequest locTableRequest = UnityWebRequest.Get($"{tableApiService.apiUrl}/localization/tables/{tableName}/displayName"))
            {
                yield return locTableRequest.SendWebRequest();
                if (locTableRequest.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        var obj = JsonConvert.DeserializeObject<Dictionary<string, string>>(locTableRequest.downloadHandler.text);
                        if (obj != null && obj.ContainsKey("displayName"))
                            localizedTableName = obj["displayName"];
                    }
                    catch { localizedTableName = null; }
                }
            }
            // --- Локализация полей ---
            using (UnityWebRequest locFieldsRequest = UnityWebRequest.Get($"{tableApiService.apiUrl}/localization/tables/{tableName}/fields"))
            {
                yield return locFieldsRequest.SendWebRequest();
                if (locFieldsRequest.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        localizedFieldNames = JsonConvert.DeserializeObject<Dictionary<string, string>>(locFieldsRequest.downloadHandler.text);
                    }
                    catch { localizedFieldNames = new Dictionary<string, string>(); }
                }
            }

            // Попытка получить иерархические данные
            bool usedHierarchy = false;
            using (UnityWebRequest webRequest = UnityWebRequest.Get($"{tableApiService.apiUrl}/{tableName}/tree"))
            {
                string token = PlayerPrefs.GetString("token", "");
                if (!string.IsNullOrEmpty(token))
                    webRequest.SetRequestHeader("Authorization", "Bearer " + token);
                yield return webRequest.SendWebRequest();
                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    try
                    {
                        string jsonString = webRequest.downloadHandler.text;

                        // Проверяем, является ли JSON массивом или объектом
                        if (jsonString.TrimStart().StartsWith("["))
                        {
                            // JSON массив - преобразуем в иерархический объект
                            var jsonArray = JArray.Parse(jsonString);
                            if (jsonArray != null && jsonArray.Count > 0)
                            {
                                // Создаем корневой объект с массивом как children
                                var hierarchicalObject = new JObject
                                {
                                    ["name"] = tableName,
                                    ["children"] = jsonArray
                                };

                                originalHierarchyData = hierarchicalObject.DeepClone() as JObject;

                                // Инициализируем currentHierarchyFilter при первой загрузке, если он не установлен
                                if (IsHierarchicalTable(tableName) && currentHierarchyFilter == null)
                                {
                                    currentHierarchyFilter = GetHierarchyRootChildrenNamesFromOriginal();
                                }

                                // Если есть выбранные фильтры — применяем их сразу
                                if (IsHierarchicalTable(tableName) && currentHierarchyFilter != null)
                                {
                                    ApplyFilter(currentHierarchyFilter);
                                }
                                else
                                {
                                    BuildTreeFromData(hierarchicalObject, tableName);
                                    NotifyDataLoaded();
                                }
                                usedHierarchy = true;
                            }
                        }
                        else
                        {
                            // JSON объект - используем старую логику
                            var jsonData = JObject.Parse(jsonString);
                            // ВАЖНО: считаем иерархическими данные, даже если children пустой массив
                            if (jsonData != null && jsonData["children"] != null)
                            {
                                originalHierarchyData = jsonData.DeepClone() as JObject; // сохраняем исходное дерево
                                // Debug.LogError($"[TreeView] originalHierarchyData сохранено: {originalHierarchyData.ToString(Newtonsoft.Json.Formatting.None).Substring(0, 200)}...");

                                // Инициализируем currentHierarchyFilter при первой загрузке, если он не установлен
                                if (IsHierarchicalTable(tableName) && currentHierarchyFilter == null)
                                {
                                    currentHierarchyFilter = GetHierarchyRootChildrenNamesFromOriginal();
                                }

                                // Если есть выбранные фильтры — применяем их сразу
                                if (IsHierarchicalTable(tableName) && currentHierarchyFilter != null)
                                {
                                    ApplyFilter(currentHierarchyFilter);
                                }
                                else
                                {
                                    BuildTreeFromData(jsonData, tableName);
                                    NotifyDataLoaded();
                                }
                                usedHierarchy = true;
                            }
                        }
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"Error parsing hierarchical data: {ex.Message}");
                        Debug.LogError($"Response content: {webRequest.downloadHandler.text}");
                    }
                }
            }
            if (usedHierarchy)
                yield break;

            // Получаем все колонки
            using (UnityWebRequest webRequest = UnityWebRequest.Get($"{tableApiService.apiUrl}/database/{tableName}/columns"))
            {
                yield return webRequest.SendWebRequest();
                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    allColumns = JsonConvert.DeserializeObject<List<string>>(webRequest.downloadHandler.text);
                }
                else
                {
                    Debug.LogError($"Error loading fields: {webRequest.error}");
                }
            }
            // Получаем все строки
            using (UnityWebRequest webRequest = UnityWebRequest.Get($"{tableApiService.apiUrl}/database/{tableName}"))
            {
                yield return webRequest.SendWebRequest();
                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    allRows = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(webRequest.downloadHandler.text);
                }
                else
                {
                    Debug.LogError($"Error loading rows: {webRequest.error}");
                }
            }
            BuildTreeFromData(allColumns, allRows, activeFilterColumns);
            NotifyDataLoaded();
        }

        /// <summary>
        /// Рекурсивно преобразует объект в плоский словарь
        /// Используется для обработки сложных объектов из базы данных
        /// 
        /// ПАРАМЕТРЫ:
        /// - obj: объект для преобразования
        /// - prefix: префикс для ключей (для вложенных объектов)
        /// 
        /// ВОЗВРАЩАЕТ:
        /// - Плоский словарь с ключами вида "prefix_property"
        /// </summary>
        private Dictionary<string, object> DeepFlattenObjectToDictionary(object obj, string prefix = "")
        {
            var dict = new Dictionary<string, object>();
            if (obj == null) return dict;
            if (obj is IDictionary<string, object> d)
            {
                foreach (var kv in d)
                {
                    if (kv.Value == null) continue;
                    if (kv.Value is string || kv.Value.GetType().Namespace?.StartsWith("System") == true)
                        dict[prefix + kv.Key] = kv.Value;
                    else
                    {
                        var subDict = DeepFlattenObjectToDictionary(kv.Value, prefix + kv.Key + "_");
                        foreach (var subKv in subDict)
                            dict[subKv.Key] = subKv.Value;
                    }
                }
            }
            else
            {
                var type = obj.GetType();
                foreach (var prop in type.GetProperties())
                {
                    var value = prop.GetValue(obj);
                    if (value == null) continue;
                    if (value is string || value.GetType().Namespace?.StartsWith("System") == true)
                        dict[prefix + prop.Name] = value;
                    else
                    {
                        var subDict = DeepFlattenObjectToDictionary(value, prefix + prop.Name + "_");
                        foreach (var subKv in subDict)
                            dict[subKv.Key] = subKv.Value;
                    }
                }
            }
            // Гарантируем наличие ключей id и name, если они есть в исходном объекте
            if (!dict.ContainsKey("id") && obj.GetType().GetProperty("id") != null)
                dict["id"] = obj.GetType().GetProperty("id").GetValue(obj);
            if (!dict.ContainsKey("name") && obj.GetType().GetProperty("name") != null)
                dict["name"] = obj.GetType().GetProperty("name").GetValue(obj);
            return dict;
        }

        /// <summary>
        /// Рекурсивно строит дерево из JSON-объекта любой вложенности.
        /// Для листовых элементов (компонентов) создаёт FieldValueTreeNode, для промежуточных — FieldTreeNode.
        /// </summary>
        private void BuildTreeRecursive(JToken node, ITreeNode parent, string tableName)
        {
            if (node is JObject jObject)
            {
                var name = jObject["name"]?.ToString() ?? "";
                var children = jObject["children"] as JArray;

                // Лист — если нет детей или они пустые
                bool isLeaf = children == null || children.Count == 0;

                var dict = jObject.ToObject<Dictionary<string, object>>();

                // Явно добавляем nodeType в Data, если есть
                if (jObject["nodeType"] != null)
                    dict["nodeType"] = jObject["nodeType"].ToString();

                if (isLeaf)
                {
                    // Создаём листовой узел (компонент)
                    var valueNode = new FieldValueTreeNode(GetNextId(), name, tableName, tableName);
                    valueNode.Data = dict;
                    if (dict.ContainsKey("id"))
                        valueNode.Id = System.Convert.ToInt32(dict["id"]);
                    parent.AddChild(valueNode);
                }
                else
                {
                    // Создаём промежуточный узел (категория/подкатегория)
                    var fieldNode = new FieldTreeNode(GetNextId(), name, tableName);
                    fieldNode.DisplayName = name;
                    if (dict.ContainsKey("id"))
                        fieldNode.Id = System.Convert.ToInt32(dict["id"]);
                    fieldNode.Data = dict; // <--- добавляем Data для промежуточных узлов
                    parent.AddChild(fieldNode);

                    foreach (var child in children)
                    {
                        BuildTreeRecursive(child, fieldNode, tableName);
                    }
                }
            }
            else if (node is JArray jArray)
            {
                // Если node это массив, обрабатываем каждый элемент как дочерний узел
                foreach (var child in jArray)
                {
                    BuildTreeRecursive(child, parent, tableName);
                }
            }
            else
            {
                // Если node это простой элемент (string, number и т.д.)
                var name = node.ToString();
                var dict = new Dictionary<string, object>
                {
                    ["name"] = name
                };

                var valueNode = new FieldValueTreeNode(GetNextId(), name, tableName, tableName);
                valueNode.Data = dict;
                parent.AddChild(valueNode);
            }
        }

        /// <summary>
        /// Строит дерево из иерархических данных (JSON) с поддержкой любой вложенности.
        /// Использует BuildTreeRecursive для обхода всех уровней.
        /// </summary>
        public void BuildTreeFromData(JObject data, string tableName)
        {
            _root = DatabaseTreeNode.CreateRoot();
            var tableNode = new TableTreeNode(GetNextId(), tableName);
            tableNode.DisplayName = data["name"]?.ToString() ?? tableName;
            _root.AddChild(tableNode);

            var children = data["children"] as JArray;
            if (children != null)
            {
                foreach (var child in children)
                {
                    BuildTreeRecursive(child, tableNode, tableName);
                }
            }
            // Debug.LogError($"[TreeView] tableNode.Children.Count after BuildTreeRecursive: {tableNode.Children.Count}");
            if (_root.Children.Count > 0)
                _root.Children[0].IsExpanded = true;
        }

        /// <summary>
        /// Строит дерево из обычных данных (колонки и строки)
        /// Используется для обычных таблиц
        /// 
        /// ПАРАМЕТРЫ:
        /// - columns: список колонок таблицы
        /// - rows: список строк данных
        /// - filterColumns: список колонок для фильтрации (null = все колонки)
        /// 
        /// ЛОГИКА:
        /// - Создает корневой узел таблицы
        /// - Создает узлы полей для каждой колонки
        /// - Добавляет значения полей как дочерние узлы
        /// - Применяет фильтрацию по колонкам
        /// - Разворачивает корневой узел по умолчанию
        /// </summary>
        private void BuildTreeFromData(List<string> columns, List<Dictionary<string, object>> rows, List<string> filterColumns)
        {
            // Debug.LogError($"[TreeView] BuildTreeFromData (обычная): columns={string.Join(", ", columns)} rows={rows.Count}");
            allColumns = new List<string>(columns);
            allRows = new List<Dictionary<string, object>>(rows);
            _root = DatabaseTreeNode.CreateRoot();
            var tableNode = new TableTreeNode(GetNextId(), lastTableName);
            tableNode.DisplayName = localizedTableName ?? lastTableName;
            _root.AddChild(tableNode);
            foreach (var column in columns)
            {
                if (filterColumns != null && !filterColumns.Contains(column))
                    continue;
                var fieldNode = new FieldTreeNode(GetNextId(), column, lastTableName);
                fieldNode.DisplayName = localizedFieldNames.ContainsKey(column) ? localizedFieldNames[column] : column;
                foreach (var row in rows)
                {
                    if (row.TryGetValue(column, out var value) && value != null)
                    {
                        var valueNode = new FieldValueTreeNode(GetNextId(), value.ToString(), column, lastTableName);
                        valueNode.Data = row;
                        fieldNode.AddChild(valueNode);
                    }
                }
                tableNode.AddChild(fieldNode);
            }
            if (_root.Children.Count > 0)
                _root.Children[0].IsExpanded = true;
        }

        /// <summary>
        /// Очищает дерево
        /// Сбрасывает все состояния и данные
        /// </summary>
        public void Clear()
        {
            _root = DatabaseTreeNode.CreateRoot();
            _visibleNodes.Clear();
            _expandedNodes.Clear();
            _selectedNodeId = null;
        }

        /// <summary>
        /// Возвращает список видимых узлов (root не включается)
        /// Фильтрует узлы по доступным таблицам и видимости полей
        /// 
        /// ВОЗВРАЩАЕТ:
        /// - Список видимых узлов для отображения
        /// </summary>
        public List<ITreeNode> GetVisibleNodes()
        {
            _visibleNodes.Clear();
            foreach (var child in _root.Children)
            {
                if (allowedTablesForRole.Contains(child.Name))
                {
                    // Фильтруем поля по isVisible
                    if (child is TableTreeNode tableNode)
                    {
                        var visibleFields = new List<ITreeNode>();
                        foreach (var field in tableNode.Children)
                        {
                            bool visible = true;
                            if (field is FieldTreeNode ftn && ftn.Data is Dictionary<string, object> dict && dict.ContainsKey("isVisible"))
                                visible = (bool)dict["isVisible"];
                            if (visible)
                                visibleFields.Add(field);
                        }
                        tableNode.Children.Clear();
                        tableNode.Children.AddRange(visibleFields);
                    }
                    _visibleNodes.Add(child);
                }
            }
            return _visibleNodes;
        }

        /// <summary>
        /// Разворачивает узел
        /// Для FieldTreeNode загружает значения асинхронно
        /// 
        /// ПАРАМЕТРЫ:
        /// - node: узел для разворачивания
        /// 
        /// ЛОГИКА:
        /// - Для FieldTreeNode: запускает асинхронную загрузку значений
        /// - Для других узлов: разворачивает немедленно
        /// - Уведомляет о завершении операции
        /// </summary>
        public void ExpandNode(ITreeNode node)
        {
            // Debug.LogError($"[TreeView] ExpandNode: {node.Name} ({node.GetType().Name})");
            if (node is FieldTreeNode fieldNode && fieldNode.Children.Count == 0)
            {
                // Запускаем корутину для подгрузки значений поля
                StartCoroutine(LoadFieldValues(fieldNode));
                return; // Ждём асинхронной загрузки, не продолжаем expand
            }
            node.IsExpanded = true;
            _expandedNodes.Add(node.Id);
            NotifyDataLoaded(); // Гарантируем обновление UI для sync expand
        }

        /// <summary>
        /// Асинхронно загружает значения поля с сервера
        /// Используется для ленивой загрузки больших объемов данных
        /// 
        /// ПАРАМЕТРЫ:
        /// - fieldNode: узел поля для загрузки значений
        /// 
        /// ЛОГИКА:
        /// - Загружает уникальные значения поля с сервера
        /// - Создает узлы значений для каждого уникального значения
        /// - Разворачивает узел поля после загрузки
        /// - Уведомляет о завершении загрузки
        /// </summary>
        private IEnumerator LoadFieldValues(FieldTreeNode fieldNode)
        {
            // Debug.LogError($"[TreeView] LoadFieldValues стартует для {fieldNode.FieldName}");
            if (tableApiService == null) tableApiService = FindObjectOfType<TableApiService>();
            if (tableApiService == null)
            {
                Debug.LogError("DatabaseTreeViewProvider: TableApiService не найден на сцене");
                yield break;
            }
            string url = $"{tableApiService.apiUrl}/database/{fieldNode.TableName}/column/{fieldNode.FieldName}/values";
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                yield return webRequest.SendWebRequest();
                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    var rawList = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(webRequest.downloadHandler.text);
                    var values = new List<string>();
                    if (rawList != null && rawList.Count > 0)
                    {
                        var key = rawList[0].Keys.First();
                        foreach (var obj in rawList)
                        {
                            var val = obj[key]?.ToString() ?? "NULL";
                            values.Add(val);
                        }
                    }
                    foreach (var val in values)
                    {
                        var valueNode = new FieldValueTreeNode(GetNextId(), val, fieldNode.FieldName, fieldNode.TableName);
                        fieldNode.AddChild(valueNode);
                    }
                    // Debug.LogError($"[TreeView] Добавлено детей: {fieldNode.Children.Count}");
                }
                else
                {
                    Debug.LogError($"Error loading field values: {webRequest.error}");
                }
            }
            fieldNode.IsExpanded = true;
            _expandedNodes.Add(fieldNode.Id);
            NotifyDataLoaded(); // Гарантируем обновление UI после асинхронной загрузки
            // Debug.LogError("[TreeView] OnDataLoaded вызван");
        }

        /// <summary>
        /// Сворачивает узел
        /// Убирает узел из списка развернутых
        /// 
        /// ПАРАМЕТРЫ:
        /// - node: узел для сворачивания
        /// </summary>
        public void CollapseNode(ITreeNode node)
        {
            node.IsExpanded = false;
            _expandedNodes.Remove(node.Id);
        }

        /// <summary>
        /// Выбирает узел
        /// Снимает выбор с предыдущего узла и выбирает новый
        /// 
        /// ПАРАМЕТРЫ:
        /// - node: узел для выбора
        /// </summary>
        public void SelectNode(ITreeNode node)
        {
            if (_selectedNodeId.HasValue)
            {
                var previousNode = FindNodeById(_selectedNodeId.Value);
                if (previousNode != null)
                {
                    previousNode.IsSelected = false;
                }
            }

            node.IsSelected = true;
            _selectedNodeId = node.Id;
        }

        /// <summary>
        /// Отменяет выбор узла
        /// Снимает выбор с указанного узла
        /// 
        /// ПАРАМЕТРЫ:
        /// - node: узел для отмены выбора
        /// </summary>
        public void DeselectNode(ITreeNode node)
        {
            if (_selectedNodeId == node.Id)
            {
                node.IsSelected = false;
                _selectedNodeId = null;
            }
        }

        /// <summary>
        /// Находит узел по ID
        /// Рекурсивно ищет узел в дереве
        /// 
        /// ПАРАМЕТРЫ:
        /// - id: ID искомого узла
        /// 
        /// ВОЗВРАЩАЕТ:
        /// - Найденный узел или null
        /// </summary>
        private ITreeNode FindNodeById(int id)
        {
            return FindNodeByIdRecursive(_root, id);
        }

        /// <summary>
        /// Рекурсивно находит узел по ID
        /// Обходит все узлы дерева для поиска
        /// 
        /// ПАРАМЕТРЫ:
        /// - node: текущий узел для проверки
        /// - id: ID искомого узла
        /// 
        /// ВОЗВРАЩАЕТ:
        /// - Найденный узел или null
        /// </summary>
        private ITreeNode FindNodeByIdRecursive(ITreeNode node, int id)
        {
            if (node.Id == id)
                return node;

            foreach (var child in node.Children)
            {
                var found = FindNodeByIdRecursive(child, id);
                if (found != null)
                    return found;
            }

            return null;
        }

        /// <summary>
        /// Обновляет состояние дерева
        /// Восстанавливает развернутые узлы и выбранный узел
        /// </summary>
        public void Refresh()
        {
            // Восстанавливаем состояние развернутых узлов
            foreach (var nodeId in _expandedNodes)
            {
                var node = FindNodeById(nodeId);
                if (node != null)
                {
                    node.IsExpanded = true;
                }
            }

            // Восстанавливаем выбранный узел
            if (_selectedNodeId.HasValue)
            {
                var node = FindNodeById(_selectedNodeId.Value);
                if (node != null)
                {
                    node.IsSelected = true;
                }
            }
        }

        private int _nextId = 1;
        /// <summary>
        /// Генерирует следующий уникальный ID для узла
        /// Используется для создания уникальных идентификаторов узлов
        /// 
        /// ВОЗВРАЩАЕТ:
        /// - Следующий уникальный ID
        /// </summary>
        private int GetNextId()
        {
            return _nextId++;
        }

        /// <summary>
        /// Возвращает текущие строки для поиска/фильтрации
        /// Используется для получения данных текущей таблицы
        /// 
        /// ВОЗВРАЩАЕТ:
        /// - Копию списка текущих строк
        /// </summary>
        public List<Dictionary<string, object>> GetCurrentRows()
        {
            return new List<Dictionary<string, object>>(currentRows);
        }

        /// <summary>
        /// Применяет фильтр по колонкам (оставляет только выбранные колонки)
        /// Основной метод для фильтрации данных
        /// 
        /// ПАРАМЕТРЫ:
        /// - columns: список колонок для отображения
        /// 
        /// ЛОГИКА:
        /// - Для иерархических таблиц: фильтрует детей корня и перестраивает дерево
        /// - Для обычных таблиц: перестраивает дерево с фильтрацией колонок
        /// - Сохраняет выбранные фильтры для последующего использования
        /// - Уведомляет о завершении операции
        /// </summary>
        public void ApplyFilter(List<string> columns)
        {
            // Debug.LogError($"[TreeView] ApplyFilter called for table: {lastTableName}, columns: {string.Join(", ", columns)}");
            // Debug.LogError($"[TreeView] IsHierarchicalTable: {IsHierarchicalTable(lastTableName)}, originalHierarchyData is null: {originalHierarchyData == null}");
            activeFilterColumns = columns;

            if (IsHierarchicalTable(lastTableName) && originalHierarchyData != null)
            {
                currentHierarchyFilter = columns; // сохраняем выбранные фильтры

                // Собираем фильтры в виде Dictionary (ключ — поле, значение — значение фильтра)
                var filtersUI = GameObject.FindObjectOfType<FiltersListItems>();
                Dictionary<string, string> filters = null;
                if (filtersUI != null)
                    filters = filtersUI.GetSelectedTreeViewFilters();
                if (filters == null || filters.Count == 0)
                {
                    // Если фильтров нет — показываем всё дерево
                    BuildTreeFromData(originalHierarchyData, lastTableName);
                    filteredHierarchyData = null;
                    NotifyDataLoaded();
                    return;
                }

                var filteredRoot = RecursiveFilterWithSubtree(originalHierarchyData, filters);
                if (filteredRoot == null)
                {
                    filteredRoot = new JObject
                    {
                        ["name"] = originalHierarchyData["name"],
                        ["children"] = new JArray()
                    };
                }
                // Debug.LogError($"[TreeView] filteredRoot after filter: {filteredRoot.ToString(Newtonsoft.Json.Formatting.Indented)}");

                BuildTreeFromData((JObject)filteredRoot, lastTableName);
                filteredHierarchyData = filteredRoot.DeepClone() as JObject;
                NotifyDataLoaded();
                return;
            }

            // Обычные таблицы — старая логика
            BuildTreeFromData(allColumns, allRows, columns);
            NotifyDataLoaded();
        }

        /// <summary>
        /// Применяет поиск по значениям (оставляет только строки, содержащие query)
        /// Основной метод для поиска по данным
        /// 
        /// ПАРАМЕТРЫ:
        /// - query: поисковый запрос (пустая строка = сброс поиска)
        /// 
        /// ЛОГИКА:
        /// - При пустом запросе: сбрасывает поиск и применяет текущие фильтры
        /// - Для иерархических таблиц: запускает асинхронный поиск
        /// - Для обычных таблиц: запускает асинхронный поиск
        /// - Сворачивает все ветки после сброса поиска
        /// </summary>
        public void ApplySearch(string query)
        {
            // Debug.LogError($"[TreeView] ApplySearch: {query}");
            activeSearchQuery = query;
            if (string.IsNullOrEmpty(query))
            {
                // Сброс поиска — перестроить дерево только по выбранным фильтрам из UI
                if (IsHierarchicalTable(lastTableName) && originalHierarchyData != null)
                {
                    // Получаем фильтры только из UI (FiltersListItems)
                    var filtersUI = GameObject.FindObjectOfType<FiltersListItems>();
                    List<string> columns = null;
                    if (filtersUI != null)
                        columns = filtersUI.GetSelectedFilters();
                    
                    // Если фильтры не получены из UI, используем сохраненные фильтры
                    if (columns == null || columns.Count == 0)
                    {
                        columns = currentHierarchyFilter ?? GetHierarchyRootChildrenNamesFromOriginal();
                    }
                    
                    // Применяем фильтры и сворачиваем все ветки
                    ApplyFilter(columns);
                    
                    // Сворачиваем все ветки после сброса поиска
                    var root = Root;
                    if (root != null && root.Children.Count > 0)
                    {
                        var tableNode = root.Children[0];
                        foreach (var child in tableNode.Children)
                            child.IsExpanded = false;
                    }
                    return;
                }
                // Обычные таблицы — сброс поиска
                if (!string.IsNullOrEmpty(lastTableName))
                    StartCoroutine(LoadTableTreeAndRows(lastTableName));
                return;
            }
            // Проверка на иерархическую таблицу (есть метод/флаг или по имени)
            if (IsHierarchicalTable(lastTableName))
            {
                // Для иерархических таблиц поиск теперь всегда через SearchAndBuildTree
                if (!string.IsNullOrEmpty(lastTableName))
                {
                    // Debug.LogError($"[TreeView] Поиск по иерархии: {query}");
                    StartCoroutine(SearchAndBuildTree(lastTableName, query));
                }
                return;
            }
            if (!string.IsNullOrEmpty(lastTableName))
                StartCoroutine(SearchAndBuildTree(lastTableName, query));
        }

        /// <summary>
        /// Вспомогательный метод для определения иерархических таблиц
        /// Определяет, какие таблицы поддерживают иерархическое отображение
        /// 
        /// ПАРАМЕТРЫ:
        /// - tableName: название таблицы для проверки
        /// 
        /// ВОЗВРАЩАЕТ:
        /// - true, если таблица поддерживает иерархию
        /// 
        /// ПРИМЕЧАНИЕ:
        /// Можно расширить список по мере необходимости
        /// </summary>
        public bool IsHierarchicalTable(string tableName)
        {
            // Можно расширить список по мере необходимости
            return tableName == "employees"
                || tableName == "organizations"
                || tableName == "bills_for_pay"
                || tableName == "arrival_invoices"
                || tableName == "components"
                || tableName == "component_placements"
                || tableName == "current_tasks"
                || tableName == "current_task_states"
                || tableName == "inventarization"
                || tableName == "license"
                || tableName == "stand_tasks"
                || tableName == "shipments"
                || tableName == "order_requests"
                || tableName == "stands"
                || tableName == "pcb_orders"
                || tableName == "pcbs"
                || tableName == "peoples";
        }

        /// <summary>
        /// Метод для рекурсивного поиска по иерархии
        /// Ищет совпадения в узлах и их дочерних элементах
        /// 
        /// ПАРАМЕТРЫ:
        /// - node: узел для поиска
        /// - query: поисковый запрос
        /// 
        /// ВОЗВРАЩАЕТ:
        /// - Отфильтрованный узел с совпадающими элементами или null
        /// 
        /// ЛОГИКА:
        /// - Проверяет все свойства узла на совпадение с запросом
        /// - Рекурсивно обрабатывает дочерние узлы
        /// - Возвращает узел, если есть совпадения в нем или его детях
        /// </summary>
        private JObject RecursiveFilter(JObject node, string query)
        {
            bool match = false;
            foreach (var prop in node.Properties())
            {
                if (prop.Value.Type == JTokenType.String && prop.Value.ToString().ToLower().Contains(query.ToLower()))
                {
                    match = true;
                    // Debug.LogError($"[TreeView] Найдено совпадение: {prop.Name} = {prop.Value}");
                }
            }
            JArray filteredChildren = null;
            if (node["children"] is JArray children)
            {
                filteredChildren = new JArray();
                foreach (var child in children)
                {
                    if (child is JObject childObj)
                    {
                        var filteredChild = RecursiveFilter(childObj, query);
                        if (filteredChild != null)
                            filteredChildren.Add(filteredChild);
                    }
                }
            }
            if (match || (filteredChildren != null && filteredChildren.Count > 0))
            {
                var result = new JObject(node); // копируем все поля
                if (filteredChildren != null)
                    result["children"] = filteredChildren;
                return result;
            }
            return null;
        }

        // Рекурсивная фильтрация: если совпал родитель — возвращаем его целиком с потомками, иначе фильтруем детей
        private JToken RecursiveFilterWithSubtree(JToken node, Dictionary<string, string> filters)
        {
            if (node == null) return null;

            // Проверяем, совпадает ли этот узел по всем фильтрам
            bool matches = true;
            foreach (var filter in filters)
            {
                if (node[filter.Key] == null || node[filter.Key].ToString() != filter.Value)
                {
                    matches = false;
                    break;
                }
            }

            // Если совпал родитель — возвращаем его целиком (со всеми потомками)
            if (matches)
                return node.DeepClone();

            // Иначе фильтруем детей
            JArray filteredChildren = null;
            if (node["children"] is JArray children)
            {
                filteredChildren = new JArray();
                foreach (var child in children)
                {
                    var filteredChild = RecursiveFilterWithSubtree(child, filters);
                    if (filteredChild != null)
                        filteredChildren.Add(filteredChild);
                }
            }

            // Если есть совпавшие дети — возвращаем этот узел только с подходящими детьми
            if (filteredChildren != null && filteredChildren.Count > 0)
            {
                var result = node.DeepClone();
                result["children"] = filteredChildren;
                return result;
            }

            // Нет совпадений — не возвращаем этот узел
            return null;
        }

        /// <summary>
        /// Расширяем SearchAndBuildTree для поддержки поиска по иерархии
        /// Асинхронно выполняет поиск и строит дерево из результатов
        /// 
        /// ПАРАМЕТРЫ:
        /// - tableName: название таблицы для поиска
        /// - query: поисковый запрос
        /// 
        /// ЛОГИКА:
        /// - Для иерархических таблиц: фильтрует исходные данные и строит дерево
        /// - Для обычных таблиц: выполняет поиск через API и строит дерево
        /// - Разворачивает все узлы для показа результатов поиска
        /// - Уведомляет о завершении операции
        /// </summary>
        private IEnumerator SearchAndBuildTree(string tableName, string query)
        {
            // Debug.LogError($"[TreeView] SearchAndBuildTree: {tableName}, query={query}");
            _root = DatabaseTreeNode.CreateRoot();
            _visibleNodes.Clear();
            _expandedNodes.Clear();
            _selectedNodeId = null;

            if (IsHierarchicalTable(tableName) && (filteredHierarchyData != null || originalHierarchyData != null))
            {
                // Debug.LogError($"[TreeView] SearchAndBuildTree по иерархии: {query}");
                // Фильтруем children корня по currentHierarchyFilter
                var searchSource = filteredHierarchyData ?? originalHierarchyData;
                JArray filteredChildren = new JArray();
                var allChildren = searchSource["children"] as JArray;
                if (allChildren != null)
                {
                    // Используем currentHierarchyFilter или полный список, если фильтры не установлены
                    var filtersToUse = currentHierarchyFilter ?? GetHierarchyRootChildrenNamesFromOriginal();
                    foreach (var child in allChildren)
                    {
                        var name = child["name"]?.ToString();
                        if (filtersToUse.Contains(name))
                            filteredChildren.Add(child);
                    }
                }
                var filteredRoot = searchSource.DeepClone() as JObject;
                filteredRoot["children"] = filteredChildren;

                var filtered = RecursiveFilter(filteredRoot, query);
                if (filtered != null)
                {
                    // Debug.LogError($"[TreeView] BuildTreeFromData по фильтру: {filtered.ToString(Newtonsoft.Json.Formatting.None).Substring(0, 200)}...");
                    BuildTreeFromData(filtered, tableName);
                    ExpandAll(_root);
                }
                else
                {
                    // Debug.LogError($"[TreeView] Нет совпадений по фильтру: {query}");
                    // Можно очистить дерево или показать пустое
                    _root = DatabaseTreeNode.CreateRoot();
                }
                NotifyDataLoaded();
                yield break;
            }

            if (tableApiService == null) tableApiService = FindObjectOfType<TableApiService>();
            if (tableApiService == null)
            {
                Debug.LogError("DatabaseTreeViewProvider: TableApiService не найден на сцене");
                NotifyDataLoaded();
                yield break;
            }
            string url = $"{tableApiService.apiUrl}/database/{tableName}/search?query={UnityWebRequest.EscapeURL(query)}";
            using (UnityWebRequest webRequest = UnityWebRequest.Get(url))
            {
                yield return webRequest.SendWebRequest();
                if (webRequest.result == UnityWebRequest.Result.Success)
                {
                    var rows = JsonConvert.DeserializeObject<List<Dictionary<string, object>>>(webRequest.downloadHandler.text);
                    if (rows != null && rows.Count > 0)
                    {
                        BuildTreeFromData(new List<string>(rows[0].Keys), rows, activeFilterColumns);
                        ExpandAll(_root);
                    }
                }
            }
            NotifyDataLoaded();
        }

        /// <summary>
        /// Рекурсивно разворачивает все узлы дерева
        /// Используется для показа результатов поиска
        /// 
        /// ПАРАМЕТРЫ:
        /// - node: узел для разворачивания
        /// </summary>
        private void ExpandAll(ITreeNode node)
        {
            node.IsExpanded = true;
            foreach (var child in node.Children)
                ExpandAll(child);
        }

        /// <summary>
        /// Получить страницу видимых узлов (для пагинации по корню дерева)
        /// Используется для пагинации корневых узлов дерева
        /// 
        /// ПАРАМЕТРЫ:
        /// - start: начальный индекс
        /// - count: количество узлов
        /// 
        /// ВОЗВРАЩАЕТ:
        /// - Список узлов для указанной страницы
        /// </summary>
        public List<ITreeNode> GetVisibleNodesPage(int start, int count)
        {
            var all = GetVisibleNodes();
            if (start < 0) start = 0;
            if (count <= 0) return new List<ITreeNode>();
            if (start >= all.Count) return new List<ITreeNode>();
            int realCount = Mathf.Min(count, all.Count - start);
            return all.GetRange(start, realCount);
        }

        /// <summary>
        /// Получить общее количество видимых узлов (для пагинации по корню дерева)
        /// Используется для вычисления общего количества страниц
        /// 
        /// ВОЗВРАЩАЕТ:
        /// - Количество видимых узлов
        /// </summary>
        public int GetVisibleNodesCount()
        {
            return GetVisibleNodes().Count;
        }

        /// <summary>
        /// Возвращает имена детей корня для иерархических таблиц
        /// Используется для получения списка доступных фильтров
        /// 
        /// ВОЗВРАЩАЕТ:
        /// - Список имен детей корня (например, департаменты/типы организаций)
        /// </summary>
        public List<string> GetHierarchyRootChildrenNames()
        {
            if (_root == null || _root.Children.Count == 0) return new List<string>();
            var tableNode = _root.Children[0];
            return tableNode.Children.Select(child => child.Name).ToList();
        }

        /// <summary>
        /// Возвращает имена детей корня из исходного JObject
        /// Используется для получения полного списка фильтров из исходных данных
        /// 
        /// ВОЗВРАЩАЕТ:
        /// - Список имен детей корня из originalHierarchyData
        /// </summary>
        public List<string> GetHierarchyRootChildrenNamesFromOriginal()
        {
            if (originalHierarchyData == null || originalHierarchyData["children"] == null)
                return new List<string>();
            var children = originalHierarchyData["children"] as JArray;
            return children?.Select(child => child["name"]?.ToString()).Where(n => !string.IsNullOrEmpty(n)).ToList() ?? new List<string>();
        }

        /// <summary>
        /// Возвращает актуальный список колонок для обычных таблиц
        /// Используется для получения списка колонок текущей таблицы
        /// 
        /// ВОЗВРАЩАЕТ:
        /// - Копию списка колонок текущей таблицы
        /// </summary>
        public List<string> GetCurrentColumns()
        {
            return new List<string>(allColumns);
        }

        /// <summary>
        /// Публичный метод для уведомления о загрузке данных
        /// Вызывает событие OnDataLoaded для уведомления UI
        /// </summary>
        public void NotifyDataLoaded()
        {
            // Проверяем, что GameObject не уничтожен
            if (this == null || !this.gameObject || !this.gameObject.activeInHierarchy)
            {
                Debug.LogWarning("DatabaseTreeViewProvider: GameObject is destroyed or inactive, skipping NotifyDataLoaded");
                return;
            }
            
            OnDataLoaded?.Invoke();
        }

        /// <summary>
        /// Возвращает текущие фильтры иерархии
        /// Используется для получения сохраненных фильтров
        /// 
        /// ВОЗВРАЩАЕТ:
        /// - Текущие фильтры или полный список, если фильтры не установлены
        /// </summary>
        public List<string> GetCurrentHierarchyFilter()
        {
            return currentHierarchyFilter ?? GetHierarchyRootChildrenNamesFromOriginal();
        }
    }
} 