using System.Collections.Generic;
using System.Collections;

public class EmployeesAddFlow : IAddFlowStrategy
{
	private EmployeeModalStrategy employeeStrategy;
	
	public EmployeesAddFlow(ActionsButtonsMainMenu actionsButtons)
	{
		employeeStrategy = new EmployeeModalStrategy(actionsButtons);
	}
	
	public bool HandleAdd(ActionsButtonsMainMenu ctx)
	{
		if (ctx == null) return false;
		var table = ctx.GetCurrentTable();
		if (table != "employees") return false;

		var effectiveRow = ctx.GetEffectiveSelectedRow();
		if (effectiveRow == null || (effectiveRow.ContainsKey("nodeType") && effectiveRow["nodeType"].ToString() != "departments"))
		{
			ctx.OpenAddModal("departments", (deptData) => {
				ctx.StartCoroutine(ctx.Api().AddRecordWithResponse("departments", deptData, (ok, createdDept) => {
					if (ok) ctx.ReloadCurrentUI();
				}));
			});
			return true;
		}

		// Получаем ID выбранного отдела
		string departmentId = effectiveRow["id"].ToString();
		
		// Открываем модальное окно с кастомной стратегией для сотрудников
		// Передаем ID отдела для автоматического создания связи
		ctx.OpenModalWithStrategy(employeeStrategy, false, new Dictionary<string, object> { { "departmentId", departmentId } });
		return true;
	}
	
	public bool HandleEdit(ActionsButtonsMainMenu ctx, Dictionary<string, object> data)
	{
		if (ctx == null) return false;
		var table = ctx.GetCurrentTable();
		if (table != "employees") return false;

		// Если редактируемая запись не из таблицы employees (например, выбран узел отдела), открываем обычную модалку для той таблицы
		if (data != null && data.ContainsKey("nodeType") && data["nodeType"] != null && data["nodeType"].ToString() != "employees")
		{
			string nodeType = data["nodeType"].ToString();
			ctx.OpenEditModal(nodeType, data, (updated) => {
				if (data.ContainsKey("id"))
				{
					ctx.StartCoroutine(ctx.Api().UpdateRecord(nodeType, data["id"], updated, (ok) => {
						ctx.ReloadCurrentUI();
					}));
				}
			});
			return true;
		}

		// Редактирование сотрудника — передаём в стратегию ПОЛНУЮ строку по id
		if (data != null && data.ContainsKey("id") && data["id"] != null)
		{
			string id = data["id"].ToString();
			ctx.StartCoroutine(ctx.Api().GetRowById("employees", id, (fullRow) => {
				var payload = fullRow ?? data;
				ctx.OpenModalWithStrategy(employeeStrategy, true, payload);
			}));
		}
		else
		{
			ctx.OpenModalWithStrategy(employeeStrategy, true, data);
		}
		return true;
	}
}


