using System.Collections.Generic;

/// <summary>
/// Сервис для получения правильных названий таблиц в нужном падеже
/// Используется для заголовков модальных окон
/// </summary>
public static class TableNameService
{
    private static readonly Dictionary<string, string> tableNames = new Dictionary<string, string>
    {
        // Основные таблицы
        { "employees", "Сотрудник" },
        { "organizations", "Организация" },
        { "licenses", "Лицензия" },
        { "arrival_invoices", "Накладная" },
        { "bills_for_pay", "Счет" },
        { "order_requests", "Заявки на заказ" },
        { "components_categories", "Категория компонента" },
        { "components_subcategories", "Подкатегория компонента" },
        { "components", "Компонент" },
        { "current_tasks", "Текущей задачи" },
        { "stand_tasks", "Задачи стенда" },
        { "stands", "Стенд" },
        { "pcbs_categories", "Категория платы" },
        { "pcbs_subcategories", "Подкатегория платы" },
        { "pcbs", "Плата" },
        { "pcb_orders", "Заказа платы" },
        { "shipments", "Отгрузка" },
        { "invoices_components", "Компонента накладной" },
        { "bills_components", "Компонента счета" },
        { "current_tasks_components", "Компонента текущей задачи" },
        { "stand_tasks_components", "Компонента задачи стенда" },
        { "pcbs_components", "Компонента платы" },
        { "order_requests_components", "Компонента заявки на заказ" },
        { "departments", "Отдел" },
        { "professions", "Профессия" },
        { "employee_departments", "Отдела сотрудников" },
        { "employees_professions", "Профессии сотрудника" },
        { "employees_vacations", "Отпуска сотрудника" },
        { "organization_types", "Типа организации" },
        { "license_types", "Типа лицензии" },
        { "license", "Лицензия" },
        { "component_placement_type", "Тип размещения компонента" },
        { "component_placements", "Размещение компонента" },
        { "current_task_states", "Статуса текущей задачи" },
        { "current_task_states_log", "Лога статуса текущей задачи" },
        { "pcb_order_states", "Статуса заказа платы" },
        { "shipment_package", "Упаковки отгрузки" },
        { "shipment_package_states", "Статуса упаковки отгрузки" },
        { "shipment_trips", "Поездки отгрузки" },
        { "shipments_stands", "Отгрузки стенда" },
        { "stand_packages", "Упаковки стенда" },
        { "stands_types", "Типа стенда" },
        { "supplier_components", "Компонента поставщика" },
        { "writeoff", "Списание" },
        { "writeoff_reasons", "Причины списания" },
        { "server_arrivals", "Прихода сервера" },
        { "server_writeoff", "Списания сервера" },
        { "inventarization", "Инвентаризация" },
        { "peoples", "Человек" },
        { "user", "Пользователь" }
    };

    /// <summary>
    /// Получает правильное название таблицы для заголовка модального окна
    /// </summary>
    /// <param name="tableName">Название таблицы в базе данных</param>
    /// <returns>Название в правильном падеже для заголовка</returns>
    public static string GetTableDisplayName(string tableName)
    {
        if (string.IsNullOrEmpty(tableName))
            return "записи";

        // Приводим к нижнему регистру для поиска
        string key = tableName.ToLower();
        
        if (tableNames.TryGetValue(key, out string displayName))
            return displayName;
        
        // Если нет в словаре, возвращаем оригинальное название
        return tableName;
    }

    /// <summary>
    /// Формирует заголовок для модального окна добавления
    /// </summary>
    /// <param name="tableName">Название таблицы</param>
    /// <returns>Заголовок вида "Добавление [название в правильном падеже]"</returns>
    public static string GetAddTitle(string tableName)
    {
        return $"Добавление {GetTableDisplayName(tableName)}";
    }

    /// <summary>
    /// Формирует заголовок для модального окна редактирования
    /// </summary>
    /// <param name="tableName">Название таблицы</param>
    /// <returns>Заголовок вида "Редактирование [название в правильном падеже]"</returns>
    public static string GetEditTitle(string tableName)
    {
        return $"Редактирование {GetTableDisplayName(tableName)}";
    }
}
