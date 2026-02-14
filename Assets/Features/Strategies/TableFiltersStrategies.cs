using System.Collections.Generic;

namespace Strategies
{
    /// <summary>
    /// Интерфейс стратегии фильтрации для таблицы
    /// </summary>
    public interface ITableFilterStrategy
    {
        /// <summary>
        /// Получить список фильтруемых колонок (или их ключей)
        /// </summary>
        List<string> GetAvailableFilters();

        /// <summary>
        /// Получить список фильтров по умолчанию (если нужно)
        /// </summary>
        List<string> GetDefaultFilters();
    }

    /// <summary>
    /// Заглушка для arrival_invoices
    /// </summary>
    public class ArrivalInvoicesFilterStrategy : ITableFilterStrategy
    {
        public List<string> GetAvailableFilters() => new List<string> { "date", "numberInvoice", "dateTimeToWarehouse", "supplierName", "componentTitle" };
        public List<string> GetDefaultFilters() => GetAvailableFilters();
    }

    /// <summary>
    /// Заглушка для bills_for_pay
    /// </summary>
    public class BillsForPayFilterStrategy : ITableFilterStrategy
    {
        public List<string> GetAvailableFilters() => new List<string> { "date", "numberBill", "expectedSupplyDate", "supplierName", "componentTitle" };
        public List<string> GetDefaultFilters() => GetAvailableFilters();
    }

    /// <summary>
    /// Заглушка для components
    /// </summary>
    public class ComponentsFilterStrategy : ITableFilterStrategy
    {
        public List<string> GetAvailableFilters() => new List<string> { "title", "subcategoryName", "material", "receiptDate", "placementInfo" };
        public List<string> GetDefaultFilters() => GetAvailableFilters();
    }

    /// <summary>
    /// Заглушка для current_tasks
    /// </summary>
    public class CurrentTasksFilterStrategy : ITableFilterStrategy
    {
        public List<string> GetAvailableFilters() => new List<string> { "deadline", "taskTitle", "employees", "currentTaskState", "stand", "standTask", "component", "manufactureTime" };
        public List<string> GetDefaultFilters() => GetAvailableFilters();
    }

    /// <summary>
    /// Заглушка для employees
    /// </summary>
    public class EmployeesFilterStrategy : ITableFilterStrategy
    {
        public List<string> GetAvailableFilters() => new List<string> { "employees", "hiringDate", "dismissalDate", "birthDate" };
        public List<string> GetDefaultFilters() => GetAvailableFilters();
    }

    /// <summary>
    /// Заглушка для organizations
    /// </summary>
    public class OrganizationsFilterStrategy : ITableFilterStrategy
    {
        public List<string> GetAvailableFilters() => new List<string> { "fullName", "shortName", "lawAddress", "factAddress", "postAddress", "inn", "kpp", "orgn", "orgnDate", "digitalDocs", "rating" };
        public List<string> GetDefaultFilters() => GetAvailableFilters();
    }

    /// <summary>
    /// Заглушка для pcb_orders
    /// </summary>
    public class PcbOrdersFilterStrategy : ITableFilterStrategy
    {
        public List<string> GetAvailableFilters() => new List<string> { "orderDate", "manufacturer", "factory", "subcategoryName", "pcbName", "article", "status" };
        public List<string> GetDefaultFilters() => GetAvailableFilters();
    }

    /// <summary>
    /// Заглушка для pcbs
    /// </summary>
    public class PcbsFilterStrategy : ITableFilterStrategy
    {
        public List<string> GetAvailableFilters() => new List<string> { "subcategoryName", "pcbName", "componentTitle", "material", "receiptDate" };
        public List<string> GetDefaultFilters() => GetAvailableFilters();
    }

    /// <summary>
    /// Фабрика для получения стратегии по имени таблицы
    /// </summary>
    public static class TableFilterStrategyFactory
    {
        private static readonly Dictionary<string, ITableFilterStrategy> strategies = new Dictionary<string, ITableFilterStrategy>
        {
            { "arrival_invoices", new ArrivalInvoicesFilterStrategy() },
            { "bills_for_pay", new BillsForPayFilterStrategy() },
            { "components", new ComponentsFilterStrategy() },
            { "current_tasks", new CurrentTasksFilterStrategy() },
            { "employees", new EmployeesFilterStrategy() },
            { "organizations", new OrganizationsFilterStrategy() },
            { "pcb_orders", new PcbOrdersFilterStrategy() },
            { "pcbs", new PcbsFilterStrategy() },
        };

        public static ITableFilterStrategy GetStrategy(string tableName)
        {
            if (strategies.TryGetValue(tableName, out var strategy))
                return strategy;
            return null;
        }
    }
} 