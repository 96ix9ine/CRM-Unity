using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Validation.Models;
using Validation.Services;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Validation.Components
{
    /// <summary>
    /// Расширение для FieldRenderer, которое автоматически добавляет валидацию к полям
    /// Вешается на тот же объект что и FieldRenderer
    /// </summary>
    public class FieldRendererValidationExtension : MonoBehaviour
    {
        [Header("Validation Extension")]
        [SerializeField] private bool m_EnableAutoValidation = true;
        [SerializeField] private FieldRenderer m_FieldRenderer;
        [SerializeField] private ValidationService m_ValidationService;
        private Dictionary<string, TextMeshProUGUI> m_ErrorTextTMPs = new Dictionary<string, TextMeshProUGUI>();

        // Отслеживание порядка создания полей
        private List<string> m_CreatedFieldNames = new List<string>();
        private int m_FieldCreationOrder = 0;

        #region Unity Lifecycle

        private void Awake()
        {
            if (m_FieldRenderer == null)
                m_FieldRenderer = GetComponent<FieldRenderer>();

            if (m_ValidationService == null)
                m_ValidationService = FindAnyObjectByType<ValidationService>();

            if (m_FieldRenderer == null)
            {
                Debug.LogError("FieldRenderer не найден!");
                return;
            }

            if (m_ValidationService == null)
            {
                Debug.LogError("ValidationService не найден на сцене!");
                return;
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Создает текстовое поле с валидацией (расширенная версия метода из FieldRenderer)
        /// </summary>
        public void RenderTextFieldWithValidation(string fieldName, string displayName, Transform container, string value = "", bool isRequired = false)
        {
            // Запоминаем порядок создания полей
            m_CreatedFieldNames.Add(fieldName);
            m_FieldCreationOrder++;

            // Сначала создаем поле через оригинальный FieldRenderer
            m_FieldRenderer.RenderTextField(fieldName, displayName, container, value);

            // Затем добавляем валидацию
            if (m_EnableAutoValidation)
            {
                SetupFieldValidation(fieldName, container, isRequired);
            }
        }

        /// <summary>
        /// Создает поле даты с валидацией (расширенная версия метода из FieldRenderer)
        /// </summary>
        public void RenderDateFieldWithValidation(string fieldName, string displayName, Transform container, string value = "", bool isRequired = false)
        {
            // Запоминаем порядок создания полей
            m_CreatedFieldNames.Add(fieldName);
            m_FieldCreationOrder++;

            m_FieldRenderer.RenderDateField(fieldName, displayName, container, value);

            if (m_EnableAutoValidation)
            {
                SetupFieldValidation(fieldName, container, isRequired, true);
            }
        }

        /// <summary>
        /// Создает dropdown поле с валидацией (расширенная версия метода из FieldRenderer)
        /// </summary>
        public async Task RenderDropdownFieldWithValidation(string fieldName, string displayName, Transform container, Dictionary<string, string> options, string selectedValue = "", bool isRequired = false)
        {
            // Запоминаем порядок создания полей
            m_CreatedFieldNames.Add(fieldName);
            m_FieldCreationOrder++;

            await m_FieldRenderer.RenderDropdownField(fieldName, displayName, container, options, selectedValue);

            // Dropdown поля обычно не обязательны для заполнения (можно выбрать "Не выбрано")
            // Но если это требуется, можно передать isRequired = true
            if (m_EnableAutoValidation)
            {
                SetupFieldValidation(fieldName, container, isRequired);
            }
        }

        /// <summary>
        /// Устанавливает валидацию для поля
        /// </summary>
        public void SetupFieldValidation(string fieldName, Transform container, bool isRequired = false, bool isDateField = false)
        {
            // Ищем созданное поле в контейнере
            var fieldObj = FindFieldObject(fieldName, container);
            if (fieldObj == null)
            {
                Debug.LogWarning($"Поле '{fieldName}' не найдено в контейнере");
                return;
            }

            // fieldObj теперь это сам компонент поля (ValueInputField, ValueDropdown, ValueCalendar, ValueUpload и т.д.)
            var inputField = fieldObj.GetComponent<InputField>();
            var tmpInputField = fieldObj.GetComponent<TMP_InputField>();
            var dropdown = fieldObj.GetComponent<Dropdown>();
            var tmpDropdown = fieldObj.GetComponent<TMP_Dropdown>();
            var isValueCalendar = fieldObj.name == "ValueCalendar";
            var isValueUpload = fieldObj.name == "ValueUpload";

            Debug.Log($"🔧 Настройка валидации для поля '{fieldName}': {fieldObj.name}");
            Debug.Log($"    InputField: {(inputField != null ? "✅" : "❌")}");
            Debug.Log($"    TMP_InputField: {(tmpInputField != null ? "✅" : "❌")}");
            Debug.Log($"    Dropdown: {(dropdown != null ? "✅" : "❌")}");
            Debug.Log($"    TMP_Dropdown: {(tmpDropdown != null ? "✅" : "❌")}");
            Debug.Log($"    ValueCalendar: {(isValueCalendar ? "✅" : "❌")}");
            Debug.Log($"    ValueUpload: {(isValueUpload ? "✅" : "❌")}");

            // Получаем ErrorText из FieldRenderer - ИСПОЛЬЗУЕМ СОХРАНЕННЫЕ КОМПОНЕНТЫ
            var errorTextTMP = m_FieldRenderer.GetErrorTextForField(fieldName);
            if (errorTextTMP != null)
            {
                Debug.Log($"✅ Получен сохраненный ErrorText для поля '{fieldName}' из FieldRenderer");
            }
            else
            {
                Debug.LogWarning($"⚠️ Сохраненный ErrorText для поля '{fieldName}' не найден в FieldRenderer");
            }

            // Получаем правило валидации через ValidationService (учитывая текущую сущность)
            var rule = m_ValidationService.GetOrCreateRuleForField(fieldName);

            // Настраиваем валидацию - ПРОВЕРЯЕМ TMP_Dropdown ПЕРВЫМ, т.к. это основной тип dropdown
            if (tmpDropdown != null)
            {
                // Для TMP_Dropdown полей настраиваем валидацию
                SetupTMPDropdownValidation(fieldName, tmpDropdown, errorTextTMP, isRequired);
                if (errorTextTMP != null)
                {
                    m_ErrorTextTMPs[fieldName] = errorTextTMP;
                    m_ValidationService.AddErrorTextTMP(fieldName, errorTextTMP);
                }
                Debug.Log($"✅ Валидация настроена для TMP_Dropdown '{fieldName}' (тип поля: {rule.fieldType}, обяз.: {isRequired})");
            }
            else if (dropdown != null)
            {
                // Для стандартных Dropdown полей настраиваем валидацию
                SetupDropdownValidation(fieldName, dropdown, errorTextTMP, isRequired);
                if (errorTextTMP != null)
                {
                    m_ErrorTextTMPs[fieldName] = errorTextTMP;
                    m_ValidationService.AddErrorTextTMP(fieldName, errorTextTMP);
                }
                Debug.Log($"✅ Валидация настроена для Dropdown '{fieldName}' (тип поля: {rule.fieldType}, обяз.: {isRequired})");
            }
            else if (tmpInputField != null)
            {
                m_ValidationService.SetupTMPInputFieldValidation(fieldName, tmpInputField, errorTextTMP);
                if (errorTextTMP != null)
                {
                    m_ErrorTextTMPs[fieldName] = errorTextTMP;
                    m_ValidationService.AddErrorTextTMP(fieldName, errorTextTMP);
                }
                Debug.Log($"✅ Валидация настроена для TMP_InputField '{fieldName}'");
            }
            else if (inputField != null)
            {
                m_ValidationService.SetupInputFieldValidation(fieldName, inputField, null);
                if (errorTextTMP != null)
                {
                    m_ErrorTextTMPs[fieldName] = errorTextTMP;
                    m_ValidationService.AddErrorTextTMP(fieldName, errorTextTMP);
                }
                Debug.Log($"✅ Валидация настроена для InputField '{fieldName}'");
            }
            else if (isValueCalendar)
            {
                // Для полей даты настраиваем валидацию
                SetupCalendarValidation(fieldName, fieldObj, errorTextTMP, isRequired);
                if (errorTextTMP != null)
                {
                    m_ErrorTextTMPs[fieldName] = errorTextTMP;
                    m_ValidationService.AddErrorTextTMP(fieldName, errorTextTMP);
                }
                Debug.Log($"✅ Валидация настроена для Calendar '{fieldName}'");
            }
            else if (isValueUpload)
            {
                // Для полей загрузки изображений настраиваем валидацию
                SetupUploadValidation(fieldName, fieldObj, errorTextTMP, isRequired);
                if (errorTextTMP != null)
                {
                    m_ErrorTextTMPs[fieldName] = errorTextTMP;
                    m_ValidationService.AddErrorTextTMP(fieldName, errorTextTMP);
                }
                Debug.Log($"✅ Валидация настроена для Upload '{fieldName}'");
            }
            else
            {
                Debug.LogWarning($"⚠️ Для поля '{fieldName}' не найден подходящий компонент для валидации");
            }
        }

        /// <summary>
        /// Устанавливает текущую сущность для валидации
        /// </summary>
        public void SetCurrentEntity(string entityName)
        {
            if (m_ValidationService != null)
            {
                m_ValidationService.SetCurrentEntity(entityName);
                Debug.Log($"<color=cyan>🏢 FieldRendererValidationExtension: установлена сущность '{entityName}' для валидации</color>");
            }
            else
            {
                Debug.LogWarning("<color=orange>⚠️ ValidationService не доступен для установки сущности</color>");
            }
        }

        /// <summary>
        /// Валидирует все поля созданные через этот renderer
        /// </summary>
        public bool ValidateAllFields()
        {
            if (m_ValidationService == null) return true;

            Debug.Log($"<color=blue>🔍 FieldRendererValidationExtension: начинаем валидацию всех полей</color>");

            // Вызываем валидацию через ValidationService
            bool allValid = m_ValidationService.ValidateAllFields();

            return allValid;
        }

        /// <summary>
        /// Очищает список созданных полей (вызывать при открытии новой модальной формы)
        /// </summary>
        public void ClearCreatedFields()
        {
            Debug.Log("<color=blue>🔄 Очистка списка созданных полей</color>");
            m_CreatedFieldNames.Clear();
            m_FieldCreationOrder = 0;

            // Также очищаем ошибки
            m_ErrorTextTMPs.Clear();
        }

        /// <summary>
        /// Предоставляет существующий текст ошибки TMP для поля или создает новый
        /// Позволяет получить ссылку на ErrorText TMP элемент для внешнего управления
        /// </summary>
        public TextMeshProUGUI GetErrorTextTMP(string fieldName)
        {
            if (m_ErrorTextTMPs.TryGetValue(fieldName, out var errorTextTMP))
                return errorTextTMP;
            return null;
        }

        /// <summary>
        /// Устанавливает внешний ErrorText TMP элемент для поля
        /// </summary>
        public void SetExternalErrorTextTMP(string fieldName, TextMeshProUGUI externalErrorTextTMP)
        {
            if (externalErrorTextTMP != null)
            {
                m_ErrorTextTMPs[fieldName] = externalErrorTextTMP;
                Debug.Log($"🔗 Внешний ErrorText TMP установлен для поля '{fieldName}'");
            }
        }

        /// <summary>
        /// Предоставляет доступ к FieldRenderer (используется ValidationService)
        /// </summary>
        public FieldRenderer GetFieldRenderer()
        {
            return m_FieldRenderer;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Ищет объект поля по имени поля (используя порядок создания)
        /// </summary>
        private GameObject FindFieldObject(string fieldName, Transform container)
        {
            Debug.Log($"<color=yellow>🔍 Поиск поля '{fieldName}' в контейнере '{container.name}' (дочерних: {container.childCount})</color>");

            // Ищем индекс поля в списке созданных полей
            int fieldIndex = m_CreatedFieldNames.IndexOf(fieldName);
            if (fieldIndex == -1)
            {
                Debug.LogWarning($"<color=orange>⚠️ Поле '{fieldName}' не найдено в списке созданных полей</color>");
                return null;
            }

            Debug.Log($"<color=cyan>📌 Найден индекс поля '{fieldName}': {fieldIndex} (всего полей: {m_CreatedFieldNames.Count})</color>");

            // Ищем ValueItemPanel(Clone) объекты по порядку
            int cloneIndex = 0;
            for (int i = 0; i < container.childCount; i++)
            {
                var child = container.GetChild(i);
                Debug.Log($"  - Проверяем дочерний элемент {i}: '{child.name}' (индекс: {i})");

                // Нас интересуют только ValueItemPanel(Clone) объекты
                if (child.name.Contains("ValueItemPanel(Clone)"))
                {
                    Debug.Log($"    📍 Найден ValueItemPanel(Clone) #{cloneIndex}");

                    // Проверяем соответствует ли индекс
                    if (cloneIndex == fieldIndex)
                    {
                        // Ищем поля ввода внутри этого дочернего элемента
                        var fieldObj = FindFieldRecursive(fieldName, child);
                        if (fieldObj != null)
                        {
                            Debug.Log($"<color=green>✅ Поле '{fieldName}' найдено по индексу {fieldIndex} в ValueItemPanel(Clone) #{cloneIndex}: {fieldObj.name}</color>");
                            return fieldObj;
                        }
                    }
                    cloneIndex++;
                }
            }

            Debug.LogWarning($"<color=red>❌ Поле '{fieldName}' не найдено по индексу {fieldIndex} среди ValueItemPanel(Clone) объектов</color>");
            return null;
        }

        /// <summary>
        /// Находит InputField/Dropdown/Calendar в указанном ValueItemPanel контейнере
        /// </summary>
        private GameObject FindFieldRecursive(string fieldName, Transform parent)
        {
            Debug.Log($"    🔍 Ищем поле ввода в '{parent.name}' (дочерних: {parent.childCount})");

            // Проверяем дочерние элементы ValueItemPanel на наличие полей ввода
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                Debug.Log($"      - Проверяем '{child.name}'");

                // Проверяем является ли этот дочерний элемент полем ввода
                if (child.name == "ValueInputField")
                {
                    var inputField = child.GetComponent<InputField>();
                    var tmpInputField = child.GetComponent<TMP_InputField>();
                    if (inputField != null || tmpInputField != null)
                    {
                        Debug.Log($"        ✅ НАЙДЕНО InputField в '{child.name}' для поля '{fieldName}'");
                        return child.gameObject;
                    }
                }
                else if (child.name == "ValueDropdown")
                {
                    var dropdown = child.GetComponent<Dropdown>();
                    var tmpDropdown = child.GetComponent<TMP_Dropdown>();
                    if (dropdown != null || tmpDropdown != null)
                    {
                        Debug.Log($"        ✅ НАЙДЕНО Dropdown в '{child.name}' для поля '{fieldName}' (TMP: {tmpDropdown != null}, UI: {dropdown != null})");
                        return child.gameObject;
                    }
                }
                else if (child.name == "ValueCalendar")
                {
                    Debug.Log($"        ✅ НАЙДЕНО Calendar в '{child.name}' для поля '{fieldName}'");
                    return child.gameObject;
                }
                else if (child.name == "ValueUpload")
                {
                    Debug.Log($"        ✅ НАЙДЕНО Upload в '{child.name}' для поля '{fieldName}'");
                    return child.gameObject;
                }
            }

            Debug.LogWarning($"        ❌ Поля ввода не найдены в '{parent.name}'");
            return null;
        }

        /// <summary>
        /// Ищет существующий ErrorText TMP в активном поле (Input, Dropdown, Calendar, Upload)
        /// </summary>
        private TextMeshProUGUI FindErrorTextInActiveField(GameObject fieldObj)
        {
            if (fieldObj == null) return null;

            Debug.Log($"🔍 Поиск ErrorText TMP для активного поля '{fieldObj.name}'");

            // Ищем ErrorText внутри всех возможных типов полей по порядку

            // 1. Ищем внутри ValueInputField
            var inputField = fieldObj.transform.Find("ValueInputField");
            if (inputField != null && inputField.gameObject.activeSelf)
            {
                var errorTextTMP = inputField.Find("ErrorText")?.GetComponent<TextMeshProUGUI>();
                if (errorTextTMP != null)
                {
                    Debug.Log($"✅ Найден ErrorText TMP для поля '{fieldObj.name}' внутри ValueInputField");
                    return errorTextTMP;
                }
            }

            // 2. Ищем внутри ValueDropdown
            var dropdown = fieldObj.transform.Find("ValueDropdown");
            if (dropdown != null && dropdown.gameObject.activeSelf)
            {
                var errorTextTMP = dropdown.Find("ErrorText")?.GetComponent<TextMeshProUGUI>();
                if (errorTextTMP != null)
                {
                    Debug.Log($"✅ Найден ErrorText TMP для поля '{fieldObj.name}' внутри ValueDropdown");
                    return errorTextTMP;
                }
            }

            // 3. Ищем внутри ValueCalendar
            var calendar = fieldObj.transform.Find("ValueCalendar");
            if (calendar != null && calendar.gameObject.activeSelf)
            {
                var errorTextTMP = calendar.Find("ErrorText")?.GetComponent<TextMeshProUGUI>();
                if (errorTextTMP != null)
                {
                    Debug.Log($"✅ Найден ErrorText TMP для поля '{fieldObj.name}' внутри ValueCalendar");
                    return errorTextTMP;
                }
            }

            // 4. Ищем внутри ValueUpload
            var upload = fieldObj.transform.Find("ValueUpload");
            if (upload != null && upload.gameObject.activeSelf)
            {
                var errorTextTMP = upload.Find("ErrorText")?.GetComponent<TextMeshProUGUI>();
                if (errorTextTMP != null)
                {
                    Debug.Log($"✅ Найден ErrorText TMP для поля '{fieldObj.name}' внутри ValueUpload");
                    return errorTextTMP;
                }
            }

            // 5. Если fieldObj сам является полем (когда мы находимся внутри ValueItemPanel)
            if (fieldObj.name == "ValueInputField" || fieldObj.name == "ValueDropdown" ||
                fieldObj.name == "ValueCalendar" || fieldObj.name == "ValueUpload")
            {
                var errorTextTMP = fieldObj.transform.Find("ErrorText")?.GetComponent<TextMeshProUGUI>();
                if (errorTextTMP != null)
                {
                    Debug.Log($"✅ Найден ErrorText TMP непосредственно в поле '{fieldObj.name}'");
                    return errorTextTMP;
                }
            }

            // 6. Ищем ErrorText в других местах (fallback) - рекурсивный поиск
            var errorTextTMP_Fallback = FindErrorTextRecursive(fieldObj.transform);
            if (errorTextTMP_Fallback != null)
            {
                Debug.Log($"✅ Найден ErrorText TMP для поля '{fieldObj.name}' рекурсивным поиском");
                return errorTextTMP_Fallback;
            }

            Debug.LogWarning($"⚠️ ErrorText TMP не найден для поля '{fieldObj.name}' ни в одном из полей");
            return null;
        }

        /// <summary>
        /// Ищет существующий ErrorText TMP в иерархии поля (legacy method)
        /// </summary>
        private TextMeshProUGUI FindErrorTextInHierarchy(GameObject fieldObj)
        {
            if (fieldObj == null) return null;

            Debug.Log($"🔍 Поиск ErrorText TMP для поля '{fieldObj.name}' (дочерних: {fieldObj.transform.childCount})");

            // Ищем ErrorText в прямых дочерних элементах поля
            var errorTextTMP = fieldObj.transform.Find("ErrorText")?.GetComponent<TextMeshProUGUI>();
            if (errorTextTMP != null)
            {
                Debug.Log($"✅ Найден существующий ErrorText TMP для поля '{fieldObj.name}'");
                return errorTextTMP;
            }

            // Если не нашли в прямых дочерних, ищем глубже рекурсивно
            errorTextTMP = FindErrorTextRecursive(fieldObj.transform);
            if (errorTextTMP != null)
            {
                Debug.Log($"✅ Найден ErrorText TMP рекурсивно для поля '{fieldObj.name}'");
                return errorTextTMP;
            }

            // Если все еще не нашли, попробуем найти в родительских объектах
            var parent = fieldObj.transform.parent;
            while (parent != null)
            {
                errorTextTMP = parent.Find("ErrorText")?.GetComponent<TextMeshProUGUI>();
                if (errorTextTMP != null)
                {
                    Debug.Log($"✅ Найден ErrorText TMP в родительском объекте '{parent.name}' для поля '{fieldObj.name}'");
                    return errorTextTMP;
                }

                // Ищем в дочерних элементах родителя
                for (int i = 0; i < parent.childCount; i++)
                {
                    var child = parent.GetChild(i);
                    if (child.name.Contains("ErrorText") || child.name.Contains("error"))
                    {
                        errorTextTMP = child.GetComponent<TextMeshProUGUI>();
                        if (errorTextTMP != null)
                        {
                            Debug.Log($"✅ Найден ErrorText TMP в '{child.name}' для поля '{fieldObj.name}'");
                            return errorTextTMP;
                        }
                    }
                }

                parent = parent.parent;
            }

            Debug.LogWarning($"⚠️ ErrorText TMP не найден для поля '{fieldObj.name}'. Создание нового ErrorText может потребоваться.");
            return null;
        }

        /// <summary>
        /// Рекурсивный поиск ErrorText компонента
        /// </summary>
        private TextMeshProUGUI FindErrorTextRecursive(Transform parent)
        {
            for (int i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);

                // Проверяем сам объект
                if (child.name.Contains("ErrorText") || child.name.Contains("error"))
                {
                    var errorTextTMP = child.GetComponent<TextMeshProUGUI>();
                    if (errorTextTMP != null)
                    {
                        return errorTextTMP;
                    }
                }

                // Проверяем прямые дочерние элементы
                var directErrorText = child.Find("ErrorText")?.GetComponent<TextMeshProUGUI>();
                if (directErrorText != null)
                {
                    return directErrorText;
                }

                // Рекурсивный поиск в дочерних элементах
                var recursiveErrorText = FindErrorTextRecursive(child);
                if (recursiveErrorText != null)
                {
                    return recursiveErrorText;
                }
            }

            return null;
        }

        /// <summary>
        /// Настраивает валидацию для TMP_Dropdown поля
        /// </summary>
        private void SetupTMPDropdownValidation(string fieldName, TMP_Dropdown dropdown, TextMeshProUGUI errorTextTMP, bool isRequired)
        {
            // Добавляем обработчик изменения значения dropdown
            dropdown.onValueChanged.AddListener((value) => {
                ValidateTMPDropdownField(fieldName, dropdown, errorTextTMP, isRequired);
            });

            // Сохраняем ссылку на ErrorText для внешнего доступа
            if (errorTextTMP != null)
            {
                m_ErrorTextTMPs[fieldName] = errorTextTMP;
            }

            Debug.Log($"✅ Настроена валидация для TMP_Dropdown '{fieldName}', обязательное: {isRequired}");
        }

        /// <summary>
        /// Валидирует TMP_Dropdown поле
        /// </summary>
        private void ValidateTMPDropdownField(string fieldName, TMP_Dropdown dropdown, TextMeshProUGUI errorTextTMP, bool isRequired)
        {
            if (dropdown == null) return;

            // Получаем правило валидации для этого поля
            var rule = m_ValidationService?.GetOrCreateRuleForField(fieldName);
            if (rule == null)
            {
                Debug.LogWarning($"⚠️ Правило валидации не найдено для TMP_Dropdown поля '{fieldName}'");
                return;
            }

            bool isValid = true;
            string errorMessage = "";
            string selectedValue = "";

            // Получаем опции для определения выбранного значения
            if (m_FieldRenderer != null && m_FieldRenderer.DropdownOptions.TryGetValue(fieldName, out var options))
            {
                var keys = options.Keys.ToList();
                if (dropdown.value >= 0 && dropdown.value < keys.Count)
                {
                    selectedValue = keys[dropdown.value];

                    // Проверяем по правилу валидации через ValidationService
                    isValid = m_ValidationService.ValidateField(fieldName, selectedValue);
                    if (!isValid)
                    {
                        errorMessage = GetFieldErrorMessage(fieldName, rule, selectedValue);
                    }

                    Debug.Log($"🔍 TMP_Dropdown '{fieldName}': selectedValue='{selectedValue}', isValid={isValid}, isRequired={rule.isRequired}");
                }
                else
                {
                    // Если индекс вне диапазона - считаем невалидным если поле обязательное
                    isValid = !rule.isRequired;
                    if (!isValid)
                    {
                        errorMessage = GetFieldErrorMessage(fieldName, rule, "");
                    }
                    Debug.LogWarning($"⚠️ TMP_Dropdown '{fieldName}': индекс вне диапазона (value={dropdown.value}, keys.Count={keys.Count})");
                }
            }
            else
            {
                // Fallback: простая проверка на обязательность
                isValid = !isRequired || dropdown.value > 0;
                if (!isValid)
                {
                    errorMessage = "Это поле обязательно для заполнения";
                }
                Debug.LogWarning($"⚠️ TMP_Dropdown '{fieldName}': опции не найдены, используем fallback проверку");
            }

            // Дополнительная проверка: если поле обязательное и выбрано пустое значение - невалидно
            if (isValid && rule.isRequired && string.IsNullOrEmpty(selectedValue))
            {
                isValid = false;
                errorMessage = GetFieldErrorMessage(fieldName, rule, selectedValue);
                Debug.Log($"🔍 TMP_Dropdown '{fieldName}': обязательное поле с пустым значением - невалидно");
            }

            // Обновляем визуальное состояние dropdown
            var colors = dropdown.colors;
            colors.normalColor = isValid ? Color.white : new Color(1f, 0.9f, 0.9f);
            dropdown.colors = colors;

            // Показываем/скрываем ошибку
            if (errorTextTMP != null)
            {
                errorTextTMP.gameObject.SetActive(!isValid);
                if (!isValid)
                {
                    errorTextTMP.text = errorMessage;
                    errorTextTMP.color = Color.red;
                }
            }

            Debug.Log($"🔍 TMP_Dropdown '{fieldName}': value={dropdown.value}, selectedValue='{selectedValue}', valid={isValid}, error='{errorMessage}'");
        }

        /// <summary>
        /// Настраивает валидацию для Dropdown поля
        /// </summary>
        private void SetupDropdownValidation(string fieldName, Dropdown dropdown, TextMeshProUGUI errorTextTMP, bool isRequired)
        {
            // Добавляем обработчик изменения значения dropdown
            dropdown.onValueChanged.AddListener((value) => {
                ValidateDropdownField(fieldName, dropdown, errorTextTMP, isRequired);
            });

            // Сохраняем ссылку на ErrorText для внешнего доступа
            if (errorTextTMP != null)
            {
                m_ErrorTextTMPs[fieldName] = errorTextTMP;
            }

            Debug.Log($"✅ Настроена валидация для Dropdown '{fieldName}', обязательное: {isRequired}");
        }

        /// <summary>
        /// Валидирует Dropdown поле
        /// </summary>
        private void ValidateDropdownField(string fieldName, Dropdown dropdown, TextMeshProUGUI errorTextTMP, bool isRequired)
        {
            if (dropdown == null) return;

            // Получаем правило валидации для этого поля
            var rule = m_ValidationService?.GetOrCreateRuleForField(fieldName);
            if (rule == null)
            {
                Debug.LogWarning($"⚠️ Правило валидации не найдено для dropdown поля '{fieldName}'");
                return;
            }

            bool isValid = true;
            string errorMessage = "";
            string selectedValue = "";

            // Получаем опции для определения выбранного значения
            if (m_FieldRenderer != null && m_FieldRenderer.DropdownOptions.TryGetValue(fieldName, out var options))
            {
                var keys = options.Keys.ToList();
                if (dropdown.value >= 0 && dropdown.value < keys.Count)
                {
                    selectedValue = keys[dropdown.value];

                    // Проверяем по правилу валидации через ValidationService
                    isValid = m_ValidationService.ValidateField(fieldName, selectedValue);
                    if (!isValid)
                    {
                        errorMessage = GetFieldErrorMessage(fieldName, rule, selectedValue);
                    }

                    Debug.Log($"🔍 Dropdown '{fieldName}': selectedValue='{selectedValue}', isValid={isValid}, isRequired={rule.isRequired}");
                }
                else
                {
                    // Если индекс вне диапазона - считаем невалидным если поле обязательное
                    isValid = !rule.isRequired;
                    if (!isValid)
                    {
                        errorMessage = GetFieldErrorMessage(fieldName, rule, "");
                    }
                    Debug.LogWarning($"⚠️ Dropdown '{fieldName}': индекс вне диапазона (value={dropdown.value}, keys.Count={keys.Count})");
                }
            }
            else
            {
                // Fallback: простая проверка на обязательность
                isValid = !isRequired || dropdown.value > 0;
                if (!isValid)
                {
                    errorMessage = "Это поле обязательно для заполнения";
                }
                Debug.LogWarning($"⚠️ Dropdown '{fieldName}': опции не найдены, используем fallback проверку");
            }

            // Дополнительная проверка: если поле обязательное и выбрано пустое значение - невалидно
            if (isValid && rule.isRequired && string.IsNullOrEmpty(selectedValue))
            {
                isValid = false;
                errorMessage = GetFieldErrorMessage(fieldName, rule, selectedValue);
                Debug.Log($"🔍 Dropdown '{fieldName}': обязательное поле с пустым значением - невалидно");
            }

            // Обновляем визуальное состояние dropdown
            var colors = dropdown.colors;
            colors.normalColor = isValid ? Color.white : new Color(1f, 0.9f, 0.9f);
            dropdown.colors = colors;

            // Показываем/скрываем ошибку
            if (errorTextTMP != null)
            {
                errorTextTMP.gameObject.SetActive(!isValid);
                if (!isValid)
                {
                    errorTextTMP.text = errorMessage;
                    errorTextTMP.color = Color.red;
                }
            }

            Debug.Log($"🔍 Dropdown '{fieldName}': value={dropdown.value}, selectedValue='{selectedValue}', valid={isValid}, error='{errorMessage}'");
        }

        /// <summary>
        /// Настраивает валидацию для поля даты
        /// </summary>
        private void SetupCalendarValidation(string fieldName, GameObject calendarObj, TextMeshProUGUI errorTextTMP, bool isRequired)
        {
            var dateInputField = calendarObj.GetComponent<DateInputField>();
            if (dateInputField == null)
            {
                Debug.LogError($"DateInputField компонент не найден на объекте '{calendarObj.name}'");
                return;
            }

            // Добавляем обработчик изменения даты
            dateInputField.OnDateChanged += (dateValue) => {
                ValidateCalendarField(fieldName, dateInputField, errorTextTMP, isRequired);
            };

            // Добавляем обработчик валидации даты
            dateInputField.OnDateValidationChanged += (isValid) => {
                ValidateCalendarField(fieldName, dateInputField, errorTextTMP, isRequired);
            };

            // Также подписываемся на изменение InputField внутри DateInputField
            var inputField = dateInputField.GetInputField();
            if (inputField != null)
            {
                inputField.onValueChanged.AddListener((value) => {
                    ValidateCalendarField(fieldName, dateInputField, errorTextTMP, isRequired);
                });
                inputField.onEndEdit.AddListener((value) => {
                    ValidateCalendarField(fieldName, dateInputField, errorTextTMP, isRequired);
                });
            }

            Debug.Log($"✅ Настроена валидация для Calendar '{fieldName}', обязательное: {isRequired}");
        }

        /// <summary>
        /// Валидирует поле даты
        /// </summary>
        private void ValidateCalendarField(string fieldName, DateInputField dateInputField, TextMeshProUGUI errorTextTMP, bool isRequired)
        {
            if (dateInputField == null) return;

            // Получаем правило валидации для этого поля
            var rule = m_ValidationService?.GetOrCreateRuleForField(fieldName);
            if (rule == null)
            {
                Debug.LogWarning($"⚠️ Правило валидации не найдено для поля даты '{fieldName}'");
                return;
            }

            bool isValid = true;
            string errorMessage = "";
            string dateValue = dateInputField.GetDateValue();

            // Проверяем обязательность поля
            if (rule.isRequired && string.IsNullOrEmpty(dateValue))
            {
                isValid = false;
                errorMessage = GetFieldErrorMessage(fieldName, rule, dateValue);
                Debug.Log($"🔍 Calendar '{fieldName}': обязательное поле с пустой датой - невалидно");
            }
            else if (!string.IsNullOrEmpty(dateValue))
            {
                // Проверяем валидность даты через ValidationService
                isValid = m_ValidationService.ValidateField(fieldName, dateValue);
                if (!isValid)
                {
                    errorMessage = GetFieldErrorMessage(fieldName, rule, dateValue);
                }
                Debug.Log($"🔍 Calendar '{fieldName}': dateValue='{dateValue}', isValid={isValid}, isRequired={rule.isRequired}");
            }

            // Обновляем визуальное состояние поля даты
            var inputField = dateInputField.GetInputField();
            if (inputField != null)
            {
                var colors = inputField.colors;
                colors.normalColor = isValid ? Color.white : new Color(1f, 0.9f, 0.9f);
                inputField.colors = colors;
            }

            // Показываем/скрываем ошибку
            if (errorTextTMP != null)
            {
                errorTextTMP.gameObject.SetActive(!isValid);
                if (!isValid)
                {
                    errorTextTMP.text = errorMessage;
                    errorTextTMP.color = Color.red;
                }
            }

            Debug.Log($"🔍 Calendar '{fieldName}': date='{dateValue}', valid={isValid}, error='{errorMessage}'");
        }

        /// <summary>
        /// Настраивает валидацию для поля загрузки изображений
        /// </summary>
        private void SetupUploadValidation(string fieldName, GameObject uploadObj, TextMeshProUGUI errorTextTMP, bool isRequired)
        {
            // Для полей загрузки изображений добавляем специальную логику валидации
            // Поле загрузки считается валидным, если изображение выбрано или загружено
            Debug.Log($"✅ Настроена валидация для Upload '{fieldName}', обязательное: {isRequired}");
        }

        /// <summary>
        /// Получает сообщение об ошибке для поля
        /// </summary>
        private string GetFieldErrorMessage(string fieldName, ValidationRule rule, string currentValue)
        {
            // СНАЧАЛА проверяем ValidationFieldCatalog
            var entityName = m_ValidationService?.GetPrivateCurrentEntity();
            if (!string.IsNullOrEmpty(entityName))
            {
                // Нормализуем имя сущности для поиска в ValidationFieldCatalog
                string normalizedEntityName = NormalizeEntityName(entityName);
                string catalogMessage = ValidationFieldCatalog.GetFieldErrorMessage(normalizedEntityName, fieldName, currentValue);

                if (!string.IsNullOrEmpty(catalogMessage))
                {
                    Debug.Log($"<color=green>✅ Использовано сообщение из ValidationFieldCatalog для dropdown '{normalizedEntityName}.{fieldName}': '{catalogMessage}'</color>");
                    return catalogMessage;
                }
            }

            // Если сообщение не найдено в каталоге, используем правило
            if (!string.IsNullOrEmpty(rule.errorMessage))
                return rule.errorMessage;

            if (rule.isRequired && string.IsNullOrEmpty(currentValue))
                return "Это поле обязательно для заполнения";

            // Fallback сообщения
            return "Некорректное значение";
        }

        /// <summary>
        /// Нормализует имя сущности для соответствия с ValidationFieldCatalog
        /// </summary>
        private string NormalizeEntityName(string entityName)
        {
            if (string.IsNullOrEmpty(entityName))
                return entityName;

            string normalized = entityName.ToLower();

            // Конкретные маппинги для несоответствий
            switch (normalized)
            {
                // Основные сущности (единственное → множественное число)
                case "pcb":
                case "pcbs":
                    return "pcb_orders";

                case "component":
                    return "components";

                case "stand":
                    return "stands";

                case "organization":
                    return "organizations";

                case "people":
                case "peoplee":
                    return "peoples";

                case "employee":
                    return "employees";

                case "shipment":
                    return "shipments";

                case "image":
                    return "images";

                case "department":
                    return "departments";

                case "profession":
                    return "professions";

                // Сущности с underscore (разные варианты написания)
                case "arrivalinvoice":
                case "arrivalinvoices":
                    return "arrival_invoices";

                case "orderrequest":
                case "orderrequests":
                    return "order_requests";

                case "currenttask":
                case "currenttasks":
                    return "current_tasks";

                // Новые сущности из бэкенда
                case "currenttaskstate":
                case "currenttaskstates":
                    return "current_task_states";

                case "standtask":
                case "standtasks":
                    return "stand_tasks";

                case "organizationtype":
                case "organizationtypes":
                    return "organization_types";

                case "standtype":
                case "standtypes":
                    return "stand_types";

                case "user":
                    return "users";

                case "pcborderstate":
                case "pcborderstates":
                    return "pcb_order_states";

                case "licensetype":
                case "licensetypes":
                    return "license_types";

                case "componentplacement":
                case "componentplacements":
                    return "component_placements";

                case "writeoff":
                    return "writeoff";

                case "writeoffreason":
                case "writeoffreasons":
                    return "writeoff_reasons";

                case "billforpay":
                case "billsforpay":
                    return "bills_for_pay";

                case "billcomponent":
                case "billscomponents":
                    return "bills_components";

                case "componentplacementtype":
                    return "component_placement_type";

                case "serverarrival":
                case "serverarrivals":
                    return "server_arrivals";

                case "serverwriteoff":
                    return "server_writeoff";

                case "shipmentpackage":
                case "shipmentpackages":
                    return "shipment_package";

                case "shipmentpackagestate":
                case "shipmentpackagestates":
                    return "shipment_package_states";

                case "shipmenttrip":
                case "shipmenttrips":
                    return "shipment_trips";

                case "shipmentstand":
                case "shipmentstands":
                    return "shipment_stands";

                case "standpackage":
                case "standpackages":
                    return "stand_packages";

                case "employeesdepartment":
                case "employeesdepartments":
                    return "employees_departments";

                case "employeesprofession":
                case "employeesprofessions":
                    return "employees_professions";

                case "employeesvacation":
                case "employeesvacations":
                    return "employees_vacations";

                case "invoicecomponent":
                case "invoicescomponents":
                    return "invoices_components";

                case "currenttaskcomponent":
                case "currenttaskscomponents":
                    return "current_tasks_components";

                case "orderrequestcomponent":
                case "orderrequestscomponents":
                    return "order_requests_components";

                case "pcbcomponent":
                case "pcbscomponents":
                    return "pcbs_components";

                case "standtaskcomponent":
                case "standtaskscomponents":
                    return "stand_tasks_components";

                case "suppliercomponent":
                case "suppliercomponents":
                    return "supplier_components";

                case "currenttaskstatelog":
                case "currenttaskstateslog":
                    return "current_task_states_log";

                case "inventarization":
                    return "inventarization";

                // Сущности которые уже в правильном формате
                case "license":
                case "images":
                case "arrival_invoices":
                case "current_tasks":
                case "organizations":
                case "peoples":
                case "employees":
                case "components":
                case "stands":
                case "shipments":
                case "pcb_orders":
                case "departments":
                case "professions":
                case "order_requests":
                case "current_task_states":
                case "stand_tasks":
                case "organization_types":
                case "stand_types":
                case "users":
                case "pcb_order_states":
                case "license_types":
                case "component_placements":
                case "writeoff_reasons":
                case "bills_for_pay":
                case "bills_components":
                case "component_placement_type":
                case "server_arrivals":
                case "server_writeoff":
                case "shipment_package":
                case "shipment_package_states":
                case "shipment_trips":
                case "shipment_stands":
                case "stand_packages":
                case "employees_departments":
                case "employees_professions":
                case "employees_vacations":
                case "invoices_components":
                case "current_tasks_components":
                case "order_requests_components":
                case "pcbs_components":
                case "stand_tasks_components":
                case "supplier_components":
                case "current_task_states_log":
                    return normalized;

                default:
                    Debug.LogWarning($"<color=orange>⚠️ Неизвестная сущность '{entityName}' - используем как есть</color>");
                    return normalized;
            }
        }

        #endregion
    }
}