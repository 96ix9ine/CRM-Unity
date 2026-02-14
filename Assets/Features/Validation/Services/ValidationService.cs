using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Validation.Models;
using Validation.Components;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;
using System;
using System.Globalization;

namespace Validation.Services
{
    /// <summary>
    /// Сервис валидации полей ввода
    /// </summary>
    public class ValidationService : MonoBehaviour
    {
        [Header("Validation Settings")]
        [SerializeField] private Color m_ValidColor = Color.white;
        [SerializeField] private Color m_InvalidColor = new Color(1f, 0.9f, 0.9f);
        [SerializeField] private Color m_ErrorTextColor = Color.red;

        [Header("Entity Settings")]
        [SerializeField] private string m_CurrentEntityName = ""; // Имя текущей сущности

        private Dictionary<string, ValidationRule> m_ValidationRules = new Dictionary<string, ValidationRule>();
        private Dictionary<string, Text> m_ErrorTexts = new Dictionary<string, Text>();
        private Dictionary<string, InputField> m_InputFields = new Dictionary<string, InputField>();
        private Dictionary<string, TMP_InputField> m_TMPInputFields = new Dictionary<string, TMP_InputField>();
        private Dictionary<string, TextMeshProUGUI> m_ErrorTextTMPs = new Dictionary<string, TextMeshProUGUI>();

        // Флаг отслеживания первой валидации
        private bool m_HasValidatedOnce = false;

        #region Unity Lifecycle

        private void Awake()
        {
            Debug.Log($"<color=green>✅ ValidationService инициализирован</color>\n" +
                     $"- Valid Color: {m_ValidColor}\n" +
                     $"- Invalid Color: {m_InvalidColor}\n" +
                     $"- Error Text Color: {m_ErrorTextColor}");
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Добавляет правило валидации для поля
        /// </summary>
        public void AddValidationRule(string fieldName, ValidationRule rule)
        {
            m_ValidationRules[fieldName] = rule;
        }

        /// <summary>
        /// Добавляет правило валидации на основе типа поля
        /// </summary>
        public void AddValidationRule(string fieldName, bool isRequired = false)
        {
            var rule = FieldTypeResolver.CreateValidationRule(fieldName, isRequired);
            m_ValidationRules[fieldName] = rule;
        }

        /// <summary>
        /// Устанавливает обязательность для существующего правила валидации
        /// </summary>
        public void SetFieldRequired(string fieldName, bool isRequired)
        {
            if (m_ValidationRules.TryGetValue(fieldName, out ValidationRule rule))
            {
                rule.isRequired = isRequired;
                Debug.Log($"<color=cyan>🔧 Поле '{fieldName}' установлено как {(isRequired ? "обязательное" : "необязательное")}</color>");
            }
            else
            {
                Debug.LogWarning($"<color=orange>⚠️ Правило для поля '{fieldName}' не найдено</color>");
            }
        }

        /// <summary>
        /// Делает указанные поля обязательными для заполнения
        /// </summary>
        public void SetFieldsRequired(string[] fieldNames)
        {
            foreach (string fieldName in fieldNames)
            {
                SetFieldRequired(fieldName, true);
            }
        }

        /// <summary>
        /// Настраивает валидацию для InputField
        /// </summary>
        public void SetupInputFieldValidation(string fieldName, InputField inputField, Text errorText = null)
        {
            if (inputField == null) return;

            var rule = GetOrCreateRule(fieldName);

            // Принудительно устанавливаем contentType в соответствии с правилом валидации
            // Это должно переопределить любую предыдущую настройку
            SetupInputFieldType(inputField, rule.fieldType);

            Debug.Log($"<color=cyan>🔧 Настройка валидации для поля '{fieldName}'</color>\n" +
                     $"- Тип: {rule.fieldType}\n" +
                     $"- ContentType: {GetContentType(rule.fieldType)} (установлен принудительно)\n" +
                     $"- Обязательное: {rule.isRequired}\n" +
                     $"- MinLength: {rule.minLength}, MaxLength: {rule.maxLength}");

            // Добавляем обработчик изменения текста
            inputField.onValueChanged.AddListener((value) => {
                ValidateInputField(fieldName, inputField, errorText);
            });

            // Сохраняем ссылку на текст ошибки
            if (errorText != null)
            {
                m_ErrorTexts[fieldName] = errorText;
                errorText.gameObject.SetActive(false);
            }

            // Сохраняем ссылку на InputField для валидации всех полей
            m_InputFields[fieldName] = inputField;

            // НЕ делаем начальную валидацию - ждем первого взаимодействия пользователя
        }

        /// <summary>
        /// Настраивает валидацию для TMP_InputField
        /// </summary>
        public void SetupTMPInputFieldValidation(string fieldName, TMP_InputField inputField, TextMeshProUGUI errorText = null)
        {
            if (inputField == null) return;

            var rule = GetOrCreateRule(fieldName);
            SetupTMPInputFieldType(inputField, rule.fieldType);

            // Добавляем обработчик изменения текста
            inputField.onValueChanged.AddListener((value) => {
                ValidateTMPInputField(fieldName, inputField, errorText);
            });

            // Сохраняем ссылку на TMP InputField для валидации всех полей
            m_TMPInputFields[fieldName] = inputField;

            // Скрываем ErrorText изначально
            if (errorText != null)
            {
                errorText.gameObject.SetActive(false);
                m_ErrorTextTMPs[fieldName] = errorText;
            }

            // НЕ делаем начальную валидацию - ждем первого взаимодействия пользователя
        }

        /// <summary>
        /// Валидирует все поля с ожиданием регистрации полей в FieldRenderer
        /// </summary>
        public bool ValidateAllFields()
        {
            // Ждем регистрации полей в FieldRenderer (исправление race condition)
            if (!WaitForFieldsRegistration())
            {
                Debug.LogWarning("<color=orange>⚠️ Не удалось дождаться регистрации полей, валидация может быть неполной</color>");
            }

            bool allValid = true;
            m_HasValidatedOnce = true; // Отмечаем что валидация была запущена

            Debug.Log($"<color=blue>🔍 Начинаем валидацию всех полей ({m_ValidationRules.Count} правил)</color>");

            foreach (var kvp in m_ValidationRules)
            {
                string fieldName = kvp.Key;
                var rule = kvp.Value;

                // Ищем связанное поле ввода для получения текущего значения
                string currentValue = GetFieldValue(fieldName);

                // Добавляем детальное логирование для диагностики
                Debug.Log($"<color=yellow>🔍 Анализ поля '{fieldName}':</color>\n" +
                         $"  - Правило: Required={rule.isRequired}, Type={rule.fieldType}\n" +
                         $"  - Текущее значение: '{currentValue}' (null: {currentValue == null})\n" +
                         $"  - Поле найдено в UI: {HasField(fieldName)}");

                // Если поле не найдено, но имеет правило валидации - считаем это ошибкой
                if (currentValue == null && !HasField(fieldName))
                {
                    Debug.LogWarning($"<color=orange>⚠️ Поле '{fieldName}' имеет правило валидации, но не найдено в UI. Считаем ошибкой.</color>");

                    if (rule.isRequired)
                    {
                        allValid = false;
                        // Показываем ошибку для не найденного обязательного поля
                        if (m_ErrorTextTMPs.TryGetValue(fieldName, out var errorTextTMP))
                        {
                            errorTextTMP.gameObject.SetActive(true);
                            errorTextTMP.text = "Поле не найдено в форме";
                            errorTextTMP.color = m_ErrorTextColor;
                        }
                        if (m_ErrorTexts.TryGetValue(fieldName, out var errorText))
                        {
                            errorText.gameObject.SetActive(true);
                            errorText.text = "Поле не найдено в форме";
                            errorText.color = m_ErrorTextColor;
                        }
                    }
                    continue;
                }

                bool isValid = ValidateValue(currentValue, rule);

                // Детальное логирование результата валидации
                if (rule.isRequired && string.IsNullOrEmpty(currentValue))
                {
                    Debug.Log($"<color=red>❌ Поле '{fieldName}' обязательно для заполнения, но значение пустое</color>");
                }
                else if (!string.IsNullOrEmpty(currentValue) && !isValid)
                {
                    Debug.Log($"<color=red>❌ Поле '{fieldName}' не прошло валидацию по формату: '{currentValue}' (тип: {rule.fieldType})</color>");
                }
                else
                {
                    Debug.Log($"<color=green>✅ Поле '{fieldName}': '{currentValue}' -> Валидно</color>");
                }

                // Обновляем цвет поля
                UpdateFieldVisualState(fieldName, isValid);

                if (!isValid)
                {
                    allValid = false;

                    Debug.Log($"<color=red>❌ Поле '{fieldName}' не прошло валидацию: '{currentValue}'</color>");

                    // Показываем ошибку для Text
                    if (m_ErrorTexts.TryGetValue(fieldName, out var errorText))
                    {
                        errorText.gameObject.SetActive(true);
                        errorText.text = GetErrorMessage(rule, currentValue);
                        errorText.color = m_ErrorTextColor;
                        Debug.Log($"  ✅ ErrorText активирован для '{fieldName}'");
                    }

                    // Показываем ошибку для TMP
                    if (m_ErrorTextTMPs.TryGetValue(fieldName, out var errorTextTMP))
                    {
                        if (errorTextTMP != null && errorTextTMP.gameObject != null)
                        {
                            errorTextTMP.gameObject.SetActive(true);
                            errorTextTMP.text = GetErrorMessage(rule, currentValue);
                            errorTextTMP.color = m_ErrorTextColor;
                            Debug.Log($"  ✅ ErrorTextTMP активирован для '{fieldName}'");
                        }
                        else
                        {
                            // Очищаем словарь от уничтоженных объектов
                            m_ErrorTextTMPs.Remove(fieldName);
                            Debug.Log($"<color=orange>🧹 Очищен уничтоженный ErrorTextTMP для поля '{fieldName}' при показе ошибки</color>");
                        }
                    }
                }
                else
                {
                    Debug.Log($"<color=green>✅ Поле '{fieldName}' прошло валидацию: '{currentValue}'</color>");

                    // Скрываем ошибку если поле валидно
                    if (m_ErrorTexts.TryGetValue(fieldName, out var errorText))
                    {
                        errorText.gameObject.SetActive(false);
                        Debug.Log($"  ✅ ErrorText скрыт для '{fieldName}'");
                    }

                    if (m_ErrorTextTMPs.TryGetValue(fieldName, out var errorTextTMP) && errorTextTMP != null && errorTextTMP.gameObject != null)
                    {
                        errorTextTMP.gameObject.SetActive(false);
                        Debug.Log($"  ✅ ErrorTextTMP скрыт для '{fieldName}'");
                    }
                    else
                    {
                        // Очищаем словарь от уничтоженных объектов
                        if (errorTextTMP == null)
                        {
                            m_ErrorTextTMPs.Remove(fieldName);
                            Debug.Log($"<color=orange>🧹 Очищен уничтоженный ErrorTextTMP для поля '{fieldName}'</color>");
                        }
                    }
                }
            }

            Debug.Log($"<color={(allValid ? "green" : "red")}>🏁 Результат валидации всех полей: {(allValid ? "✅ Все поля валидны" : "❌ Есть ошибки")}</color>");

            return allValid;
        }

        /// <summary>
        /// Проверяет есть ли поле с таким именем в валидаторе
        /// </summary>
        private bool HasField(string fieldName)
        {
            // Проверяем InputField поля в локальных словарях
            if (m_InputFields.ContainsKey(fieldName) || m_TMPInputFields.ContainsKey(fieldName))
                return true;

            // Проверяем поля через FieldRendererValidationExtension
            var fieldRendererExtension = FindAnyObjectByType<FieldRendererValidationExtension>();
            if (fieldRendererExtension != null)
            {
                var fieldRenderer = fieldRendererExtension.GetFieldRenderer();
                if (fieldRenderer != null)
                {
                    // СНАЧАЛА проверяем InputField поля в FieldRenderer
                    if (fieldRenderer.InputFields.ContainsKey(fieldName))
                        return true;

                    // Проверяем TMP_InputField поля
                    // (могут быть в fieldRenderer но не в локальных словарях)

                    // Проверяем dropdown поля
                    if (fieldRenderer.DropdownFields.ContainsKey(fieldName))
                        return true;

                    // Проверяем поля дат
                    var calendarFields = fieldRenderer.DateFields;
                    if (calendarFields != null && calendarFields.ContainsKey(fieldName))
                        return true;

                    // Также проверяем опции dropdown (для полей которые существуют через опции)
                    if (fieldRenderer.DropdownOptions.ContainsKey(fieldName))
                        return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Обновляет визуальное состояние поля (цвет)
        /// </summary>
        private void UpdateFieldVisualState(string fieldName, bool isValid)
        {
            // Ищем InputField
            if (m_InputFields.TryGetValue(fieldName, out var inputField) && inputField != null)
            {
                var colors = inputField.colors;
                colors.normalColor = isValid ? m_ValidColor : m_InvalidColor;
                inputField.colors = colors;
                Debug.Log($"✅ Обновлен цвет InputField '{fieldName}': {(isValid ? "валидный" : "невалидный")}");
            }

            // Ищем TMP InputField
            if (m_TMPInputFields.TryGetValue(fieldName, out var tmpInputField) && tmpInputField != null)
            {
                var colors = tmpInputField.colors;
                colors.normalColor = isValid ? m_ValidColor : m_InvalidColor;
                tmpInputField.colors = colors;
                Debug.Log($"✅ Обновлен цвет TMP_InputField '{fieldName}': {(isValid ? "валидный" : "невалидный")}");
            }

            // Ищем Dropdown поля через FieldRendererValidationExtension
            var fieldRendererExtension = FindAnyObjectByType<FieldRendererValidationExtension>();
            if (fieldRendererExtension != null)
            {
                var dropdowns = fieldRendererExtension.GetFieldRenderer().DropdownFields;
                if (dropdowns.TryGetValue(fieldName, out var dropdown) && dropdown != null)
                {
                    var colors = dropdown.colors;
                    colors.normalColor = isValid ? m_ValidColor : m_InvalidColor;
                    dropdown.colors = colors;
                    Debug.Log($"✅ Обновлен цвет Dropdown '{fieldName}': {(isValid ? "валидный" : "невалидный")}");
                }

                // Проверяем поля дат через FieldRendererValidationExtension
                var calendarFields = fieldRendererExtension.GetFieldRenderer().DateFields;
                if (calendarFields != null && calendarFields.TryGetValue(fieldName, out var dateInputField) && dateInputField != null)
                {
                    var calendarInputField = dateInputField.GetInputField();
                    if (calendarInputField != null)
                    {
                        var colors = calendarInputField.colors;
                        colors.normalColor = isValid ? m_ValidColor : m_InvalidColor;
                        calendarInputField.colors = colors;
                        Debug.Log($"✅ Обновлен цвет Calendar '{fieldName}': {(isValid ? "валидный" : "невалидный")}");
                    }
                }
            }
        }

        /// <summary>
        /// Получает текущее значение поля по имени
        /// </summary>
        private string GetFieldValue(string fieldName)
        {
            Debug.Log($"<color=cyan>🔍 GetFieldValue: ищем поле '{fieldName}'</color>");

            // Ищем в стандартных InputField
            if (m_InputFields.TryGetValue(fieldName, out var inputField) && inputField != null)
            {
                Debug.Log($"<color=green>✅ Найден InputField '{fieldName}': '{inputField.text}'</color>");
                return inputField.text;
            }

            // Ищем в TMP InputField
            if (m_TMPInputFields.TryGetValue(fieldName, out var tmpInputField) && tmpInputField != null)
            {
                Debug.Log($"<color=green>✅ Найден TMP_InputField '{fieldName}': '{tmpInputField.text}'</color>");
                return tmpInputField.text;
            }

            // ИЩЕМ В ПОЛЯХ ЧЕРЕЗ FieldRendererValidationExtension (ДО проверки dropdown)
            var fieldRendererExtension = FindAnyObjectByType<FieldRendererValidationExtension>();
            if (fieldRendererExtension != null)
            {
                var fieldRenderer = fieldRendererExtension.GetFieldRenderer();
                if (fieldRenderer != null)
                {
                    // СНАЧАЛА проверяем InputField в FieldRenderer
                    if (fieldRenderer.InputFields.TryGetValue(fieldName, out var rendererInputField) && rendererInputField != null)
                    {
                        Debug.Log($"<color=green>✅ Найден InputField в FieldRenderer '{fieldName}': '{rendererInputField.text}'</color>");
                        return rendererInputField.text;
                    }
                }
            }

            // Используем уже найденный fieldRendererExtension для поиска dropdown полей
            if (fieldRendererExtension != null)
            {
                var fieldRenderer = fieldRendererExtension.GetFieldRenderer();
                if (fieldRenderer != null)
                {
                    // Проверяем dropdown поля
                    if (fieldRenderer.DropdownOptions.TryGetValue(fieldName, out var options))
                    {
                        Debug.Log($"<color=blue>🔍 GetFieldValue: найдены опции для dropdown '{fieldName}' ({options.Count()} опций)</color>");
                        Debug.Log($"<color=blue>🔍 Опции dropdown '{fieldName}': {string.Join(", ", options.Select(kv => $"{kv.Key}='{kv.Value}'"))}</color>");

                        // Ищем TMP_Dropdown поля (основной тип в проекте)
                        var tmpDropdowns = fieldRenderer.DropdownFields;
                        if (tmpDropdowns != null && tmpDropdowns.TryGetValue(fieldName, out var tmpDropdown) && tmpDropdown != null)
                        {
                            var keys = options.Keys.ToList();
                            Debug.Log($"<color=blue>🔍 GetFieldValue: найден TMP_Dropdown '{fieldName}', value={tmpDropdown.value}, keys.Count={keys.Count}</color>");
                            Debug.Log($"<color=blue>🔍 Ключи опций: [{string.Join(", ", keys)}]</color>");

                            if (tmpDropdown.value >= 0 && tmpDropdown.value < keys.Count)
                            {
                                var selectedKey = keys[tmpDropdown.value];
                                Debug.Log($"<color=blue>🔍 GetFieldValue: выбранный ключ для '{fieldName}' = '{selectedKey}' (index: {tmpDropdown.value})</color>");

                                // Для полей supplierId и factoryId разрешаем ID=0 как валидное значение
                                bool isIdField = fieldName.EndsWith("Id") || fieldName.ToLower().Contains("id");
                                bool isEmptyValue = string.IsNullOrEmpty(selectedKey) || selectedKey == "-1";

                                // Для ID полей считаем пустыми только строковые пустые значения и -1
                                if (isIdField)
                                {
                                    isEmptyValue = string.IsNullOrEmpty(selectedKey) || selectedKey == "-1";
                                }
                                else
                                {
                                    // Для не-ID полей считаем пустыми также и "0"
                                    isEmptyValue = string.IsNullOrEmpty(selectedKey) || selectedKey == "0" || selectedKey == "-1";
                                }

                                if (isEmptyValue)
                                {
                                    Debug.Log($"<color=orange>⚠️ GetFieldValue: dropdown '{fieldName}' имеет пустое значение '{selectedKey}' (isIdField: {isIdField})</color>");
                                    return "";  // Пустое значение для невалидного dropdown
                                }
                                Debug.Log($"<color=green>✅ GetFieldValue: dropdown '{fieldName}' возвращает '{selectedKey}' (isIdField: {isIdField})</color>");
                                return selectedKey;  // Валидное значение
                            }
                            Debug.Log($"<color=orange>⚠️ GetFieldValue: dropdown '{fieldName}' value={tmpDropdown.value} вне диапазона keys.Count={keys.Count}</color>");
                            return ""; // Пустое значение для невалидного dropdown
                        }
                    }

                    // Если TMP_Dropdown не найден, ищем стандартный Dropdown в иерархии
                    if (fieldRenderer != null)
                    {
                        // Ищем через ValidationExtension, который может найти поля в иерархии
                        var createdFields = GetPrivateCreatedFields(fieldRendererExtension);
                        if (createdFields != null && createdFields.Contains(fieldName))
                        {
                            // Пытаемся найти dropdown в контейнере через тот же механизм что и валидация
                            var dropdownContainer = FindDropdownContainer(fieldName, fieldRendererExtension);
                            if (dropdownContainer != null)
                            {
                                var standardDropdown = dropdownContainer.GetComponent<Dropdown>();
                                if (standardDropdown != null)
                                {
                                    var keys = options.Keys.ToList();
                                    if (standardDropdown.value >= 0 && standardDropdown.value < keys.Count)
                                    {
                                        var selectedKey = keys[standardDropdown.value];
                                        if (string.IsNullOrEmpty(selectedKey) || selectedKey == "0" || selectedKey == "-1")
                                        {
                                            return "";
                                        }
                                        return selectedKey;
                                    }
                                    return "";
                                }
                            }
                        }
                    }

                    Debug.LogWarning($"<color=orange>⚠️ Dropdown поле '{fieldName}' не найдено ни в TMP_Dropdown ни в стандартных Dropdown</color>");
                }

                // Проверяем поля дат через FieldRendererValidationExtension
                var calendarFields = fieldRendererExtension.GetFieldRenderer().DateFields;
                if (calendarFields != null && calendarFields.TryGetValue(fieldName, out var dateInputField) && dateInputField != null)
                {
                    string dateValue = dateInputField.GetDateValue();
                    Debug.Log($"<color=green>✅ Найдено DateField '{fieldName}': '{dateValue}'</color>");

                    // Детальная диагностика для полей дат
                    if (string.IsNullOrEmpty(dateValue))
                    {
                        Debug.Log($"<color=orange>⚠️ DateField '{fieldName}' имеет пустое значение</color>");
                    }
                    else
                    {
                        Debug.Log($"<color=green>✅ DateField '{fieldName}' содержит валидную дату: '{dateValue}'</color>");
                    }

                    return dateValue;
                }
                else
                {
                    Debug.Log($"<color=orange>⚠️ DateField '{fieldName}' не найден в calendarFields (total: {calendarFields?.Count ?? 0})</color>");
                }
            }

            Debug.LogWarning($"<color=orange>⚠️ Поле '{fieldName}' не найдено в словарях валидации</color>\n" +
                           $"- Текущая сущность: '{m_CurrentEntityName}' (нормализовано: '{NormalizeEntityName(m_CurrentEntityName)}')\n" +
                           $"- Поиск в: InputFields({m_InputFields.Count}), TMPInputFields({m_TMPInputFields.Count})\n" +
                           $"- FieldRendererExtension: {(FindAnyObjectByType<FieldRendererValidationExtension>() != null ? "найден" : "не найден")}");
            return null; // Возвращаем null чтобы отличить не найденное поле от пустого поля
        }

        /// <summary>
        /// Валидирует конкретное поле
        /// </summary>
        public bool ValidateField(string fieldName, string value)
        {
            if (!m_ValidationRules.TryGetValue(fieldName, out ValidationRule rule))
                return true; // Нет правила - нет валидации

            return ValidateValue(value, rule);
        }

        /// <summary>
        /// Получает тип контента для InputField на основе типа поля
        /// </summary>
        public InputField.ContentType GetContentType(FieldType fieldType)
        {
            switch (fieldType)
            {
                case FieldType.Integer:
                    return InputField.ContentType.IntegerNumber;
                case FieldType.Float:
                case FieldType.Decimal:
                    return InputField.ContentType.DecimalNumber;
                case FieldType.Email:
                    return InputField.ContentType.EmailAddress;
                case FieldType.Phone:
                    return InputField.ContentType.Name; // Для телефонов используем alphanumeric
                case FieldType.Inn:
                case FieldType.Kpp:
                case FieldType.Ogrn:
                    return InputField.ContentType.IntegerNumber;
                case FieldType.Url:
                    return InputField.ContentType.Standard;
                default:
                    return InputField.ContentType.Standard;
            }
        }

        /// <summary>
        /// Очищает ресурсы для указанного поля
        /// </summary>
        public void ClearField(string fieldName)
        {
            // Удаляем правила валидации
            m_ValidationRules.Remove(fieldName);

            // Удаляем текст ошибки
            if (m_ErrorTexts.ContainsKey(fieldName))
            {
                var errorText = m_ErrorTexts[fieldName];
                if (errorText != null && errorText.gameObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(errorText.gameObject);
                }
                m_ErrorTexts.Remove(fieldName);
            }

            // Удаляем ссылки на InputField
            m_InputFields.Remove(fieldName);
            m_TMPInputFields.Remove(fieldName);

            Debug.Log($"<color=yellow>🗑️ Очищены ресурсы для поля '{fieldName}'</color>");
        }

        /// <summary>
        /// Устанавливает имя текущей сущности для валидации
        /// </summary>
        public void SetCurrentEntity(string entityName)
        {
            // Сохраняем оригинальное имя для логирования
            string originalEntityName = entityName?.ToLower() ?? "";
            string normalizedEntityName = NormalizeEntityName(originalEntityName);

            // Если сущность меняется, очищаем старые правила
            if (!string.IsNullOrEmpty(m_CurrentEntityName) && m_CurrentEntityName != originalEntityName)
            {
                Debug.Log($"<color=yellow>🧹 Смена сущности с '{m_CurrentEntityName}' на '{originalEntityName}' - очищаем старые правила</color>");
                ClearAllFields();
            }

            // Сохраняем ОРИГИНАЛЬНОЕ имя (оно будет нормализовано при поиске правил)
            m_CurrentEntityName = originalEntityName;

            // Загружаем правила валидации из ValidationFieldCatalog для текущей сущности
            LoadValidationRulesFromCatalog(normalizedEntityName);

            // Детальное логирование процесса
            Debug.Log($"<color=cyan>🏢 Установлена текущая сущность:</color>\n" +
                     $"- Оригинальное имя: '{originalEntityName}'\n" +
                     $"- Нормализованное имя: '{normalizedEntityName}'\n" +
                     $"- Сохраненное в m_CurrentEntityName: '{m_CurrentEntityName}'\n" +
                     $"- Будет использоваться для поиска правил: '{normalizedEntityName}'");

            // Проверяем есть ли правила для этой сущности в каталоге
            bool hasRulesInCatalog = ValidationFieldCatalog.GetEntityValidationRules(normalizedEntityName).Count > 0;
            Debug.Log($"<color={(hasRulesInCatalog ? "green" : "orange")}>📋 Правила в ValidationFieldCatalog: {(hasRulesInCatalog ? "найдены" : "не найдены")} для '{normalizedEntityName}'</color>");
        }

        /// <summary>
        /// Получает или создает правило валидации для поля (публичный метод)
        /// </summary>
        public ValidationRule GetOrCreateRuleForField(string fieldName)
        {
            return GetOrCreateRule(fieldName);
        }

        /// <summary>
        /// Добавляет внешний ErrorText TMP для поля (вызывается из FieldRendererValidationExtension)
        /// </summary>
        public void AddErrorTextTMP(string fieldName, TextMeshProUGUI errorTextTMP)
        {
            if (errorTextTMP != null)
            {
                m_ErrorTextTMPs[fieldName] = errorTextTMP;
                Debug.Log($"<color=cyan>🔗 Добавлен внешний ErrorText TMP для поля '{fieldName}'</color>");
            }
        }

        /// <summary>
        /// Очищает все ресурсы валидации
        /// </summary>
        public void ClearAllFields()
        {
            Debug.Log($"<color=yellow>🗑️ Очистка всех ресурсов валидации ({m_ValidationRules.Count} полей)</color>");

            // Уничтожаем все тексты ошибок
            foreach (var kvp in m_ErrorTexts)
            {
                if (kvp.Value != null && kvp.Value.gameObject != null)
                {
                    UnityEngine.Object.DestroyImmediate(kvp.Value.gameObject);
                }
            }

            // Очищаем все словари
            m_ValidationRules.Clear();
            m_ErrorTexts.Clear();
            m_ErrorTextTMPs.Clear();
            m_InputFields.Clear();
            m_TMPInputFields.Clear();

            Debug.Log($"<color=green>✅ Все ресурсы валидации очищены</color>");
        }

        /// <summary>
        /// Приватный метод для получения текущей сущности (используется FieldRendererValidationExtension)
        /// </summary>
        public string GetPrivateCurrentEntity()
        {
            return m_CurrentEntityName;
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Загружает правила валидации из ValidationFieldCatalog для текущей сущности
        /// </summary>
        private void LoadValidationRulesFromCatalog(string normalizedEntityName)
        {
            Debug.Log($"<color=cyan>📥 Загрузка правил валидации из ValidationFieldCatalog для '{normalizedEntityName}'</color>");

            try
            {
                var entityRules = ValidationFieldCatalog.GetEntityValidationRules(normalizedEntityName);

                Debug.Log($"<color=blue>📋 Найдено {entityRules.Count} правил для сущности '{normalizedEntityName}'</color>");

                // Очищаем существующие правила перед загрузкой новых
                m_ValidationRules.Clear();

                foreach (var kvp in entityRules)
                {
                    string fieldName = kvp.Key;
                    ValidationRule rule = kvp.Value;

                    m_ValidationRules[fieldName] = rule;

                    Debug.Log($"<color=green>✅ Загружено правило для поля '{fieldName}': Required={rule.isRequired}, Type={rule.fieldType}</color>");
                }

                Debug.Log($"<color=green>🎉 Успешно загружено {m_ValidationRules.Count} правил валидации для '{normalizedEntityName}'</color>");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"<color=red>❌ Ошибка при загрузке правил валидации для '{normalizedEntityName}': {ex.Message}</color>");
            }
        }

        private ValidationRule GetOrCreateRule(string fieldName)
        {
            if (!m_ValidationRules.TryGetValue(fieldName, out ValidationRule rule))
            {
                // Если указано имя сущности, пытаемся получить правила из ValidationFieldCatalog
                if (!string.IsNullOrEmpty(m_CurrentEntityName))
                {
                    string normalizedEntityName = NormalizeEntityName(m_CurrentEntityName);

                    Debug.Log($"<color=magenta>🔍 Поиск правила для '{normalizedEntityName}.{fieldName}'</color>");

                    rule = ValidationFieldCatalog.GetFieldValidationRule(normalizedEntityName, fieldName);
                    if (rule != null)
                    {
                        Debug.Log($"<color=cyan>📋 Использовано правило из ValidationFieldCatalog для '{normalizedEntityName}.{fieldName}': Required={rule.isRequired}</color>");
                    }
                    else
                    {
                        Debug.Log($"<color=orange>⚠️ Правило не найдено в ValidationFieldCatalog для '{normalizedEntityName}.{fieldName}'</color>");
                    }
                }
                else
                {
                    Debug.Log($"<color=red>❌ m_CurrentEntityName пустой для поля '{fieldName}'</color>");
                }

                // Если правило не найдено в каталоге, используем базовый резолвер
                if (rule == null)
                {
                    rule = FieldTypeResolver.CreateValidationRule(fieldName);
                    Debug.Log($"<color=yellow>⚠️ Использовано базовое правило для '{fieldName}': Required={rule.isRequired}</color>");
                }

                m_ValidationRules[fieldName] = rule;
            }
            return rule;
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

        private void SetupInputFieldType(InputField inputField, FieldType fieldType)
        {
            var contentType = GetContentType(fieldType);
            inputField.contentType = contentType;

            // Устанавливаем lineType в однострочный для большинства полей
            if (fieldType != FieldType.String ||
                (fieldType == FieldType.String && !IsMultilineField(inputField.name)))
            {
                inputField.lineType = InputField.LineType.SingleLine;
            }
        }

        private void SetupTMPInputFieldType(TMP_InputField inputField, FieldType fieldType)
        {
            // Для TMP_InputField устанавливаем соответствующий тип контента
            switch (fieldType)
            {
                case FieldType.Integer:
                    inputField.contentType = TMP_InputField.ContentType.IntegerNumber;
                    break;
                case FieldType.Float:
                case FieldType.Decimal:
                    inputField.contentType = TMP_InputField.ContentType.DecimalNumber;
                    break;
                case FieldType.Email:
                    inputField.contentType = TMP_InputField.ContentType.EmailAddress;
                    break;
                case FieldType.Phone:
                case FieldType.Inn:
                case FieldType.Kpp:
                case FieldType.Ogrn:
                    inputField.contentType = TMP_InputField.ContentType.Alphanumeric;
                    break;
                default:
                    inputField.contentType = TMP_InputField.ContentType.Standard;
                    break;
            }

            // Устанавливаем lineType
            if (fieldType != FieldType.String || !IsMultilineField(inputField.name))
            {
                inputField.lineType = TMP_InputField.LineType.SingleLine;
            }
        }

        private void ValidateInputField(string fieldName, InputField inputField, Text errorText)
        {
            var rule = GetOrCreateRule(fieldName);
            bool isValid = ValidateValue(inputField.text, rule);

            // Изменяем цвет фона
            var colors = inputField.colors;
            colors.normalColor = isValid ? m_ValidColor : m_InvalidColor;
            inputField.colors = colors;

            // Показываем/скрываем ошибку
            if (errorText != null)
            {
                errorText.gameObject.SetActive(!isValid);
                if (!isValid)
                {
                    errorText.text = GetErrorMessage(rule, inputField.text);
                    errorText.color = m_ErrorTextColor;
                }
            }
        }

        private void ValidateTMPInputField(string fieldName, TMP_InputField inputField, TextMeshProUGUI errorText)
        {
            var rule = GetOrCreateRule(fieldName);
            bool isValid = ValidateValue(inputField.text, rule);

            // Изменяем цвет фона
            var colors = inputField.colors;
            colors.normalColor = isValid ? m_ValidColor : m_InvalidColor;
            inputField.colors = colors;

            // Показываем/скрываем ошибку
            if (errorText != null)
            {
                errorText.gameObject.SetActive(!isValid);
                if (!isValid)
                {
                    errorText.text = GetErrorMessage(rule, inputField.text);
                    errorText.color = m_ErrorTextColor;
                }
            }
        }

        private bool ValidateValue(string value, ValidationRule rule)
        {
            // Проверка обязательного поля
            if (rule.isRequired && string.IsNullOrEmpty(value))
                return false;

            // Если поле не обязательное и пустое - считаем валидным
            if (!rule.isRequired && string.IsNullOrEmpty(value))
                return true;

            // Проверка длины строки
            if (rule.fieldType == FieldType.String || rule.fieldType == FieldType.Email ||
                rule.fieldType == FieldType.Phone || rule.fieldType == FieldType.Url)
            {
                if (value.Length < rule.minLength || value.Length > rule.maxLength)
                    return false;
            }

            // Проверка по паттерну
            if (!string.IsNullOrEmpty(rule.customPattern))
            {
                if (!Regex.IsMatch(value, rule.customPattern))
                    return false;
            }

            // Проверка числовых значений
            if (rule.fieldType == FieldType.Integer || rule.fieldType == FieldType.Float || rule.fieldType == FieldType.Decimal)
            {
                if (!float.TryParse(value.Replace('.', ','), out float number))
                    return false;

                if (number < rule.minValue || number > rule.maxValue)
                    return false;

                // Дополнительная проверка для целых чисел
                if (rule.fieldType == FieldType.Integer)
                {
                    if (!int.TryParse(value, out int intValue))
                        return false;
                }
            }

            // Специфические проверки
            switch (rule.fieldType)
            {
                case FieldType.Email:
                    return Regex.IsMatch(value, @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$");

                case FieldType.Phone:
                    return Regex.IsMatch(value, @"^[+0-9\s\-\(\)]*$");

                case FieldType.Inn:
                    return Regex.IsMatch(value, @"^\d{10}(\d{2})?$");

                case FieldType.Kpp:
                    return Regex.IsMatch(value, @"^\d{9}$");

                case FieldType.Ogrn:
                    return Regex.IsMatch(value, @"^\d{13}(\d{2})?$");

                case FieldType.Date:
                    return DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

                case FieldType.DateTime:
                    return DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out _);

                case FieldType.Time:
                    return TimeSpan.TryParse(value, out _);

                case FieldType.Url:
                    return Uri.TryCreate(value, UriKind.Absolute, out Uri uriResult) &&
                           (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

                case FieldType.Array:
                    return !string.IsNullOrEmpty(value); // Базовая проверка для массивов
            }

            return true;
        }

        private string GetErrorMessage(ValidationRule rule, string value)
        {
            // СНАЧАЛА проверяем есть ли сообщение в ValidationFieldCatalog для текущей сущности
            if (!string.IsNullOrEmpty(m_CurrentEntityName))
            {
                string normalizedEntityName = NormalizeEntityName(m_CurrentEntityName);

                // Получаем имя поля из правила (нужно передать в GetErrorMessage)
                // Для этого найдем ключ по правилу в словаре m_ValidationRules
                string fieldName = null;
                foreach (var kvp in m_ValidationRules)
                {
                    if (kvp.Value == rule)
                    {
                        fieldName = kvp.Key;
                        break;
                    }
                }

                if (!string.IsNullOrEmpty(fieldName))
                {
                    string catalogMessage = ValidationFieldCatalog.GetFieldErrorMessage(normalizedEntityName, fieldName, value);
                    if (!string.IsNullOrEmpty(catalogMessage))
                    {
                        Debug.Log($"<color=green>✅ Использовано сообщение из ValidationFieldCatalog для '{normalizedEntityName}.{fieldName}': '{catalogMessage}'</color>");
                        return catalogMessage;
                    }
                }
            }

            // Если сообщение не найдено в каталоге, используем логику правила
            if (!string.IsNullOrEmpty(rule.errorMessage))
                return rule.errorMessage;

            if (rule.isRequired && string.IsNullOrEmpty(value))
                return "Это поле обязательно для заполнения";

            if (rule.fieldType == FieldType.String || rule.fieldType == FieldType.Email ||
                rule.fieldType == FieldType.Phone || rule.fieldType == FieldType.Url)
            {
                if (value.Length < rule.minLength)
                    return $"Минимальная длина: {rule.minLength} символов";
                if (value.Length > rule.maxLength)
                    return $"Максимальная длина: {rule.maxLength} символов";
            }

            if (rule.fieldType == FieldType.Integer || rule.fieldType == FieldType.Float || rule.fieldType == FieldType.Decimal)
            {
                if (!float.TryParse(value.Replace('.', ','), out float number))
                    return "Введите корректное число";

                if (number < rule.minValue)
                    return $"Минимальное значение: {rule.minValue}";
                if (number > rule.maxValue)
                    return $"Максимальное значение: {rule.maxValue}";
            }

            switch (rule.fieldType)
            {
                case FieldType.Email:
                    return "Введите корректный email адрес";
                case FieldType.Phone:
                    return "Введите корректный номер телефона";
                case FieldType.Inn:
                    return "ИНН должен содержать 10 или 12 цифр";
                case FieldType.Kpp:
                    return "КПП должен содержать 9 цифр";
                case FieldType.Ogrn:
                    return "ОГРН должен содержать 13 цифр";
                case FieldType.Url:
                    return "Введите корректный URL адрес";
                case FieldType.Date:
                    return "Введите корректную дату в формате ГГГГ-ММ-ДД";
                case FieldType.DateTime:
                    return "Введите корректную дату и время";
                case FieldType.Time:
                    return "Введите корректное время";
                case FieldType.Array:
                    return "Выберите хотя бы одно значение";
            }

            return "Некорректное значение";
        }

        private bool IsMultilineField(string fieldName)
        {
            return fieldName.ToLower().Contains("comment") ||
                   fieldName.ToLower().Contains("description") ||
                   fieldName.ToLower().Contains("address");
        }

        /// <summary>
        /// Получает приватный список созданных полей из FieldRendererValidationExtension
        /// </summary>
        private List<string> GetPrivateCreatedFields(FieldRendererValidationExtension extension)
        {
            // Используем рефлексию для доступа к приватному полю m_CreatedFieldNames
            var type = extension.GetType();
            var field = type.GetField("m_CreatedFieldNames",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (field != null && field.GetValue(extension) is List<string> createdFields)
            {
                return createdFields;
            }

            return null;
        }

        /// <summary>
        /// Ожидает регистрации полей в FieldRenderer (исправление race condition)
        /// </summary>
        private bool WaitForFieldsRegistration()
        {
            int maxAttempts = 10; // Максимальное количество попыток
            int delayMs = 100;    // Задержка между попытками в миллисекундах

            Debug.Log("<color=blue>⏳ Ожидаем регистрации полей в FieldRenderer...</color>");

            for (int attempt = 0; attempt < maxAttempts; attempt++)
            {
                var fieldRendererExtension = FindAnyObjectByType<FieldRendererValidationExtension>();
                if (fieldRendererExtension != null)
                {
                    var fieldRenderer = fieldRendererExtension.GetFieldRenderer();
                    if (fieldRenderer != null)
                    {
                        // Проверяем что хотя бы некоторые поля зарегистрированы
                        int totalFields = fieldRenderer.InputFields.Count +
                                         fieldRenderer.DropdownFields.Count +
                                         (fieldRenderer.DateFields?.Count ?? 0);

                        if (totalFields > 0)
                        {
                            Debug.Log($"<color=green>✅ Поля зарегистрированы в FieldRenderer: InputFields={fieldRenderer.InputFields.Count}, DropdownFields={fieldRenderer.DropdownFields.Count}, DateFields={fieldRenderer.DateFields?.Count ?? 0}</color>");
                            return true;
                        }
                    }
                }

                if (attempt < maxAttempts - 1)
                {
                    System.Threading.Thread.Sleep(delayMs);
                }
            }

            Debug.LogWarning($"<color=orange>⚠️ Не удалось дождаться регистрации полей после {maxAttempts} попыток</color>");
            return false;
        }

        /// <summary>
        /// Ищет контейнер dropdown поля по имени
        /// </summary>
        private GameObject FindDropdownContainer(string fieldName, FieldRendererValidationExtension extension)
        {
            // Ищем активный TableEditView на сцене
            var tableEditView = FindAnyObjectByType<TableEditView>();
            if (tableEditView == null) return null;

            // Ищем контейнер полей
            var fieldsContainer = tableEditView.transform.Find("Panel/ScrollRect/Content");
            if (fieldsContainer == null) return null;

            // Ищем созданные поля в том же порядке что и в FieldRendererValidationExtension
            var createdFields = GetPrivateCreatedFields(extension);
            if (createdFields == null) return null;

            int fieldIndex = createdFields.IndexOf(fieldName);
            if (fieldIndex == -1) return null;

            // Ищем ValueItemPanel(Clone) по индексу
            int cloneIndex = 0;
            for (int i = 0; i < fieldsContainer.childCount; i++)
            {
                var child = fieldsContainer.GetChild(i);
                if (child.name.Contains("ValueItemPanel(Clone)"))
                {
                    if (cloneIndex == fieldIndex)
                    {
                        // Ищем ValueDropdown в этом элементе
                        var valueDropdown = child.Find("ValueDropdown");
                        if (valueDropdown != null)
                        {
                            return valueDropdown.gameObject;
                        }
                    }
                    cloneIndex++;
                }
            }

            return null;
        }

        #endregion
    }
}