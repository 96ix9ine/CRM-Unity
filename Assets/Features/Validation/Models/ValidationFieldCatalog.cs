using UnityEngine;
using Validation.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Validation.Models
{
    /// <summary>
    /// ЕДИНЫЙ каталог полей валидации для всех сущностей CRM системы
    /// </summary>
    public static class ValidationFieldCatalog
    {
        /// <summary>
        /// Получает правило валидации для поля конкретной сущности
        /// </summary>
        public static ValidationRule GetFieldValidationRule(string entityName, string fieldName)
        {
            var fieldInfo = GetEntityFieldInfo(entityName, fieldName);
            if (fieldInfo == null)
                return GetDefaultRule(fieldName);

            return new ValidationRule
            {
                fieldType = fieldInfo.FieldType,
                isRequired = fieldInfo.IsRequired,
                minLength = fieldInfo.MinLength,
                maxLength = fieldInfo.MaxLength,
                minValue = fieldInfo.MinValue,
                maxValue = fieldInfo.MaxValue,
                errorMessage = fieldInfo.ErrorMessage,
                customPattern = fieldInfo.CustomPattern
            };
        }

        /// <summary>
        /// Получает все правила валидации для сущности
        /// </summary>
        public static Dictionary<string, ValidationRule> GetEntityValidationRules(string entityName)
        {
            var rules = new Dictionary<string, ValidationRule>();
            var fields = GetAllEntityFields(entityName);

            foreach (var field in fields)
            {
                rules[field.FieldName] = GetFieldValidationRule(entityName, field.FieldName);
            }

            return rules;
        }

        /// <summary>
        /// Проверяет обязательность поля для сущности
        /// </summary>
        public static bool IsFieldRequired(string entityName, string fieldName)
        {
            var fieldInfo = GetEntityFieldInfo(entityName, fieldName);
            return fieldInfo?.IsRequired ?? false;
        }

        /// <summary>
        /// Получает сообщение об ошибке для поля
        /// </summary>
        public static string GetFieldErrorMessage(string entityName, string fieldName, string currentValue = null)
        {
            var fieldInfo = GetEntityFieldInfo(entityName, fieldName);
            if (fieldInfo == null || string.IsNullOrEmpty(fieldInfo.ErrorMessage))
                return GetDefaultErrorMessage(fieldName, currentValue);

            if (fieldInfo.IsRequired && string.IsNullOrEmpty(currentValue))
                return fieldInfo.ErrorMessage;

            // Проверяем специфические ошибки
            if (fieldInfo.FieldType == FieldType.String || fieldInfo.FieldType == FieldType.Email || fieldInfo.FieldType == FieldType.Phone)
            {
                if (currentValue?.Length < fieldInfo.MinLength)
                    return $"Минимальная длина: {fieldInfo.MinLength} символов";
                if (currentValue?.Length > fieldInfo.MaxLength)
                    return $"Максимальная длина: {fieldInfo.MaxLength} символов";
            }

            return fieldInfo.ErrorMessage;
        }

        /// <summary>
        /// Получает информацию о поле сущности
        /// </summary>
        private static EntityFieldInfo GetEntityFieldInfo(string entityName, string fieldName)
        {
            var allFields = GetAllEntityFields(entityName);
            return allFields.FirstOrDefault(f => f.FieldName.Equals(fieldName, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Получает все поля сущности
        /// </summary>
        private static List<EntityFieldInfo> GetAllEntityFields(string entityName)
        {
            switch (entityName.ToLower())
            {
                // Основные сущности (исправленные)
                case "organizations": return OrganizationsFields;
                case "peoples": return PeoplesFields;
                case "employees": return EmployeesFields;
                case "components": return ComponentsFields;
                case "stands": return StandsFields;
                case "shipments": return ShipmentsFields;
                case "pcb_orders": return PcbOrdersFields;
                case "departments": return DepartmentsFields;
                case "professions": return ProfessionsFields;

                // Полностью замененные сущности
                case "images": return ImagesFields;
                case "license": return LicenseFields;
                case "order_requests": return OrderRequestsFields;

                // Новые сущности из бэкенда
                case "current_task_states": return CurrentTaskStatesFields;
                case "stand_tasks": return StandTasksFields;
                case "organization_types": return OrganizationTypesFields;
                case "stand_types": return StandTypesFields;
                case "users": return UsersFields;
                case "pcb_order_states": return PcbOrderStatesFields;
                case "license_types": return LicenseTypesFields;
                case "component_placements": return ComponentPlacementsFields;
                case "writeoff": return WriteoffFields;
                case "writeoff_reasons": return WriteoffReasonsFields;
                case "bills_for_pay": return BillsForPayFields;
                case "bills_components": return BillsComponentsFields;
                case "component_placement_type": return ComponentPlacementTypeFields;
                case "server_arrivals": return ServerArrivalsFields;
                case "server_writeoff": return ServerWriteoffFields;
                case "shipment_package": return ShipmentPackageFields;
                case "shipment_package_states": return ShipmentPackageStatesFields;
                case "shipment_trips": return ShipmentTripsFields;
                case "shipment_stands": return ShipmentStandsFields;
                case "stand_packages": return StandPackagesFields;
                case "employees_departments": return EmployeesDepartmentsFields;
                case "employees_professions": return EmployeesProfessionsFields;
                case "employees_vacations": return EmployeesVacationsFields;
                case "invoices_components": return InvoicesComponentsFields;
                case "current_tasks_components": return CurrentTasksComponentsFields;
                case "order_requests_components": return OrderRequestsComponentsFields;
                case "pcbs_components": return PcbsComponentsFields;
                case "stand_tasks_components": return StandTasksComponentsFields;
                case "supplier_components": return SupplierComponentsFields;
                case "current_task_states_log": return CurrentTaskStatesLogFields;
                case "inventarization": return InventarizationFields;

                // Существующие сущности без изменений
                case "arrival_invoices": return ArrivalInvoicesFields;
                case "current_tasks": return CurrentTasksFields;

                default: return new List<EntityFieldInfo>();
            }
        }

        /// <summary>
        /// Создает правило по умолчанию для неизвестного поля
        /// </summary>
        private static ValidationRule GetDefaultRule(string fieldName)
        {
            var fieldType = FieldTypeResolver.GetFieldType(fieldName);
            return new ValidationRule
            {
                fieldType = fieldType,
                isRequired = false,
                minLength = 0,
                maxLength = 255,
                minValue = float.MinValue,
                maxValue = float.MaxValue,
                errorMessage = "Поле заполнено некорректно"
            };
        }

        /// <summary>
        /// Получает сообщение об ошибке по умолчанию
        /// </summary>
        private static string GetDefaultErrorMessage(string fieldName, string currentValue)
        {
            if (string.IsNullOrEmpty(currentValue))
                return "Это поле обязательно для заполнения";

            var fieldType = FieldTypeResolver.GetFieldType(fieldName);
            switch (fieldType)
            {
                case FieldType.Email:
                    return "Введите корректный email адрес";
                case FieldType.Phone:
                    return "Введите корректный номер телефона";
                case FieldType.Inn:
                    return "ИНН должен содержать 12 цифр";
                case FieldType.Kpp:
                    return "КПП должен содержать 9 цифр";
                case FieldType.Ogrn:
                    return "ОГРН должен содержать 13 цифр";
                case FieldType.Url:
                    return "Введите корректный URL адрес";
                case FieldType.Date:
                    return "Введите корректную дату";
                default:
                    return "Некорректное значение";
            }
        }

        #region Organizations Fields (ИСПРАВЛЕНО)

        private static readonly List<EntityFieldInfo> OrganizationsFields = new List<EntityFieldInfo>
        {
            new EntityFieldInfo { FieldName = "parentId", FieldType = FieldType.Integer, IsRequired = false },
            new EntityFieldInfo { FieldName = "fullName", FieldType = FieldType.String, IsRequired = true, MinLength = 3, MaxLength = 80, ErrorMessage = "Полное название должно содержать от 3 до 80 символов" },
            new EntityFieldInfo { FieldName = "shortName", FieldType = FieldType.String, IsRequired = true, MinLength = 2, MaxLength = 80, ErrorMessage = "Краткое название должно содержать от 2 до 80 символов" },
            // ИСПРАВЛЕНО: сделаны обязательными поля адресов
            new EntityFieldInfo { FieldName = "lawAddress", FieldType = FieldType.String, IsRequired = true, MaxLength = 100, ErrorMessage = "Юридический адрес обязателен и не должен превышать 100 символов" },
            new EntityFieldInfo { FieldName = "factAddress", FieldType = FieldType.String, IsRequired = true, MaxLength = 100, ErrorMessage = "Фактический адрес обязателен и не должен превышать 100 символов" },
            new EntityFieldInfo { FieldName = "postAddress", FieldType = FieldType.String, IsRequired = true, MaxLength = 100, ErrorMessage = "Почтовый адрес обязателен и не должен превышать 100 символов" },
            // ИСПРАВЛЕНО: уточнены длины полей
            new EntityFieldInfo { FieldName = "inn", FieldType = FieldType.Inn, IsRequired = true, MaxLength = 12, ErrorMessage = "ИНН должен содержать 12 цифр" },
            new EntityFieldInfo { FieldName = "kpp", FieldType = FieldType.Kpp, IsRequired = false, ErrorMessage = "КПП должен содержать 9 цифр" },
            new EntityFieldInfo { FieldName = "orgn", FieldType = FieldType.Ogrn, IsRequired = false, MaxLength = 13, ErrorMessage = "ОГРН должен содержать 13 цифр" },
            // ИСПРАВЛЕНО: сделано обязательным
            new EntityFieldInfo { FieldName = "orgnDate", FieldType = FieldType.Date, IsRequired = true, ErrorMessage = "Дата ОГРН обязательна" },
            new EntityFieldInfo { FieldName = "phone", FieldType = FieldType.Phone, IsRequired = false, MaxLength = 12 },
            new EntityFieldInfo { FieldName = "email", FieldType = FieldType.Email, IsRequired = false, MaxLength = 80 },
            // ИСПРАВЛЕНО: сделаны обязательными
            new EntityFieldInfo { FieldName = "digitalDocs", FieldType = FieldType.Boolean, IsRequired = true, ErrorMessage = "Поле цифровых документов обязательно" },
            new EntityFieldInfo { FieldName = "rating", FieldType = FieldType.Float, IsRequired = true, MinValue = 0f, MaxValue = 5f, ErrorMessage = "Рейтинг обязателен и должен быть от 0 до 5" },
            new EntityFieldInfo { FieldName = "comment", FieldType = FieldType.String, IsRequired = false, MaxLength = 45, ErrorMessage = "Комментарий не должен превышать 45 символов" },
            new EntityFieldInfo { FieldName = "contactPeopleId", FieldType = FieldType.Integer, IsRequired = false },
            new EntityFieldInfo { FieldName = "organizationTypeId", FieldType = FieldType.Integer, IsRequired = false }
        };

        #endregion

        #region Peoples Fields (ИСПРАВЛЕНО)

        private static readonly List<EntityFieldInfo> PeoplesFields = new List<EntityFieldInfo>
        {
            new EntityFieldInfo { FieldName = "firstName", FieldType = FieldType.String, IsRequired = true, MinLength = 2, MaxLength = 50, ErrorMessage = "Имя должно содержать от 2 до 50 символов" },
            new EntityFieldInfo { FieldName = "middleName", FieldType = FieldType.String, IsRequired = true, MinLength = 2, MaxLength = 50, ErrorMessage = "Отчество должно содержать от 2 до 50 символов" },
            new EntityFieldInfo { FieldName = "lastName", FieldType = FieldType.String, IsRequired = true, MinLength = 2, MaxLength = 50, ErrorMessage = "Фамилия должна содержать от 2 до 50 символов" },
            new EntityFieldInfo { FieldName = "phone", FieldType = FieldType.Phone, IsRequired = false, MaxLength = 12 },
            // ИСПРАВЛЕНО: сделано обязательным
            new EntityFieldInfo { FieldName = "email", FieldType = FieldType.Email, IsRequired = true, MaxLength = 80, ErrorMessage = "Email обязателен" },
            new EntityFieldInfo { FieldName = "birthDate", FieldType = FieldType.Date, IsRequired = false },
            // ИСПРАВЛЕНО: убрано ограничение длины
            new EntityFieldInfo { FieldName = "comment", FieldType = FieldType.String, IsRequired = false, ErrorMessage = "Комментарий без ограничений длины" }
        };

        #endregion

        #region Employees Fields (КАРДИНАЛЬНО ИЗМЕНЕНО)

        private static readonly List<EntityFieldInfo> EmployeesFields = new List<EntityFieldInfo>
        {
            // УДАЛЕНЫ: firstName, middleName, lastName, salary, departmentId, professionId
            // ДОБАВЛЕНО: peopleId (required)
            new EntityFieldInfo { FieldName = "peopleId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "People ID обязателен" },
            // ДОБАВЛЕНО: hiringDate (nullable)
            new EntityFieldInfo { FieldName = "hiringDate", FieldType = FieldType.Date, IsRequired = false, ErrorMessage = "Дата приема на работу" },
            // ДОБАВЛЕНО: dismissalDate (nullable)
            new EntityFieldInfo { FieldName = "dismissalDate", FieldType = FieldType.Date, IsRequired = false, ErrorMessage = "Дата увольнения" }
        };

        #endregion

        #region Components Fields (ИСПРАВЛЕНО)

        private static readonly List<EntityFieldInfo> ComponentsFields = new List<EntityFieldInfo>
        {
            new EntityFieldInfo { FieldName = "parentId", FieldType = FieldType.Integer, IsRequired = false },
            new EntityFieldInfo { FieldName = "title", FieldType = FieldType.String, IsRequired = true, MinLength = 3, MaxLength = 120, ErrorMessage = "Название должно содержать от 3 до 120 символов" },
            new EntityFieldInfo { FieldName = "photo", FieldType = FieldType.String, IsRequired = false },
            new EntityFieldInfo { FieldName = "width", FieldType = FieldType.Float, IsRequired = false, MinValue = 0f, ErrorMessage = "Ширина должна быть числом больше или равна 0" },
            new EntityFieldInfo { FieldName = "height", FieldType = FieldType.Float, IsRequired = false, MinValue = 0f, ErrorMessage = "Высота должна быть числом больше или равна 0" },
            new EntityFieldInfo { FieldName = "thickness", FieldType = FieldType.Float, IsRequired = false, MinValue = 0f, ErrorMessage = "Толщина должна быть числом больше или равна 0" },
            new EntityFieldInfo { FieldName = "weight", FieldType = FieldType.Float, IsRequired = false, MinValue = 0f, ErrorMessage = "Вес должен быть числом больше или равен 0" },
            new EntityFieldInfo { FieldName = "material", FieldType = FieldType.String, IsRequired = false, MaxLength = 45, ErrorMessage = "Материал не должен превышать 45 символов" },
            new EntityFieldInfo { FieldName = "receiptDate", FieldType = FieldType.Date, IsRequired = false },
            // ИСПРАВЛЕНО: убрано ограничение длины
            new EntityFieldInfo { FieldName = "drawingReference", FieldType = FieldType.String, IsRequired = false, ErrorMessage = "Чертеж без ограничений длины" },
            // ИСПРАВЛЕНО: убрана обязательность (defaultValue = 0 в бэкенде)
            new EntityFieldInfo { FieldName = "quantity", FieldType = FieldType.Integer, IsRequired = false, MinValue = 0, ErrorMessage = "Количество должно быть целым числом больше или равно 0" },
            new EntityFieldInfo { FieldName = "placementId", FieldType = FieldType.Integer, IsRequired = false }
        };

        #endregion

        #region Stands Fields (ИСПРАВЛЕНО)

        private static readonly List<EntityFieldInfo> StandsFields = new List<EntityFieldInfo>
        {
            new EntityFieldInfo { FieldName = "parentId", FieldType = FieldType.Integer, IsRequired = false },
            new EntityFieldInfo { FieldName = "title", FieldType = FieldType.String, IsRequired = true, MinLength = 2, MaxLength = 100, ErrorMessage = "Название должно содержать от 2 до 100 символов" },
            new EntityFieldInfo { FieldName = "image", FieldType = FieldType.String, IsRequired = false },
            // ИСПРАВЛЕНО: изменены типы с float на string (как в бэкенде)
            new EntityFieldInfo { FieldName = "width", FieldType = FieldType.String, IsRequired = false, MaxLength = 100, ErrorMessage = "Ширина не должна превышать 100 символов" },
            new EntityFieldInfo { FieldName = "height", FieldType = FieldType.String, IsRequired = false, MaxLength = 100, ErrorMessage = "Высота не должна превышать 100 символов" },
            new EntityFieldInfo { FieldName = "thickness", FieldType = FieldType.String, IsRequired = false, MaxLength = 100, ErrorMessage = "Толщина не должна превышать 100 символов" },
            new EntityFieldInfo { FieldName = "weightNetto", FieldType = FieldType.Float, IsRequired = false, MinValue = 0f, ErrorMessage = "Вес нетто должен быть числом больше или равен 0" },
            new EntityFieldInfo { FieldName = "weightBrutto", FieldType = FieldType.Float, IsRequired = false, MinValue = 0f, ErrorMessage = "Вес брутто должен быть числом больше или равен 0" },
            new EntityFieldInfo { FieldName = "link", FieldType = FieldType.Url, IsRequired = false, MaxLength = 100, ErrorMessage = "Ссылка не должна превышать 100 символов" },
            new EntityFieldInfo { FieldName = "vendorCode", FieldType = FieldType.String, IsRequired = false },
            new EntityFieldInfo { FieldName = "manufactureTime", FieldType = FieldType.Integer, IsRequired = false, MinValue = 1, MaxValue = 9999, ErrorMessage = "Время производства должно быть от 1 до 9999" },
            new EntityFieldInfo { FieldName = "comment", FieldType = FieldType.String, IsRequired = false, MaxLength = 45, ErrorMessage = "Комментарий не должен превышать 45 символов" },
            new EntityFieldInfo { FieldName = "standTypeId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Тип стенда обязателен для выбора" },
            new EntityFieldInfo { FieldName = "employeeId", FieldType = FieldType.Integer, IsRequired = false }
        };

        #endregion

        #region Shipments Fields (ИСПРАВЛЕНО)

        private static readonly List<EntityFieldInfo> ShipmentsFields = new List<EntityFieldInfo>
        {
            new EntityFieldInfo { FieldName = "price", FieldType = FieldType.Float, IsRequired = true, MinValue = 0f, ErrorMessage = "Цена должна быть числом больше или равна 0" },
            new EntityFieldInfo { FieldName = "addedDate", FieldType = FieldType.Date, IsRequired = false },
            new EntityFieldInfo { FieldName = "shipmentDate", FieldType = FieldType.Date, IsRequired = true, ErrorMessage = "Дата отгрузки обязательна" },
            new EntityFieldInfo { FieldName = "arrivalDate", FieldType = FieldType.Date, IsRequired = true, ErrorMessage = "Дата прибытия обязательна" },
            // ИСПРАВЛЕНО: specificationImage - это массив строк в бэкенде
            new EntityFieldInfo { FieldName = "specificationImage", FieldType = FieldType.Array, IsRequired = false, ErrorMessage = "Спецификация должна быть массивом строк" },
            new EntityFieldInfo { FieldName = "comment", FieldType = FieldType.String, IsRequired = false, MaxLength = 200, ErrorMessage = "Комментарий не должен превышать 200 символов" },
            new EntityFieldInfo { FieldName = "factoryId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Фабрика обязательна для выбора" },
            new EntityFieldInfo { FieldName = "transporterId", FieldType = FieldType.Integer, IsRequired = false },
            new EntityFieldInfo { FieldName = "clientId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Клиент обязателен для выбора" },
            new EntityFieldInfo { FieldName = "licenseId", FieldType = FieldType.Integer, IsRequired = false }
        };

        #endregion

        #region PCB Orders Fields (ИСПРАВЛЕНО)

        private static readonly List<EntityFieldInfo> PcbOrdersFields = new List<EntityFieldInfo>
        {
            new EntityFieldInfo { FieldName = "orderDate", FieldType = FieldType.Date, IsRequired = true, ErrorMessage = "Дата заказа обязательна" },
            new EntityFieldInfo { FieldName = "billNumber", FieldType = FieldType.String, IsRequired = true, MinLength = 3, MaxLength = 45, ErrorMessage = "Номер счета должен содержать от 3 до 45 символов" },
            new EntityFieldInfo { FieldName = "count", FieldType = FieldType.Integer, IsRequired = true, MinValue = 1, ErrorMessage = "Количество должно быть целым числом больше 0" },
            // ИСПРАВЛЕНО: сделаны обязательными
            new EntityFieldInfo { FieldName = "width", FieldType = FieldType.Float, IsRequired = true, MinValue = 0f, ErrorMessage = "Ширина обязательна и должна быть числом больше или равна 0" },
            new EntityFieldInfo { FieldName = "height", FieldType = FieldType.Float, IsRequired = true, MinValue = 0f, ErrorMessage = "Высота обязательна и должна быть числом больше или равна 0" },
            new EntityFieldInfo { FieldName = "thickness", FieldType = FieldType.Float, IsRequired = true, MinValue = 0f, ErrorMessage = "Толщина обязательна и должна быть числом больше или равна 0" },
            new EntityFieldInfo { FieldName = "article", FieldType = FieldType.String, IsRequired = true, MaxLength = 45, ErrorMessage = "Артикул обязателен и не должен превышать 45 символов" },
            new EntityFieldInfo { FieldName = "price", FieldType = FieldType.Float, IsRequired = true, MinValue = 0f, ErrorMessage = "Цена должна быть числом больше или равна 0" },
            new EntityFieldInfo { FieldName = "pcbId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "PCB обязателен для выбора" },
            new EntityFieldInfo { FieldName = "pcbManufacturerId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Производитель PCB обязателен для выбора" },
            new EntityFieldInfo { FieldName = "factoryId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Фабрика обязательна для выбора" },
            new EntityFieldInfo { FieldName = "employeeId", FieldType = FieldType.Integer, IsRequired = false },
            new EntityFieldInfo { FieldName = "pcbOrderStatusId", FieldType = FieldType.Integer, IsRequired = false }
        };

        #endregion

        #region Departments Fields (ИСПРАВЛЕНО)

        private static readonly List<EntityFieldInfo> DepartmentsFields = new List<EntityFieldInfo>
        {
            // ИСПРАВЛЕНО: поле name изменено на title
            new EntityFieldInfo { FieldName = "title", FieldType = FieldType.String, IsRequired = true, MinLength = 2, MaxLength = 100, ErrorMessage = "Название отдела должно содержать от 2 до 100 символов" }
        };

        #endregion

        #region Professions Fields (ИСПРАВЛЕНО)

        private static readonly List<EntityFieldInfo> ProfessionsFields = new List<EntityFieldInfo>
        {
            // ИСПРАВЛЕНО: поле name изменено на title
            new EntityFieldInfo { FieldName = "title", FieldType = FieldType.String, IsRequired = true, MinLength = 2, MaxLength = 100, ErrorMessage = "Название профессии должно содержать от 2 до 100 символов" }
        };

        #endregion

        #region Images Fields (ПОЛНАЯ ЗАМЕНА)

        private static readonly List<EntityFieldInfo> ImagesFields = new List<EntityFieldInfo>
        {
              new EntityFieldInfo { FieldName = "filename", FieldType = FieldType.String, IsRequired = true, MaxLength = 255, ErrorMessage = "Имя файла обязательно" },
            new EntityFieldInfo { FieldName = "originalName", FieldType = FieldType.String, IsRequired = true, MaxLength = 255, ErrorMessage = "Оригинальное имя файла обязательно" },
            new EntityFieldInfo { FieldName = "mimetype", FieldType = FieldType.String, IsRequired = true, MaxLength = 100, ErrorMessage = "MIME тип обязателен" },
            new EntityFieldInfo { FieldName = "size", FieldType = FieldType.Integer, IsRequired = true, MinValue = 0, ErrorMessage = "Размер файла обязателен" },
            new EntityFieldInfo { FieldName = "path", FieldType = FieldType.String, IsRequired = true, MaxLength = 500, ErrorMessage = "Путь к файлу обязателен" },
            new EntityFieldInfo { FieldName = "targetType", FieldType = FieldType.String, IsRequired = true, MaxLength = 50, ErrorMessage = "Тип цели обязателен" },
            new EntityFieldInfo { FieldName = "targetId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "ID цели обязателен" },
            new EntityFieldInfo { FieldName = "description", FieldType = FieldType.String, IsRequired = false, MaxLength = 500, ErrorMessage = "Описание не должно превышать 500 символов" }
        };

        #endregion

        #region License Fields (ПОЛНАЯ ЗАМЕНА)

        private static readonly List<EntityFieldInfo> LicenseFields = new List<EntityFieldInfo>
        {
              new EntityFieldInfo { FieldName = "licenseCode", FieldType = FieldType.String, IsRequired = true, MaxLength = 45, ErrorMessage = "Код лицензии обязателен и не должен превышать 45 символов" },
            new EntityFieldInfo { FieldName = "places", FieldType = FieldType.Integer, IsRequired = true, MinValue = 1, ErrorMessage = "Количество мест обязательно" },
            new EntityFieldInfo { FieldName = "start", FieldType = FieldType.Date, IsRequired = false, ErrorMessage = "Дата начала действия лицензии" },
            new EntityFieldInfo { FieldName = "end", FieldType = FieldType.Date, IsRequired = false, ErrorMessage = "Дата окончания действия лицензии" },
            new EntityFieldInfo { FieldName = "timeout", FieldType = FieldType.Time, IsRequired = false, ErrorMessage = "Таймаут лицензии" },
            new EntityFieldInfo { FieldName = "comment", FieldType = FieldType.String, IsRequired = false, MaxLength = 45, ErrorMessage = "Комментарий не должен превышать 45 символов" },
            new EntityFieldInfo { FieldName = "licenseTypeId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Тип лицензии обязателен" },
            new EntityFieldInfo { FieldName = "standId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Стенд обязателен" }
        };

        #endregion

        #region Order Requests Fields (ПОЛНАЯ ЗАМЕНА)

        private static readonly List<EntityFieldInfo> OrderRequestsFields = new List<EntityFieldInfo>
        {
              new EntityFieldInfo { FieldName = "title", FieldType = FieldType.String, IsRequired = true, MaxLength = 45, ErrorMessage = "Название запроса обязательно и не должно превышать 45 символов" },
            new EntityFieldInfo { FieldName = "requestDatetime", FieldType = FieldType.DateTime, IsRequired = true, ErrorMessage = "Дата и время запроса обязательны" },
            new EntityFieldInfo { FieldName = "executionDatetime", FieldType = FieldType.DateTime, IsRequired = true, ErrorMessage = "Дата и время исполнения обязательны" },
            new EntityFieldInfo { FieldName = "comment", FieldType = FieldType.String, IsRequired = false, MaxLength = 45, ErrorMessage = "Комментарий не должен превышать 45 символов" },
            new EntityFieldInfo { FieldName = "factoryId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Фабрика обязательна" },
            new EntityFieldInfo { FieldName = "standId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Стенд обязателен" },
            new EntityFieldInfo { FieldName = "employeeCreatorId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Сотрудник создатель обязателен" }
        };

        #endregion

        #region Новые сущности из бэкенда

        private static readonly List<EntityFieldInfo> CurrentTaskStatesFields = new List<EntityFieldInfo>
        {
              new EntityFieldInfo { FieldName = "title", FieldType = FieldType.String, IsRequired = true, MaxLength = 100, ErrorMessage = "Название состояния обязательно" },
            new EntityFieldInfo { FieldName = "description", FieldType = FieldType.String, IsRequired = false, MaxLength = 500, ErrorMessage = "Описание не должно превышать 500 символов" }
        };

        private static readonly List<EntityFieldInfo> StandTasksFields = new List<EntityFieldInfo>
        {
              new EntityFieldInfo { FieldName = "title", FieldType = FieldType.String, IsRequired = true, MaxLength = 200, ErrorMessage = "Название задачи обязательно" },
            new EntityFieldInfo { FieldName = "description", FieldType = FieldType.String, IsRequired = false, MaxLength = 1000, ErrorMessage = "Описание не должно превышать 1000 символов" },
            new EntityFieldInfo { FieldName = "duration", FieldType = FieldType.Integer, IsRequired = false, MinValue = 1, ErrorMessage = "Длительность должна быть положительным числом" },
            new EntityFieldInfo { FieldName = "complexity", FieldType = FieldType.Integer, IsRequired = false, MinValue = 1, MaxValue = 10, ErrorMessage = "Сложность должна быть от 1 до 10" }
        };

        private static readonly List<EntityFieldInfo> OrganizationTypesFields = new List<EntityFieldInfo>
        {
              new EntityFieldInfo { FieldName = "title", FieldType = FieldType.String, IsRequired = true, MaxLength = 100, ErrorMessage = "Название типа организации обязательно" },
            new EntityFieldInfo { FieldName = "description", FieldType = FieldType.String, IsRequired = false, MaxLength = 500, ErrorMessage = "Описание не должно превышать 500 символов" }
        };

        private static readonly List<EntityFieldInfo> StandTypesFields = new List<EntityFieldInfo>
        {
            new EntityFieldInfo { FieldName = "title", FieldType = FieldType.String, IsRequired = true, MaxLength = 100, ErrorMessage = "Название типа стенда обязательно" },
            new EntityFieldInfo { FieldName = "description", FieldType = FieldType.String, IsRequired = false, MaxLength = 500, ErrorMessage = "Описание не должно превышать 500 символов" }
        };

        private static readonly List<EntityFieldInfo> UsersFields = new List<EntityFieldInfo>
        {
            new EntityFieldInfo { FieldName = "userName", FieldType = FieldType.String, IsRequired = true, MaxLength = 50, ErrorMessage = "Имя пользователя обязательно" },
            new EntityFieldInfo { FieldName = "email", FieldType = FieldType.Email, IsRequired = true, MaxLength = 100, ErrorMessage = "Email обязателен" },
            new EntityFieldInfo { FieldName = "password", FieldType = FieldType.String, IsRequired = true, MaxLength = 255, ErrorMessage = "Пароль обязателен" },
            new EntityFieldInfo { FieldName = "passwordHash", FieldType = FieldType.String, IsRequired = true, MaxLength = 255, ErrorMessage = "Хеш пароля обязателен" },
            new EntityFieldInfo { FieldName = "passwordSalt", FieldType = FieldType.String, IsRequired = false, MaxLength = 255, ErrorMessage = "Соль пароля" },
            new EntityFieldInfo { FieldName = "isActive", FieldType = FieldType.Boolean, IsRequired = false },
            new EntityFieldInfo { FieldName = "lastLogin", FieldType = FieldType.DateTime, IsRequired = false }
        };

        private static readonly List<EntityFieldInfo> PcbOrderStatesFields = new List<EntityFieldInfo>
        {
            new EntityFieldInfo { FieldName = "title", FieldType = FieldType.String, IsRequired = true, MaxLength = 100, ErrorMessage = "Название состояния заказа PCB обязательно" },
            new EntityFieldInfo { FieldName = "description", FieldType = FieldType.String, IsRequired = false, MaxLength = 500, ErrorMessage = "Описание не должно превышать 500 символов" }
        };

        private static readonly List<EntityFieldInfo> LicenseTypesFields = new List<EntityFieldInfo>
        {
            new EntityFieldInfo { FieldName = "title", FieldType = FieldType.String, IsRequired = true, MaxLength = 100, ErrorMessage = "Название типа лицензии обязательно" },
            new EntityFieldInfo { FieldName = "description", FieldType = FieldType.String, IsRequired = false, MaxLength = 500, ErrorMessage = "Описание не должно превышать 500 символов" }
        };

        private static readonly List<EntityFieldInfo> ComponentPlacementsFields = new List<EntityFieldInfo>
        {
            new EntityFieldInfo { FieldName = "title", FieldType = FieldType.String, IsRequired = true, MaxLength = 100, ErrorMessage = "Название размещения обязательно" },
            new EntityFieldInfo { FieldName = "description", FieldType = FieldType.String, IsRequired = false, MaxLength = 500, ErrorMessage = "Описание не должно превышать 500 символов" },
            new EntityFieldInfo { FieldName = "componentPlacementTypeId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Тип размещения обязателен" }
        };

        private static readonly List<EntityFieldInfo> WriteoffFields = new List<EntityFieldInfo>
        {
            new EntityFieldInfo { FieldName = "writeoffDate", FieldType = FieldType.Date, IsRequired = true, ErrorMessage = "Дата списания обязательна" },
            new EntityFieldInfo { FieldName = "count", FieldType = FieldType.Integer, IsRequired = true, MinValue = 1, ErrorMessage = "Количество должно быть положительным числом" },
            new EntityFieldInfo { FieldName = "comment", FieldType = FieldType.String, IsRequired = false, MaxLength = 500, ErrorMessage = "Комментарий не должен превышать 500 символов" },
            new EntityFieldInfo { FieldName = "quantity", FieldType = FieldType.Integer, IsRequired = true, MinValue = 1, ErrorMessage = "Количество должно быть положительным числом" },
            new EntityFieldInfo { FieldName = "reason", FieldType = FieldType.String, IsRequired = false, MaxLength = 500, ErrorMessage = "Причина не должна превышать 500 символов" },
            new EntityFieldInfo { FieldName = "componentId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Компонент обязателен" },
            new EntityFieldInfo { FieldName = "writeoffReasonId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Причина списания обязательна" },
            new EntityFieldInfo { FieldName = "employeeId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Сотрудник обязателен" }
        };

        private static readonly List<EntityFieldInfo> WriteoffReasonsFields = new List<EntityFieldInfo>
        {
            new EntityFieldInfo { FieldName = "title", FieldType = FieldType.String, IsRequired = true, MaxLength = 100, ErrorMessage = "Название причины списания обязательно" },
            new EntityFieldInfo { FieldName = "description", FieldType = FieldType.String, IsRequired = false, MaxLength = 500, ErrorMessage = "Описание не должно превышать 500 символов" }
        };

        private static readonly List<EntityFieldInfo> BillsForPayFields = new List<EntityFieldInfo>
        {
            new EntityFieldInfo { FieldName = "billNumber", FieldType = FieldType.String, IsRequired = true, MaxLength = 50, ErrorMessage = "Номер счета обязателен" },
            new EntityFieldInfo { FieldName = "numberBill", FieldType = FieldType.String, IsRequired = true, MaxLength = 50, ErrorMessage = "Номер счета обязателен" },
            new EntityFieldInfo { FieldName = "scanPhoto", FieldType = FieldType.String, IsRequired = false, ErrorMessage = "Скан счета" },
            new EntityFieldInfo { FieldName = "totalAmount", FieldType = FieldType.Float, IsRequired = true, MinValue = 0f, ErrorMessage = "Общая сумма должна быть положительным числом" },
            new EntityFieldInfo { FieldName = "vat", FieldType = FieldType.Float, IsRequired = false, MinValue = 0f, ErrorMessage = "НДС должен быть числом больше или равен 0" },
            new EntityFieldInfo { FieldName = "link", FieldType = FieldType.Url, IsRequired = false, MaxLength = 500, ErrorMessage = "Ссылка не должна превышать 500 символов" },
            new EntityFieldInfo { FieldName = "billDate", FieldType = FieldType.Date, IsRequired = true, ErrorMessage = "Дата счета обязательна" },
            new EntityFieldInfo { FieldName = "amount", FieldType = FieldType.Float, IsRequired = true, MinValue = 0f, ErrorMessage = "Сумма должна быть положительным числом" },
            new EntityFieldInfo { FieldName = "isPaid", FieldType = FieldType.Boolean, IsRequired = false },
            new EntityFieldInfo { FieldName = "paidDate", FieldType = FieldType.Date, IsRequired = false, ErrorMessage = "Дата оплаты" },
            new EntityFieldInfo { FieldName = "organizationId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Организация обязательна" }
        };

        private static readonly List<EntityFieldInfo> BillsComponentsFields = new List<EntityFieldInfo>
        {
              new EntityFieldInfo { FieldName = "billId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Счет обязателен" },
            new EntityFieldInfo { FieldName = "componentId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Компонент обязателен" },
            new EntityFieldInfo { FieldName = "quantity", FieldType = FieldType.Integer, IsRequired = true, MinValue = 1, ErrorMessage = "Количество должно быть положительным числом" },
            new EntityFieldInfo { FieldName = "price", FieldType = FieldType.Float, IsRequired = true, MinValue = 0f, ErrorMessage = "Цена должна быть положительным числом" }
        };

        private static readonly List<EntityFieldInfo> ComponentPlacementTypeFields = new List<EntityFieldInfo>
        {
              new EntityFieldInfo { FieldName = "title", FieldType = FieldType.String, IsRequired = true, MaxLength = 100, ErrorMessage = "Название типа размещения обязательно" },
            new EntityFieldInfo { FieldName = "description", FieldType = FieldType.String, IsRequired = false, MaxLength = 500, ErrorMessage = "Описание не должно превышать 500 символов" }
        };

        private static readonly List<EntityFieldInfo> ServerArrivalsFields = new List<EntityFieldInfo>
        {
              new EntityFieldInfo { FieldName = "arrivalDate", FieldType = FieldType.DateTime, IsRequired = true, ErrorMessage = "Дата прибытия обязательна" },
            new EntityFieldInfo { FieldName = "quantity", FieldType = FieldType.Integer, IsRequired = true, MinValue = 1, ErrorMessage = "Количество должно быть положительным числом" },
            new EntityFieldInfo { FieldName = "componentId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Компонент обязателен" },
            new EntityFieldInfo { FieldName = "employeeId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Сотрудник обязателен" }
        };

        private static readonly List<EntityFieldInfo> ServerWriteoffFields = new List<EntityFieldInfo>
        {
              new EntityFieldInfo { FieldName = "writeoffDate", FieldType = FieldType.DateTime, IsRequired = true, ErrorMessage = "Дата списания обязательна" },
            new EntityFieldInfo { FieldName = "quantity", FieldType = FieldType.Integer, IsRequired = true, MinValue = 1, ErrorMessage = "Количество должно быть положительным числом" },
            new EntityFieldInfo { FieldName = "componentId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Компонент обязателен" },
            new EntityFieldInfo { FieldName = "writeoffReasonId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Причина списания обязательна" },
            new EntityFieldInfo { FieldName = "employeeId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Сотрудник обязателен" }
        };

        private static readonly List<EntityFieldInfo> ShipmentPackageFields = new List<EntityFieldInfo>
        {
              new EntityFieldInfo { FieldName = "packageNumber", FieldType = FieldType.String, IsRequired = true, MaxLength = 50, ErrorMessage = "Номер посылки обязателен" },
            new EntityFieldInfo { FieldName = "weight", FieldType = FieldType.Float, IsRequired = true, MinValue = 0f, ErrorMessage = "Вес должен быть положительным числом" },
            new EntityFieldInfo { FieldName = "shipmentId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Отправка обязательна" },
            new EntityFieldInfo { FieldName = "shipmentPackageStateId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Состояние посылки обязательно" }
        };

        private static readonly List<EntityFieldInfo> ShipmentPackageStatesFields = new List<EntityFieldInfo>
        {
              new EntityFieldInfo { FieldName = "title", FieldType = FieldType.String, IsRequired = true, MaxLength = 100, ErrorMessage = "Название состояния посылки обязательно" },
            new EntityFieldInfo { FieldName = "description", FieldType = FieldType.String, IsRequired = false, MaxLength = 500, ErrorMessage = "Описание не должно превышать 500 символов" }
        };

        private static readonly List<EntityFieldInfo> ShipmentTripsFields = new List<EntityFieldInfo>
        {
              new EntityFieldInfo { FieldName = "tripDate", FieldType = FieldType.DateTime, IsRequired = true, ErrorMessage = "Дата поездки обязательна" },
            new EntityFieldInfo { FieldName = "description", FieldType = FieldType.String, IsRequired = false, MaxLength = 500, ErrorMessage = "Описание не должно превышать 500 символов" },
            new EntityFieldInfo { FieldName = "shipmentId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Отправка обязательна" },
            new EntityFieldInfo { FieldName = "transporterId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Перевозчик обязателен" }
        };

        private static readonly List<EntityFieldInfo> ShipmentStandsFields = new List<EntityFieldInfo>
        {
              new EntityFieldInfo { FieldName = "quantity", FieldType = FieldType.Integer, IsRequired = true, MinValue = 1, ErrorMessage = "Количество должно быть положительным числом" },
            new EntityFieldInfo { FieldName = "shipmentId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Отправка обязательна" },
            new EntityFieldInfo { FieldName = "standId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Стенд обязателен" }
        };

        private static readonly List<EntityFieldInfo> StandPackagesFields = new List<EntityFieldInfo>
        {
              new EntityFieldInfo { FieldName = "shipmentPackageId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Посылка обязательна" },
            new EntityFieldInfo { FieldName = "standId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Стенд обязателен" }
        };

        private static readonly List<EntityFieldInfo> EmployeesDepartmentsFields = new List<EntityFieldInfo>
        {
              new EntityFieldInfo { FieldName = "employeeId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Сотрудник обязателен" },
            new EntityFieldInfo { FieldName = "departmentId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Отдел обязателен" },
            new EntityFieldInfo { FieldName = "startDate", FieldType = FieldType.Date, IsRequired = true, ErrorMessage = "Дата начала обязательна" },
            new EntityFieldInfo { FieldName = "endDate", FieldType = FieldType.Date, IsRequired = false, ErrorMessage = "Дата окончания" }
        };

        private static readonly List<EntityFieldInfo> EmployeesProfessionsFields = new List<EntityFieldInfo>
        {
              new EntityFieldInfo { FieldName = "employeeId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Сотрудник обязателен" },
            new EntityFieldInfo { FieldName = "professionId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Профессия обязательна" },
            new EntityFieldInfo { FieldName = "startDate", FieldType = FieldType.Date, IsRequired = true, ErrorMessage = "Дата начала обязательна" },
            new EntityFieldInfo { FieldName = "endDate", FieldType = FieldType.Date, IsRequired = false, ErrorMessage = "Дата окончания" }
        };

        private static readonly List<EntityFieldInfo> EmployeesVacationsFields = new List<EntityFieldInfo>
        {
              new EntityFieldInfo { FieldName = "startDate", FieldType = FieldType.Date, IsRequired = true, ErrorMessage = "Дата начала отпуска обязательна" },
            new EntityFieldInfo { FieldName = "endDate", FieldType = FieldType.Date, IsRequired = true, ErrorMessage = "Дата окончания отпуска обязательна" },
            new EntityFieldInfo { FieldName = "employeeId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Сотрудник обязателен" },
            new EntityFieldInfo { FieldName = "vacationTypeId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Тип отпуска обязателен" }
        };

        private static readonly List<EntityFieldInfo> InvoicesComponentsFields = new List<EntityFieldInfo>
        {
              new EntityFieldInfo { FieldName = "invoiceId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Накладная обязательна" },
            new EntityFieldInfo { FieldName = "componentId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Компонент обязателен" },
            new EntityFieldInfo { FieldName = "quantity", FieldType = FieldType.Integer, IsRequired = true, MinValue = 1, ErrorMessage = "Количество должно быть положительным числом" }
        };

        private static readonly List<EntityFieldInfo> CurrentTasksComponentsFields = new List<EntityFieldInfo>
        {
              new EntityFieldInfo { FieldName = "currentTaskId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Текущая задача обязательна" },
            new EntityFieldInfo { FieldName = "componentId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Компонент обязателен" },
            new EntityFieldInfo { FieldName = "quantity", FieldType = FieldType.Integer, IsRequired = true, MinValue = 1, ErrorMessage = "Количество должно быть положительным числом" }
        };

        private static readonly List<EntityFieldInfo> OrderRequestsComponentsFields = new List<EntityFieldInfo>
        {
              new EntityFieldInfo { FieldName = "orderRequestId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Запрос заказа обязателен" },
            new EntityFieldInfo { FieldName = "componentId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Компонент обязателен" },
            new EntityFieldInfo { FieldName = "quantity", FieldType = FieldType.Integer, IsRequired = true, MinValue = 1, ErrorMessage = "Количество должно быть положительным числом" }
        };

        private static readonly List<EntityFieldInfo> PcbsComponentsFields = new List<EntityFieldInfo>
        {
              new EntityFieldInfo { FieldName = "pcbId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "PCB обязателен" },
            new EntityFieldInfo { FieldName = "componentId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Компонент обязателен" },
            new EntityFieldInfo { FieldName = "quantity", FieldType = FieldType.Integer, IsRequired = true, MinValue = 1, ErrorMessage = "Количество должно быть положительным числом" }
        };

        private static readonly List<EntityFieldInfo> StandTasksComponentsFields = new List<EntityFieldInfo>
        {
              new EntityFieldInfo { FieldName = "standTaskId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Задача стенда обязательна" },
            new EntityFieldInfo { FieldName = "componentId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Компонент обязателен" },
            new EntityFieldInfo { FieldName = "quantity", FieldType = FieldType.Integer, IsRequired = true, MinValue = 1, ErrorMessage = "Количество должно быть положительным числом" }
        };

        private static readonly List<EntityFieldInfo> SupplierComponentsFields = new List<EntityFieldInfo>
        {
              new EntityFieldInfo { FieldName = "supplierId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Поставщик обязателен" },
            new EntityFieldInfo { FieldName = "componentId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Компонент обязателен" },
            new EntityFieldInfo { FieldName = "price", FieldType = FieldType.Float, IsRequired = true, MinValue = 0f, ErrorMessage = "Цена должна быть положительным числом" },
            new EntityFieldInfo { FieldName = "deliveryTime", FieldType = FieldType.Integer, IsRequired = false, MinValue = 1, ErrorMessage = "Время доставки должно быть положительным числом" }
        };

        private static readonly List<EntityFieldInfo> CurrentTaskStatesLogFields = new List<EntityFieldInfo>
        {
              new EntityFieldInfo { FieldName = "changeDate", FieldType = FieldType.DateTime, IsRequired = true, ErrorMessage = "Дата изменения обязательна" },
            new EntityFieldInfo { FieldName = "comment", FieldType = FieldType.String, IsRequired = false, MaxLength = 500, ErrorMessage = "Комментарий не должен превышать 500 символов" },
            new EntityFieldInfo { FieldName = "currentTaskId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Текущая задача обязательна" },
            new EntityFieldInfo { FieldName = "currentTaskStateId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Состояние задачи обязательно" },
            new EntityFieldInfo { FieldName = "employeeId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Сотрудник обязателен" }
        };

        private static readonly List<EntityFieldInfo> InventarizationFields = new List<EntityFieldInfo>
        {
              new EntityFieldInfo { FieldName = "inventarizationDate", FieldType = FieldType.DateTime, IsRequired = true, ErrorMessage = "Дата инвентаризации обязательна" },
            new EntityFieldInfo { FieldName = "componentCount", FieldType = FieldType.Integer, IsRequired = true, MinValue = 0, ErrorMessage = "Количество компонентов должно быть положительным числом" },
            new EntityFieldInfo { FieldName = "inventarizationQuality", FieldType = FieldType.String, IsRequired = false, MaxLength = 100, ErrorMessage = "Качество инвентаризации не должно превышать 100 символов" },
            new EntityFieldInfo { FieldName = "actualQuantity", FieldType = FieldType.Integer, IsRequired = true, MinValue = 0, ErrorMessage = "Фактическое количество должно быть положительным числом" },
            new EntityFieldInfo { FieldName = "expectedQuantity", FieldType = FieldType.Integer, IsRequired = true, MinValue = 0, ErrorMessage = "Ожидаемое количество должно быть положительным числом" },
            new EntityFieldInfo { FieldName = "difference", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Разница обязательна" },
            new EntityFieldInfo { FieldName = "componentId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Компонент обязателен" },
            new EntityFieldInfo { FieldName = "employeeId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Сотрудник обязателен" }
        };

        #endregion

        #region Существующие сущности без изменений

        private static readonly List<EntityFieldInfo> ArrivalInvoicesFields = new List<EntityFieldInfo>
        {
              new EntityFieldInfo { FieldName = "date", FieldType = FieldType.Date, IsRequired = true, ErrorMessage = "Дата накладной обязательна" },
            new EntityFieldInfo { FieldName = "numberInvoice", FieldType = FieldType.String, IsRequired = true, MinLength = 3, MaxLength = 45, ErrorMessage = "Номер накладной должен содержать от 3 до 45 символов" },
            new EntityFieldInfo { FieldName = "scanPhoto", FieldType = FieldType.String, IsRequired = false, ErrorMessage = "Скан документа должен быть загружен" },
            new EntityFieldInfo { FieldName = "dateTimeToWarehouse", FieldType = FieldType.DateTime, IsRequired = true, ErrorMessage = "Дата и время прибытия на склад обязательны" },
            new EntityFieldInfo { FieldName = "price", FieldType = FieldType.Float, IsRequired = true, MinValue = 0f, ErrorMessage = "Цена должна быть числом больше или равна 0" },
            new EntityFieldInfo { FieldName = "vat", FieldType = FieldType.Boolean, IsRequired = false },
            new EntityFieldInfo { FieldName = "supplierId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Поставщик обязателен для выбора" },
            new EntityFieldInfo { FieldName = "factoryId", FieldType = FieldType.Integer, IsRequired = true, ErrorMessage = "Фабрика обязательна для выбора" }
        };

        private static readonly List<EntityFieldInfo> CurrentTasksFields = new List<EntityFieldInfo>
        {
              new EntityFieldInfo { FieldName = "isCompleted", FieldType = FieldType.Boolean, IsRequired = false },
            new EntityFieldInfo { FieldName = "currentTaskStateId", FieldType = FieldType.Integer, IsRequired = false },
            new EntityFieldInfo { FieldName = "standTaskId", FieldType = FieldType.Integer, IsRequired = false },
            new EntityFieldInfo { FieldName = "shipmentStandId", FieldType = FieldType.Integer, IsRequired = false }
        };

        #endregion
    }

    /// <summary>
    /// Информация о поле сущности для валидации
    /// </summary>
    public class EntityFieldInfo
    {
        public string FieldName { get; set; }
        public FieldType FieldType { get; set; } = FieldType.String;
        public bool IsRequired { get; set; }
        public int MinLength { get; set; } = 0;
        public int MaxLength { get; set; } = 255;
        public float MinValue { get; set; } = float.MinValue;
        public float MaxValue { get; set; } = float.MaxValue;
        public string ErrorMessage { get; set; }
        public string CustomPattern { get; set; }
    }
}