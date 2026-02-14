using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace Validation.Models
{
    /// <summary>
    /// Базовый резолвер типов полей для общих случаев
    /// Полная информация о полях сущностей теперь в ValidationFieldCatalog.cs
    /// </summary>
    public static class FieldTypeResolver
    {
        /// <summary>
        /// Определяет тип поля на основе его названия для базовых случаев
        /// </summary>
        public static FieldType GetFieldType(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
                return FieldType.String;

            string lowerFieldName = fieldName.ToLower();

            // Финансовые поля
            if (ContainsAny(lowerFieldName, new[] { "price", "cost", "salary", "amount", "total" }))
                return FieldType.Float;

            // Количество
            if (ContainsAny(lowerFieldName, new[] { "count", "quantity", "number" }))
                return FieldType.Integer;

            // Персональные данные
            if (ContainsAny(lowerFieldName, new[] { "firstname", "lastname", "middlename", "fullname", "shortname" }))
                return FieldType.String;
            if (ContainsAny(lowerFieldName, new[] { "email", "mail" }))
                return FieldType.Email;
            if (ContainsAny(lowerFieldName, new[] { "phone", "tel", "mobile", "fax" }))
                return FieldType.Phone;

            // Документы и реквизиты
            if (ContainsAny(lowerFieldName, new[] { "inn", "tin", "taxid" }))
                return FieldType.Inn;
            if (ContainsAny(lowerFieldName, new[] { "kpp" }))
                return FieldType.Kpp;
            if (ContainsAny(lowerFieldName, new[] { "ogrn", "grn" }))
                return FieldType.Ogrn;

            // Даты
            if (ContainsAny(lowerFieldName, new[] { "date", "time", "created", "updated", "birth", "expiry", "order", "delivery", "completion" }))
                return lowerFieldName.Contains("time") ? FieldType.DateTime : FieldType.Date;

            // Медиа и URL
            if (ContainsAny(lowerFieldName, new[] { "url", "website", "link", "http", "href" }))
                return FieldType.Url;
            if (ContainsAny(lowerFieldName, new[] { "image", "photo", "scan", "file", "path" }))
                return FieldType.String;

            // Статусы и флаги
            if (ContainsAny(lowerFieldName, new[] { "is", "has", "digital", "active", "deleted", "completed" }))
                return FieldType.Boolean;

            // Размеры
            if (ContainsAny(lowerFieldName, new[] { "width", "height", "thickness", "weight" }))
                return FieldType.Float;

            // ID поля
            if (ContainsAny(lowerFieldName, new[] { "id" }))
                return FieldType.Integer;

            return FieldType.String;
        }

        /// <summary>
        /// Создает правило валидации для указанного поля
        /// </summary>
        public static ValidationRule CreateValidationRule(string fieldName, bool isRequired = false)
        {
            var fieldType = GetFieldType(fieldName);
            var rule = new ValidationRule
            {
                fieldType = fieldType,
                isRequired = isRequired
            };

            // Настраиваем параметры в зависимости от типа поля
            switch (fieldType)
            {
                case FieldType.String:
                    rule.minLength = isRequired ? 2 : 0;
                    rule.maxLength = GetDefaultStringLength(fieldName);
                    break;
                case FieldType.Integer:
                    rule.minValue = 0;
                    rule.maxValue = int.MaxValue;
                    break;
                case FieldType.Float:
                case FieldType.Decimal:
                    rule.minValue = 0f;
                    rule.maxValue = float.MaxValue;
                    break;
                case FieldType.Email:
                    rule.minLength = 5;
                    rule.maxLength = 100;
                    rule.customPattern = @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$";
                    break;
                case FieldType.Phone:
                    rule.minLength = 10;
                    rule.maxLength = 20;
                    rule.customPattern = @"^[+0-9\s\-\(\)]*$";
                    break;
                case FieldType.Inn:
                    rule.customPattern = @"^\d{10}(\d{2})?$";
                    break;
                case FieldType.Kpp:
                    rule.customPattern = @"^\d{9}$";
                    break;
                case FieldType.Ogrn:
                    rule.customPattern = @"^\d{13}(\d{2})?$";
                    break;
                case FieldType.Url:
                    rule.minLength = 5;
                    rule.maxLength = 200;
                    break;
                case FieldType.Date:
                case FieldType.DateTime:
                    rule.minValue = 0f;
                    rule.maxValue = float.MaxValue;
                    break;
            }

            return rule;
        }

        /// <summary>
        /// Проверяет является ли поле обязательным для заполнения
        /// </summary>
        public static bool IsFieldRequired(string fieldName)
        {
            if (string.IsNullOrEmpty(fieldName))
                return false;

            string lowerFieldName = fieldName.ToLower();

            // Обязательные поля по умолчанию
            if (lowerFieldName.Contains("name") || lowerFieldName.Contains("title"))
                return true;

            if (lowerFieldName.Contains("inn") || lowerFieldName.Contains("kpp") || lowerFieldName.Contains("ogrn"))
                return true;

            if (lowerFieldName.Contains("email") || lowerFieldName.Contains("phone"))
                return true;

            if (lowerFieldName.Contains("vendorcode"))
                return true;

            // Поля с датами часто обязательны
            if (lowerFieldName.Contains("date") && !lowerFieldName.Contains("end"))
                return true;

            return false;
        }

        /// <summary>
        /// Проверяет содержится ли любой из паттернов в строке
        /// </summary>
        private static bool ContainsAny(string text, string[] patterns)
        {
            foreach (var pattern in patterns)
            {
                if (text.Contains(pattern))
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Получает максимальную длину строки по умолчанию для поля
        /// </summary>
        private static int GetDefaultStringLength(string fieldName)
        {
            string lowerFieldName = fieldName.ToLower();

            // Устанавливаем разумные ограничения по умолчанию
            if (lowerFieldName.Contains("name"))
                return 100;
            if (lowerFieldName.Contains("title"))
                return 200;
            if (lowerFieldName.Contains("description"))
                return 500;
            if (lowerFieldName.Contains("comment"))
                return 200;
            if (lowerFieldName.Contains("address"))
                return 255;

            return 255; // По умолчанию
        }
    }
}