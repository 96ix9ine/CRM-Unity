using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using Validation.Components;

/// <summary>
/// Контроллер модального окна редактирования таблицы
/// Оркестрирует работу между View, Renderer и Services
/// </summary>
public class TableEditController : MonoBehaviour
{
    [Header("Dependencies")]
    [SerializeField] private TableEditView m_View;
    [SerializeField] private FieldRenderer m_FieldRenderer;
    [SerializeField] private FieldRendererValidationExtension m_ValidationExtension;
    [SerializeField] private IFormMetadataService m_FormMetadataService;
    [SerializeField] private INodeTypeMapService m_NodeTypeMapService;
    [SerializeField] private IValueProcessor m_ValueProcessor;
    [SerializeField] private IRecordValidationPolicy m_RecordValidationPolicy;
    [SerializeField] private TableApiService m_TableApiService;

    // Состояние модального окна
    private ModalState m_CurrentState;
    private IModalStrategy m_CurrentStrategy;
    private bool m_IsStrategySession;
    private int m_BuildVersion;

    // События
    public event Action<Dictionary<string, object>> OnSave;
    public event Action OnRecordSaved;
    public event Action OnRecordDeleted;

    // Кэш соответствия nodeType -> table name
    private Dictionary<string, string> m_NodeTypeToTableMap;

    #region Initialization

    private void Awake()
    {
        InitializeDependencies();
    }

    private void InitializeDependencies()
    {
        if (m_View == null)
            m_View = GetComponent<TableEditView>();
            
        if (m_FieldRenderer == null)
            m_FieldRenderer = GetComponent<FieldRenderer>();
            
        if (m_FormMetadataService == null)
            m_FormMetadataService = FindAnyObjectByType<FormMetadataService>();
            
        if (m_NodeTypeMapService == null)
            m_NodeTypeMapService = FindAnyObjectByType<NodeTypeMapService>();
            
        if (m_ValueProcessor == null)
            m_ValueProcessor = new ValueProcessor();
            
        if (m_RecordValidationPolicy == null)
            m_RecordValidationPolicy = new RecordValidationPolicy();

        if (m_TableApiService == null)
            m_TableApiService = FindAnyObjectByType<TableApiService>();

        if (m_ValidationExtension == null)
            m_ValidationExtension = GetComponent<FieldRendererValidationExtension>();

    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Открывает модальное окно с кастомной стратегией
    /// </summary>
    public async void OpenWithStrategy(IModalStrategy strategy, bool isEdit = false, Dictionary<string, object> data = null)
    {
        // Гарантируем инициализацию зависимостей на случай, если Awake ещё не вызывался
        InitializeDependencies();
        if (m_View == null)
        {
            m_View = GetComponent<TableEditView>();
        }
        if (m_FieldRenderer == null)
        {
            m_FieldRenderer = GetComponent<FieldRenderer>();
        }
        if (m_View == null)
        {
            Debug.LogError("TableEditController.OpenWithStrategy: m_View == null");
            return;
        }
        // Сбрасываем внешние обработчики сохранения от предыдущих экранов
        OnSave = null;
        m_CurrentStrategy = strategy;
        m_IsStrategySession = true;
        m_BuildVersion++; // инкремент версии сборки, чтобы отменять предыдущие рендеры
        m_CurrentState = new ModalState
        {
            InitialData = data ?? new Dictionary<string, object>(),
            IsEditMode = isEdit
        };

        // Заголовок
        var operation = isEdit ? $"Редактирование {strategy.StrategyName}" : $"Добавление {strategy.StrategyName}";
        m_View.SetTitle(operation);
        // Зафиксируем операцию для стратегий (используется в GetCurrentOperation)
        if (m_CurrentState != null)
        {
            m_CurrentState.CurrentOperation = operation;
        }

        // Загружаем необходимые данные
        await LoadRequiredDataAsync();

        // Определим таблицу контекста для стратегий (нужна для FK/валидации)
        if (data != null && data.ContainsKey("nodeType") && data["nodeType"] != null)
        {
            var mappedTable = GetTableName(data["nodeType"].ToString());
            if (!string.IsNullOrEmpty(mappedTable))
            {
                m_CurrentState.CurrentTable = mappedTable;
            }
        }

        // Если это режим редактирования и у нас есть id — попробуем подтянуть полную запись с бэка
        if (isEdit && data != null && data.ContainsKey("id"))
        {
            try
            {
                string tableForFetch = null;
                if (data.ContainsKey("nodeType") && data["nodeType"] != null)
                {
                    var mapped = GetTableName(data["nodeType"].ToString());
                    if (!string.IsNullOrEmpty(mapped)) tableForFetch = mapped;
                }

                // Если удалось определить таблицу — подтянем полные данные
                if (!string.IsNullOrEmpty(tableForFetch))
                {
                    var fullRow = await GetFullRowDataAsync(tableForFetch, data["id"].ToString());
                    if (fullRow != null && fullRow.Count > 0)
                    {
                        m_CurrentState.InitialData = fullRow;
                    }
                    Debug.Log("Полная запись загружена: " + Newtonsoft.Json.JsonConvert.SerializeObject(fullRow));

                    // Зафиксируем таблицу контекста, если ещё не установлена
                    if (string.IsNullOrEmpty(m_CurrentState.CurrentTable))
                        m_CurrentState.CurrentTable = tableForFetch;
                }
            }
            catch (Exception e)
            {
                Debug.LogWarning($"OpenWithStrategy: не удалось загрузить полные данные записи: {e.Message}");
            }
        }

        // Очищаем поля
        m_View.ClearFields();
        m_FieldRenderer.ClearFields();

        // Очищаем валидацию
        if (m_ValidationExtension != null)
            m_ValidationExtension.ClearCreatedFields();

        // Устанавливаем текущую сущность для валидации ПЕРЕД созданием полей
        if (m_ValidationExtension != null && !string.IsNullOrEmpty(m_CurrentState?.CurrentTable))
        {
            m_ValidationExtension.SetCurrentEntity(m_CurrentState.CurrentTable);
            Debug.Log($"<color=cyan>🏢 Установлена сущность '{m_CurrentState.CurrentTable}' для стратегической модалки</color>");
        }

        // Стратегия создает поля с актуальными initialData
        strategy.CreateFields(this, isEdit, m_CurrentState.InitialData);

        // Обновим таб-навигацию после генерации полей стратегией
        m_FieldRenderer.BuildTabNavigation();

        // Привязка кнопок
        m_View.BindSaveButton(OnSaveButtonClicked);
        m_View.BindCancelButton(Close);

        // Показываем модалку
        m_View.Show();
        PopupUI.Instance.Show();
    }

    /// <summary>
    /// Открывает модальное окно для создания или редактирования записи
    /// </summary>
    public async void Open(string operation, string nodeType, List<string> columns, 
        Dictionary<string, object> data = null, string parentKey = null, object parentValue = null)
    {
        // Гарантируем инициализацию зависимостей
        InitializeDependencies();
        if (m_View == null)
        {
            m_View = GetComponent<TableEditView>();
        }
        if (m_FieldRenderer == null)
        {
            m_FieldRenderer = GetComponent<FieldRenderer>();
        }
        if (m_View == null)
        {
            Debug.LogError("TableEditController.Open: m_View == null");
            return;
        }
        // Сбрасываем внешние обработчики сохранения от предыдущих экранов
        OnSave = null;
        // Сброс стратегии: обычная модалка не должна вызывать стратегический HandleSave
        m_CurrentStrategy = null;
        m_IsStrategySession = false;
        m_BuildVersion++; // новая сборка формы

        // Загружаем необходимые данные
        await LoadRequiredDataAsync();

        // Определяем таблицу
        string table = GetTableName(nodeType);

        m_CurrentState = new ModalState
        {
            CurrentTable = table,
            CurrentOperation = operation,
            Columns = columns ?? new List<string>(),
            InitialData = data ?? new Dictionary<string, object>(),
            ParentKey = parentKey,
            ParentValue = parentValue,
            IsEditMode = operation.ToLower().Contains("редактирование")
        };

        // Если это редактирование, получаем полные данные по id
        if (m_CurrentState.IsEditMode && data != null && data.ContainsKey("id"))
        {
            var fullData = await GetFullRowDataAsync(table, data["id"].ToString());
            if (fullData != null)
                m_CurrentState.InitialData = fullData;
            Debug.Log("Полная запись загружена: " + Newtonsoft.Json.JsonConvert.SerializeObject(fullData));
        }

        // Загружаем метаданные формы
        var formMetadata = await LoadFormMetadataAsync(table);
        if (formMetadata == null)
            formMetadata = new FormMetadata();

        // Очищаем поля
        m_View.ClearFields();
        m_FieldRenderer.ClearFields();

        // Очищаем валидацию
        if (m_ValidationExtension != null)
            m_ValidationExtension.ClearCreatedFields();

        // Устанавливаем текущую сущность для валидации ПЕРЕД созданием полей
        if (m_ValidationExtension != null && !string.IsNullOrEmpty(m_CurrentState?.CurrentTable))
        {
            m_ValidationExtension.SetCurrentEntity(m_CurrentState.CurrentTable);
            Debug.Log($"<color=cyan>🏢 Установлена сущность '{m_CurrentState.CurrentTable}' для стандартной модалки</color>");
        }

        // Создаем поля
        await CreateFieldsAsync(formMetadata);

        // Построить Tab-навигацию
        m_FieldRenderer.BuildTabNavigation();

        // Привязка кнопок
        m_View.BindSaveButton(OnSaveButtonClicked);
        m_View.BindCancelButton(Close);

        // Устанавливаем заголовок и показываем модалку
        m_View.SetTitle(operation);
        m_View.Show();
        var popup = PopupUI.Instance;
        if (popup != null) popup.Show();
    }

    /// <summary>
    /// Явный сброс подписчиков на событие сохранения (нужен для сценариев повторного открытия в одном контроллере)
    /// </summary>
    public void ResetOnSaveHandlers()
    {
        OnSave = null;
    }

    /// <summary>
    /// Закрывает модальное окно
    /// </summary>
    public void Close()
    {
        m_View.Hide();
        var popup = PopupUI.Instance;
        if (popup != null) popup.Hide();
        OnSave = null;
        m_CurrentStrategy = null;
        m_IsStrategySession = false;
        var calendar = CalendarService.Instance;
        if (calendar != null) calendar.CloseActiveCalendar();
    }

    /// <summary>
    /// Обрабатывает нажатие кнопки сохранения
    /// </summary>
    public void OnSaveButtonClicked()
    {
        var result = CollectFieldData();
        if (m_CurrentState == null)
        {
            Debug.LogWarning("TableEditController: m_CurrentState == null в OnSaveButtonClicked");
            return;
        }

        // Если есть активная стратегическая сессия, используем стратегию
        if (m_IsStrategySession && m_CurrentStrategy != null)
        {
            // Дополнительная валидация для стратегий: убедимся, что известна таблица
            if (string.IsNullOrEmpty(m_CurrentState?.CurrentTable))
            {
                // Попробуем вывести nodeType из initialData и замаппить
                if (m_CurrentState != null && m_CurrentState.InitialData != null && m_CurrentState.InitialData.TryGetValue("nodeType", out var nt) && nt != null)
                {
                    var mapped = GetTableName(nt.ToString());
                    if (!string.IsNullOrEmpty(mapped)) m_CurrentState.CurrentTable = mapped;
                }
            }

            // Устанавливаем текущую сущность для валидации
            if (m_ValidationExtension != null && !string.IsNullOrEmpty(m_CurrentState?.CurrentTable))
            {
                m_ValidationExtension.SetCurrentEntity(m_CurrentState.CurrentTable);
            }

            // ВАЛИДАЦИЯ ДЛЯ СТРАТЕГИЙ: проверяем поля перед сохранением
            if (m_ValidationExtension != null)
            {
                bool allValid = m_ValidationExtension.ValidateAllFields();
                if (!allValid)
                {
                    Debug.LogWarning("⚠️ Валидация не пройдена для стратегической модальной формы. Сохранение отменено.");
                    // Не закрываем модальное окно и не вызываем стратегию
                    return;
                }
                Debug.Log("✅ Валидация пройдена для стратегической модальной формы");
            }
            else
            {
                Debug.LogWarning("⚠️ FieldRendererValidationExtension не найден для стратегической формы. Валидация пропущена.");
            }

            m_CurrentStrategy.HandleSave(this, result, () => {
                OnRecordSaved?.Invoke();
                Close();
            });
            return;
        }

        // Устанавливаем текущую сущность для валидации (стандартные модальные окна)
        if (m_ValidationExtension != null && !string.IsNullOrEmpty(m_CurrentState?.CurrentTable))
        {
            m_ValidationExtension.SetCurrentEntity(m_CurrentState.CurrentTable);
        }

        // Единая валидация для всех модальных окон
        if (m_ValidationExtension != null)
        {
            bool allValid = m_ValidationExtension.ValidateAllFields();
            if (!allValid)
            {
                Debug.LogWarning("⚠️ Валидация не пройдена для стандартной модальной формы. Сохранение отменено.");
                return;
            }
            Debug.Log("✅ Валидация пройдена для стандартной модальной формы");
        }
        else
        {
            // Fallback к старой валидации если расширение недоступно
            if (!ValidateData(result))
                return;
        }

        // Стандартное сохранение
        if (OnSave != null)
        {
            OnSave.Invoke(result);
            Close();
            return;
        }

        // Фоллбек: если внешний обработчик не привязан — выполнить прямое сохранение через API
        if (!string.IsNullOrEmpty(m_CurrentState?.CurrentTable))
        {
            if (m_TableApiService == null)
                m_TableApiService = FindAnyObjectByType<TableApiService>();
            if (m_TableApiService != null)
            {
                // Определяем: добавление или редактирование
                bool isAdd = m_CurrentState.CurrentOperation != null && m_CurrentState.CurrentOperation.ToLower().Contains("добавление");
                if (isAdd)
                {
                    // Добавление с возможной загрузкой изображений после получения id
                    StartCoroutine(SaveWithOptionalImageUpload_Add(m_CurrentState.CurrentTable, result));
                }
                else
                {
                    // Нужен id для обновления: возьмём из InitialData
                    if (m_CurrentState.InitialData != null && m_CurrentState.InitialData.ContainsKey("id") && m_CurrentState.InitialData["id"] != null)
                    {
                        var id = m_CurrentState.InitialData["id"].ToString();
                        StartCoroutine(SaveWithOptionalImageUpload_Update(m_CurrentState.CurrentTable, id, result));
                    }
                    else
                    {
                        Debug.LogWarning($"TableEditController: нет id для обновления {m_CurrentState?.CurrentTable}");
                    }
                }
                return;
            }
        }

        Debug.LogWarning($"TableEditController: OnSave не привязан и нет API. Операция={m_CurrentState?.CurrentOperation}, Таблица={m_CurrentState?.CurrentTable}. Результат: {Newtonsoft.Json.JsonConvert.SerializeObject(result)}");
    }

    /// <summary>
    /// Публичное API для стратегий: сгенерировать поля для таблицы в указанный контейнер
    /// </summary>
    public async Task CreateFieldsForTable(string tableName, List<string> fields, Transform container, bool isEdit = false, Dictionary<string, object> data = null)
    {
        if (container == null || fields == null) return;

        // Устанавливаем текущую сущность для валидации ПЕРЕД созданием полей
        if (m_ValidationExtension != null)
        {
            m_ValidationExtension.SetCurrentEntity(tableName);
            // Очищаем старые поля от предыдущих форм
            m_ValidationExtension.ClearCreatedFields();
        }

        var formMetadata = await LoadFormMetadataAsync(tableName);

        foreach (var field in fields)
        {
            if (string.Equals(field, "id", StringComparison.OrdinalIgnoreCase))
                continue;

            // Локализация
            var displayName = await GetFieldDisplayNameAsync(tableName, field);

            // Тип поля
            bool isSelectField = false;
            bool isDateField = false;
            if (formMetadata.TryGetField(field, out var fieldMeta))
            {
                isSelectField = fieldMeta.Type == "select";
                isDateField = fieldMeta.Type == "date";
            }
            else
            {
                isSelectField = IsForeignKey(field);
                isDateField = field.ToLower().Contains("date");
            }

            if (isSelectField)
            {
                var options = await LoadDropdownOptions(field, formMetadata);
                string selected = (isEdit && data != null && data.ContainsKey(field)) ? (data[field]?.ToString() ?? "") : "";
                // Используем валидацию если доступна
                if (m_ValidationExtension != null)
                    await m_ValidationExtension.RenderDropdownFieldWithValidation(field, displayName, container, options, selected);
                else
                    await m_FieldRenderer.RenderDropdownField(field, displayName, container, options, selected);
            }
            else if (isDateField)
            {
                string value = (isEdit && data != null && data.ContainsKey(field)) ? (data[field]?.ToString() ?? "") : "";
                // Используем валидацию если доступна
                if (m_ValidationExtension != null)
                    m_ValidationExtension.RenderDateFieldWithValidation(field, displayName, container, value);
                else
                    m_FieldRenderer.RenderDateField(field, displayName, container, value);
            }
            else
            {
                string value = (isEdit && data != null && data.ContainsKey(field)) ? (data[field]?.ToString() ?? "") : "";
                // Используем валидацию если доступна
                if (m_ValidationExtension != null)
                    m_ValidationExtension.RenderTextFieldWithValidation(field, displayName, container, value);
                else
                    m_FieldRenderer.RenderTextField(field, displayName, container, value);
            }
        }

        // После пакетного рендера таблицы — обновим табы
        m_FieldRenderer.BuildTabNavigation();
    }

    /// <summary>
    /// Публичное API: специализированные dropdown’ы
    /// </summary>
    public async Task CreateProfessionDropdown(Transform container, bool isEdit = false, Dictionary<string, object> data = null)
    {
        // Используем общий механизм FK-провайдера в FieldRenderer
        await m_FieldRenderer.CreateDropdownField(
            "professionId",
            "Профессия",
            container,
            isEdit,
            data,
            // Явно укажем провайдер для стабильности; при его отсутствии сработает внутренний fallback по ("", "professionId")
            optionsProvider: () => FindAnyObjectByType<ForeignKeyOptionsRegistry>()
                .GetOptionsAsync("employees", "professionId")
        );
    }

    public async Task CreateStandDropdown(Transform container, bool isEdit = false, Dictionary<string, object> data = null)
    {
        var options = await LoadDropdownOptions("standId", new FormMetadata());
        string selected = (isEdit && data != null && data.ContainsKey("standId")) ? data["standId"]?.ToString() : "";

        // Используем валидацию если доступна
        if (m_ValidationExtension != null)
            await m_ValidationExtension.RenderDropdownFieldWithValidation("standId", "Стенд", container, options, selected);
        else
            await m_FieldRenderer.RenderDropdownField("standId", "Стенд", container, options, selected);
    }

    #endregion

    #region Public Properties for Strategy

    public Transform GetFieldsContainer() => m_View.FieldsContainer;
    public string GetCurrentOperation() => m_CurrentState?.CurrentOperation ?? "";
    public Dictionary<string, object> GetInitialData() => m_CurrentState?.InitialData ?? new Dictionary<string, object>();
    public GameObject CreateTableNameSeparator(string tableName, Transform container) => m_View.CreateTableNameSeparator(tableName, container);
    public int GetBuildVersion() => m_BuildVersion;
    public string GetCurrentTableName() => m_CurrentState?.CurrentTable ?? string.Empty;
    public FieldRendererValidationExtension GetFieldRendererValidationExtension() => m_ValidationExtension;

    #endregion

    #region Private Methods

    private async Task LoadRequiredDataAsync()
    {
        // Загружаем карту nodeType -> table
        if (m_NodeTypeMapService != null && (m_NodeTypeToTableMap == null || m_NodeTypeToTableMap.Count == 0))
        {
            try
            {
                m_NodeTypeToTableMap = await m_NodeTypeMapService.GetNodeTypeToTableMapAsync();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"Не удалось загрузить карту nodeType->table: {e.Message}");
                m_NodeTypeToTableMap = m_NodeTypeToTableMap ?? new Dictionary<string, string>();
            }
        }
    }

    private string GetTableName(string nodeType)
    {
        if (!string.IsNullOrEmpty(nodeType))
        {
            // если есть кэш — попробуем из кэша
            if (m_NodeTypeToTableMap != null && m_NodeTypeToTableMap.TryGetValue(nodeType, out var mapped))
            {
                if (!string.IsNullOrEmpty(mapped)) return mapped;
            }

            return nodeType;
        }
        return string.Empty;
    }

    private async Task<Dictionary<string, object>> GetFullRowDataAsync(string table, string id)
    {
        if (string.IsNullOrEmpty(table) || string.IsNullOrEmpty(id)) return null;
        if (m_TableApiService == null)
        {
            m_TableApiService = FindAnyObjectByType<TableApiService>();
            if (m_TableApiService == null)
            {
                Debug.LogError("TableEditController: TableApiService не найден для загрузки строки по id");
                return null;
            }
        }

        Dictionary<string, object> row = null;
        bool done = false;
        StartCoroutine(m_TableApiService.GetRowById(table, id, (r) => { row = r; done = true; }));
        while (!done) await Task.Yield();

        return row;
    }

    private async Task<FormMetadata> LoadFormMetadataAsync(string table)
    {
        if (m_FormMetadataService != null)
        {
            return await m_FormMetadataService.GetFormMetadataAsync(table);
        }
        return new FormMetadata();
    }

    private async Task CreateFieldsAsync(FormMetadata formMetadata)
    {
        if (m_CurrentState?.Columns == null) return;

        var template = m_View.FieldPrefab;

        foreach (var column in m_CurrentState.Columns)
        {
            // Пропускаем поле ID всегда
            if (string.Equals(column, "id", StringComparison.OrdinalIgnoreCase))
                continue;

            // Всегда используем единый контейнер (scroll view)
            Transform targetContainer = m_View.FieldsContainer;

            // Получаем локализованное название поля
            var displayName = await GetFieldDisplayNameAsync(m_CurrentState.CurrentTable, column);

            // Определяем тип поля
            bool isSelectField = false;
            bool isDateField = false;

            Debug.Log($"[TableEditController] Определение типа для поля '{column}'");

            if (formMetadata.TryGetField(column, out var fieldMeta))
            {
                isSelectField = fieldMeta.Type == "select";
                isDateField = fieldMeta.Type == "date";
                Debug.Log($"[TableEditController] Метаданные найдены для '{column}': Type='{fieldMeta.Type}', isSelect={isSelectField}, isDate={isDateField}");
            }
            else
            {
                // Fallback логика
                isSelectField = IsForeignKey(column);
                isDateField = column.ToLower().Contains("date");
                Debug.Log($"[TableEditController] Используем fallback логику для '{column}': isSelect={isSelectField} (IsForeignKey={IsForeignKey(column)}), isDate={isDateField}");
            }

            // Создаем поле в зависимости от типа
            if (isSelectField)
            {
                Debug.Log($"[TableEditController] Создаю Dropdown поле для '{column}'");
                await CreateDropdownField(column, displayName, targetContainer, formMetadata);
            }
            else if (isDateField)
            {
                Debug.Log($"[TableEditController] Создаю Date поле для '{column}'");
                CreateDateField(column, displayName, targetContainer);
            }
            else
            {
                Debug.Log($"[TableEditController] Создаю Text поле для '{column}'");
                CreateTextField(column, displayName, targetContainer);
            }

        }
    }

    private async Task CreateDropdownField(string column, string displayName, Transform container, FormMetadata formMetadata)
    {
        // Загружаем опции для dropdown
        var options = await LoadDropdownOptions(column, formMetadata);

        // Получаем выбранное значение
        string selectedValue = "";
        if (m_CurrentState.InitialData.ContainsKey(column))
            selectedValue = m_CurrentState.InitialData[column]?.ToString() ?? "";
        else if (m_CurrentState.ParentKey == column && m_CurrentState.ParentValue != null)
            selectedValue = m_CurrentState.ParentValue.ToString();

        // Используем валидацию если доступна
        if (m_ValidationExtension != null)
            await m_ValidationExtension.RenderDropdownFieldWithValidation(column, displayName, container, options, selectedValue);
        else
            await m_FieldRenderer.RenderDropdownField(column, displayName, container, options, selectedValue);
    }

    private void CreateDateField(string column, string displayName, Transform container)
    {
        string value = "";
        if (m_CurrentState.InitialData.ContainsKey(column))
            value = m_CurrentState.InitialData[column]?.ToString() ?? "";

        // Используем валидацию если доступна
        if (m_ValidationExtension != null)
            m_ValidationExtension.RenderDateFieldWithValidation(column, displayName, container, value);
        else
            m_FieldRenderer.RenderDateField(column, displayName, container, value);
    }

    private void CreateTextField(string column, string displayName, Transform container)
    {
        string value = "";
        if (m_CurrentState.InitialData.ContainsKey(column))
            value = m_CurrentState.InitialData[column]?.ToString() ?? "";

        // Используем валидацию если доступна
        if (m_ValidationExtension != null)
            m_ValidationExtension.RenderTextFieldWithValidation(column, displayName, container, value);
        else
            m_FieldRenderer.RenderTextField(column, displayName, container, value);
    }

    private async Task<Dictionary<string, string>> LoadDropdownOptions(string column, FormMetadata formMetadata)
    {
        var options = new Dictionary<string, string>();
        Debug.Log($"[TableEditController] Загрузка опций для dropdown '{column}'");

        // Загружаем из метаданных формы
        if (formMetadata.TryGetField(column, out var fieldMeta) && fieldMeta.Type == "select")
        {
            Debug.Log($"[TableEditController] Найдены метаданные типа 'select' для '{column}', опций: {fieldMeta.Options?.Count ?? 0}");
            foreach (var option in fieldMeta.Options)
            {
                if (!string.IsNullOrEmpty(option.Id))
                {
                    options[option.Id] = option.Label ?? option.Id;
                }
            }
            Debug.Log($"[TableEditController] Загружено опций из метаданных для '{column}': {options.Count}");
        }
        else
        {
            Debug.Log($"[TableEditController] Метаданные типа 'select' не найдены для '{column}', пробуем FK registry");
        }

        // Fallback: если опции из метаданных не пришли, попробуем реестр FK-провайдеров
        if (options.Count == 0)
        {
            var registry = FindAnyObjectByType<ForeignKeyOptionsRegistry>();
            if (registry != null)
            {
                var tableName = m_CurrentState?.CurrentTable ?? string.Empty;
                Debug.Log($"[TableEditController] Пробуем загрузить опции из FK registry для {tableName}.{column}");
                try
                {
                    var fkOptions = await registry.GetOptionsAsync(tableName, column);
                    if (fkOptions != null)
                    {
                        foreach (var kv in fkOptions)
                            options[kv.Key] = kv.Value;
                        Debug.Log($"[TableEditController] Загружено опций из FK registry для '{column}': {options.Count}");
                    }
                    else
                    {
                        Debug.LogWarning($"[TableEditController] FK registry вернул null для {tableName}.{column}");
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"LoadDropdownOptions FK registry fallback error for {tableName}.{column}: {e.Message}");
                }
            }
            else
            {
                Debug.LogWarning($"[TableEditController] ForeignKeyOptionsRegistry не найден");
            }
        }

        var finalOptions = AddNoParentOption(options);
        Debug.Log($"[TableEditController] Итого опций для '{column}': {finalOptions.Count}");
        return finalOptions;
    }

    private Dictionary<string, string> AddNoParentOption(Dictionary<string, string> options)
    {
        var result = new Dictionary<string, string>();
        result[""] = "Не выбрано";
        
        foreach (var kvp in options)
        {
            result[kvp.Key] = kvp.Value;
        }
        
        return result;
    }

    private void Update()
    {
        // Обработка Tab: перевод фокуса на следующий виджет.
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            var order = m_FieldRenderer.GetTabOrder();
            if (order == null || order.Count == 0) return;

            var current = UnityEngine.EventSystems.EventSystem.current?.currentSelectedGameObject;
            int idx = -1;
            if (current != null)
            {
                var sel = current.GetComponent<UnityEngine.UI.Selectable>();
                if (sel != null)
                {
                    idx = order.IndexOf(sel);
                }
            }

            int nextIdx = (idx + 1 + order.Count) % order.Count;
            var next = order[nextIdx];
            if (next != null)
            {
                next.Select();
            }
        }
    }

    private async Task<string> GetFieldDisplayNameAsync(string table, string column)
    {
        try
        {
            var localized = await DatabaseLocalizationService.Instance.GetFieldDisplayNameAsync(table, column);
            return string.IsNullOrEmpty(localized) ? column : localized;
        }
        catch
        {
            return column;
        }
    }

    private bool IsForeignKey(string column)
    {
        return column.ToLower().EndsWith("id") && column.ToLower() != "id";
    }

    private Dictionary<string, object> CollectFieldData()
    {
        var result = new Dictionary<string, object>();

        // Собираем данные из текстовых полей
        foreach (var kvp in m_FieldRenderer.InputFields)
        {
            string value = kvp.Value.text;
            result[kvp.Key] = ProcessFieldValue(kvp.Key, value);
        }

        // Собираем данные из dropdown полей
        foreach (var kvp in m_FieldRenderer.DropdownFields)
        {
            var dropdown = kvp.Value;
            var options = m_FieldRenderer.DropdownOptions[kvp.Key];
            var keys = options.Keys.ToList();
            var selectedIdx = dropdown.value;

            if (keys.Count > 0 && selectedIdx >= 0 && selectedIdx < keys.Count)
            {
                var selectedId = keys[selectedIdx];
                result[kvp.Key] = string.IsNullOrEmpty(selectedId) ? null : selectedId;
            }
            else
            {
                result[kvp.Key] = null;
            }
        }

        // Собираем данные из полей даты
        foreach (var kvp in m_FieldRenderer.DateFields)
        {
            string value = kvp.Value.GetDateValue();
            result[kvp.Key] = ProcessFieldValue(kvp.Key, value);
        }

        // Собираем данные из полей изображения (имя файла). Если пусто — null
        foreach (var fieldName in new [] { "photo", "icon", "image" })
        {
            if (m_FieldRenderer.TryGetImageFieldValue(fieldName, out var fileName))
            {
                result[fieldName] = string.IsNullOrEmpty(fileName) ? null : (object)fileName;
            }
        }

        return result;
    }

    private object ProcessFieldValue(string fieldName, string value)
    {
        if (m_ValueProcessor != null)
        {
            return m_ValueProcessor.Process(fieldName, value);
        }
        return value;
    }

    private bool ValidateData(Dictionary<string, object> data)
    {
        if (m_RecordValidationPolicy != null)
        {
            return m_RecordValidationPolicy.Validate(m_CurrentState.CurrentTable, data, out _);
        }
        return true;
    }

    #endregion

    #region Image upload save flows
    private IEnumerator SaveWithOptionalImageUpload_Add(string tableName, Dictionary<string, object> payload)
    {
        // 1) отправляем запись и ждём ответ с id
        bool done = false; bool ok = false; Dictionary<string, object> created = null;
        yield return StartCoroutine(m_TableApiService.AddRecordWithResponse(tableName, payload, (succ, resp) => { ok = succ; created = resp; done = true; }));
        if (!ok)
        {
            Debug.LogWarning($"AddRecordWithResponse failed for {tableName}");
            yield break;
        }

        string newId = null;
        if (created != null && created.TryGetValue("id", out var idObj) && idObj != null)
            newId = idObj.ToString();

        // Если удалось получить id и есть локально выбранные изображения — загружаем
        if (!string.IsNullOrEmpty(newId))
        {
#if UNITY_EDITOR
            foreach (var fieldName in new [] { "photo", "icon", "image" })
            {
                if (m_FieldRenderer.TryGetPendingLocalImagePath(fieldName, out var fullPath) && System.IO.File.Exists(fullPath))
                {
                    byte[] bytes = null;
                    try { bytes = System.IO.File.ReadAllBytes(fullPath); } catch (Exception e) { Debug.LogError($"ReadAllBytes error: {e.Message}"); }
                    if (bytes != null && bytes.Length > 0)
                    {
                        string fileName = System.IO.Path.GetFileName(fullPath);
                        bool uploadDone = false; bool uploadOk = false;
                        yield return StartCoroutine(m_TableApiService.UploadImage(tableName, newId, fileName, bytes, (uok) => { uploadOk = uok; uploadDone = true; }));
                        if (!uploadOk) Debug.LogWarning($"UploadImage failed for {tableName}/{newId} {fileName}");
                    }
                }
            }
#endif
        }

        OnRecordSaved?.Invoke();
        Close();
    }

    private IEnumerator SaveWithOptionalImageUpload_Update(string tableName, string id, Dictionary<string, object> payload)
    {
        bool done = false; bool ok = false;
        yield return StartCoroutine(m_TableApiService.UpdateRecord(tableName, id, payload, (succ) => { ok = succ; done = true; }));
        if (!ok)
        {
            Debug.LogWarning($"UpdateRecord failed for {tableName}/{id}");
            yield break;
        }

#if UNITY_EDITOR
        foreach (var fieldName in new [] { "photo", "icon", "image" })
        {
            if (m_FieldRenderer.TryGetPendingLocalImagePath(fieldName, out var fullPath) && System.IO.File.Exists(fullPath))
            {
                byte[] bytes = null;
                try { bytes = System.IO.File.ReadAllBytes(fullPath); } catch (Exception e) { Debug.LogError($"ReadAllBytes error: {e.Message}"); }
                if (bytes != null && bytes.Length > 0)
                {
                    string fileName = System.IO.Path.GetFileName(fullPath);
                    bool uploadDone = false; bool uploadOk = false;
                    yield return StartCoroutine(m_TableApiService.UploadImage(tableName, id, fileName, bytes, (uok) => { uploadOk = uok; uploadDone = true; }));
                    if (!uploadOk) Debug.LogWarning($"UploadImage failed for {tableName}/{id} {fileName}");
                }
            }
            else
            {
                // Если нет локального файла, но в поле выбран файл по имени — перепривяжем существующее изображение PATCH'ем
                if (m_FieldRenderer.ImageFieldValues != null && m_FieldRenderer.ImageFieldValues.TryGetValue(fieldName, out var selectedFileName) && !string.IsNullOrEmpty(selectedFileName))
                {
                    List<Dictionary<string, object>> allImages = null;
                    yield return StartCoroutine(m_TableApiService.GetJson<List<Dictionary<string, object>>>($"/database/images", (list) => allImages = list));
                    if (allImages != null)
                    {
                        string matchImageId = null;
                        foreach (var row in allImages)
                        {
                            if (row != null && row.ContainsKey("filename") && row["filename"] != null && row["filename"].ToString() == selectedFileName)
                            {
                                if (row.ContainsKey("id") && row["id"] != null) matchImageId = row["id"].ToString();
                                break;
                            }
                        }
                        if (!string.IsNullOrEmpty(matchImageId))
                        {
                            bool patchOk = false; bool patchDone = false;
                            yield return StartCoroutine(m_TableApiService.UpdateImageTarget(matchImageId, tableName, id, (ok) => { patchOk = ok; patchDone = true; }));
                            if (!patchOk) Debug.LogWarning($"UpdateImageTarget failed for image {matchImageId} -> {tableName}/{id}");
                        }
                    }
                }
            }
        }
#endif

        OnRecordSaved?.Invoke();
        Close();
    }
    #endregion
}
