using System.Collections.Generic;
using Models.Menu;
using UnityEngine;

namespace Services.Menu
{
	/// <summary>
	/// Строит дерево меню. Жестко зашитые пункты.
	/// </summary>
	public class MenuDefinitionService : MonoBehaviour
	{
		public MenuNode BuildRoot()
		{
			// Документы
			var documents = new MenuNode("Документы", null, new List<MenuNode>
			{
				new MenuNode("Менеджер накладных", "arrival_invoices"),
				new MenuNode("Менеджер счетов", "bills_for_pay"),
				new MenuNode("Заявки на заказ комплектующих", "order_requests"),
			});

			// Организации
			var orgEmployeesSub = new MenuNode("Менеджер сотрудников наших компаний", null, new List<MenuNode>
			{
				new MenuNode("Сотрудники", "employees"),
				new MenuNode("Отделы", "departments"),
				new MenuNode("Профессии", "professions"),
			});
			var organizations = new MenuNode("Организации", null, new List<MenuNode>
			{
				new MenuNode("Менеджер организаций", "organizations"),
				orgEmployeesSub,
			});

			// Лицензии (сразу таблица)
			var licenses = new MenuNode("Лицензии", "license");

			// Стенды
			var stands = new MenuNode("Стенды", null, new List<MenuNode>
			{
				new MenuNode("Техпроцесс", "stand_tasks"),
				new MenuNode("Менеджер стендов", "stands"),
			});

			// Отгрузки
			var shipments = new MenuNode("Отгрузки", null, new List<MenuNode>
			{
				new MenuNode("Менеджер отгрузок", "shipments"),
				// При клике на "Текущие задачи" хотим выбрать: открыть current_tasks или зайти в состояния
				new MenuNode("Текущие задачи и состояния", null, new List<MenuNode>
				{
					new MenuNode("Текущие задачи", "current_tasks"),
					new MenuNode("Состояния текущих задач", "current_task_states"),
				}),
			});

			// Склад
			var warehouse = new MenuNode("Склад", null, new List<MenuNode>
			{
				new MenuNode("Компоненты склада", null, new List<MenuNode>
				{
					new MenuNode("Компоненты", "components"),
					new MenuNode("Размещения компонентов", "component_placements"),
				}),
				new MenuNode("Приход/Расход", "writeoff"),
				new MenuNode("Инвентаризации", "inventarization"),
			});

			// Печатные платы
			var pcbs = new MenuNode("Печатные платы", null, new List<MenuNode>
			{
				new MenuNode("Заказы ПП", "pcb_orders"),
				new MenuNode("Менеджер печатных плат", "pcbs"),
			});

			// Система
			var system = new MenuNode("Система", null, new List<MenuNode>
			{
				new MenuNode("Менеджер пользователей", "user"),
				new MenuNode("Импорт/Экспорт БД, таблиц. Обновление БД на новый год", null), // заглушка
				new MenuNode("Журнал текущих задач", "current_task_states_log"),
			});

			return new MenuNode("ROOT", null, new List<MenuNode>
			{
				documents,
				organizations,
				licenses,
				stands,
				shipments,
				warehouse,
				pcbs,
				system,
			});
		}
	}
}


