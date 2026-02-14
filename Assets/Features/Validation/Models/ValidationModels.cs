using System;

namespace Validation.Models
{
    /// <summary>
    /// Типы данных полей на основе анализа backend entity
    /// </summary>
    public enum FieldType
    {
        String,         // текстовые поля
        Integer,        // целые числа
        Float,          // десятичные числа
        Decimal,        // финансовые десятичные числа
        Date,           // дата
        Time,           // время
        DateTime,       // дата и время
        Boolean,        // флаги
        Email,          // email
        Phone,          // телефон
        Inn,            // ИНН (12 цифр)
        Kpp,            // КПП (9 цифр)
        Ogrn,           // ОГРН (13 цифр)
        Array,          // массивы
        Url             // URL адреса
    }

    /// <summary>
    /// Правила валидации для поля
    /// </summary>
    [Serializable]
    public class ValidationRule
    {
        public FieldType fieldType;
        public bool isRequired;
        public int minLength;
        public int maxLength;
        public float minValue;
        public float maxValue;
        public string customPattern;
        public string errorMessage;

        public ValidationRule()
        {
            fieldType = FieldType.String;
            isRequired = false;
            minLength = 0;
            maxLength = 255;
            minValue = float.MinValue;
            maxValue = float.MaxValue;
            customPattern = "";
            errorMessage = "";
        }

        public ValidationRule(FieldType type, bool required = false, int minLen = 0, int maxLen = 255)
        {
            fieldType = type;
            isRequired = required;
            minLength = minLen;
            maxLength = maxLen;
            minValue = float.MinValue;
            maxValue = float.MaxValue;
            customPattern = "";
            errorMessage = "";
        }
    }
}