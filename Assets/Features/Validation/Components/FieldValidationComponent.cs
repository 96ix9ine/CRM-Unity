using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Validation.Models;
using Validation.Services;

namespace Validation.Components
{
    /// <summary>
    /// Компонент для автоматической валидации полей
    /// Добавляется к InputField для автоматической настройки валидации
    /// </summary>
    public class FieldValidationComponent : MonoBehaviour
    {
        [Header("Validation Settings")]
        [SerializeField] private string m_FieldName;
        [SerializeField] private bool m_IsRequired = false;
        [SerializeField] private ValidationRule m_CustomRule;
        [SerializeField] private bool m_UseCustomRule = false;
        [SerializeField] private Text m_ErrorText;
        [SerializeField] private TextMeshProUGUI m_ErrorTextTMP;

        private ValidationService m_ValidationService;
        private InputField m_InputField;
        private TMP_InputField m_TMPInputField;

        #region Unity Lifecycle

        private void Awake()
        {
            m_InputField = GetComponent<InputField>();
            m_TMPInputField = GetComponent<TMP_InputField>();
            m_ValidationService = FindAnyObjectByType<ValidationService>();

            if (m_ValidationService == null)
            {
                Debug.LogWarning("ValidationService не найден на сцене!");
                return;
            }

            SetupValidation();
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Устанавливает имя поля для валидации
        /// </summary>
        public void SetFieldName(string fieldName)
        {
            m_FieldName = fieldName;
            SetupValidation();
        }

        /// <summary>
        /// Устанавливает обязательность поля
        /// </summary>
        public void SetRequired(bool required)
        {
            m_IsRequired = required;
            SetupValidation();
        }

        /// <summary>
        /// Устанавливает текстовое поле для отображения ошибок
        /// </summary>
        public void SetErrorText(Text errorText)
        {
            m_ErrorText = errorText;
            SetupValidation();
        }

        /// <summary>
        /// Устанавливает текстовое поле для отображения ошибок (TMP)
        /// </summary>
        public void SetErrorText(TextMeshProUGUI errorText)
        {
            m_ErrorTextTMP = errorText;
            SetupValidation();
        }

        /// <summary>
        /// Валидирует текущее значение поля
        /// </summary>
        public bool ValidateCurrent()
        {
            if (m_ValidationService == null) return true;

            string value = "";
            if (m_InputField != null) value = m_InputField.text;
            else if (m_TMPInputField != null) value = m_TMPInputField.text;

            return m_ValidationService.ValidateField(m_FieldName, value);
        }

        #endregion

        #region Private Methods

        private void SetupValidation()
        {
            if (m_ValidationService == null || string.IsNullOrEmpty(m_FieldName))
                return;

            // Добавляем правило валидации
            if (m_UseCustomRule && m_CustomRule != null)
            {
                m_ValidationService.AddValidationRule(m_FieldName, m_CustomRule);
            }
            else
            {
                m_ValidationService.AddValidationRule(m_FieldName, m_IsRequired);
            }

            // Настраиваем валидацию для соответствующего типа поля
            if (m_InputField != null)
            {
                m_ValidationService.SetupInputFieldValidation(m_FieldName, m_InputField, m_ErrorText);
            }
            else if (m_TMPInputField != null)
            {
                m_ValidationService.SetupTMPInputFieldValidation(m_FieldName, m_TMPInputField, m_ErrorTextTMP);
            }
        }

        #endregion
    }
}