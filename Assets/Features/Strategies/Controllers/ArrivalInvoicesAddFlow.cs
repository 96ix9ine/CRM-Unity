using System.Collections.Generic;

public class ArrivalInvoicesAddFlow : IAddFlowStrategy
{
	public bool HandleAdd(ActionsButtonsMainMenu ctx)
	{
		if (ctx == null) return false;
		var table = ctx.GetCurrentTable();
		if (table != "arrival_invoices") return false;

		var selected = ctx.GetEffectiveSelectedRow();
		string nodeType = selected != null && selected.ContainsKey("nodeType") ? selected["nodeType"].ToString() : null;
		string selectedId = selected != null && selected.ContainsKey("id") ? selected["id"].ToString() : null;

		// Если выбрана накладная (arrival_invoices) - добавляем компоненты (invoices_components)
		if (!string.IsNullOrEmpty(nodeType) && nodeType == "arrival_invoices" && !string.IsNullOrEmpty(selectedId))
		{
			var initialData = new Dictionary<string, object>
			{
				{ "arrivalInvoiceId", selectedId }
			};

			ctx.OpenAddModalWithData("invoices_components", initialData, (data) => {
				// Убеждаемся, что arrivalInvoiceId установлен
				if (!data.ContainsKey("arrivalInvoiceId"))
					data["arrivalInvoiceId"] = selectedId;

				ctx.StartCoroutine(ctx.Api().AddRecordWithResponse("invoices_components", data, (ok, created) => {
					if (ok) ctx.ReloadCurrentUI();
				}));
			});
			return true;
		}

		// Иначе (выбран корень дерева или другой nodeType) - создаём новую накладную
		ctx.OpenAddModal("arrival_invoices", (data) => {
			ctx.StartCoroutine(ctx.Api().AddRecordWithResponse("arrival_invoices", data, (ok, created) => {
				if (ok) ctx.ReloadCurrentUI();
			}));
		});
		return true;
	}
	
	public bool HandleEdit(ActionsButtonsMainMenu ctx, Dictionary<string, object> data)
	{
		if (ctx == null) return false;
		var table = ctx.GetCurrentTable();
		if (table != "arrival_invoices") return false;

		var selected = ctx.GetEffectiveSelectedRow();
		string nodeType = selected != null && selected.ContainsKey("nodeType") ? selected["nodeType"].ToString() : null;

		// Если выбран компонент накладной (invoices_components) - редактируем его
		if (!string.IsNullOrEmpty(nodeType) && nodeType == "invoices_components")
		{
			ctx.OpenEditModal("invoices_components", data, (updatedData) => {
				ctx.StartCoroutine(ctx.Api().UpdateRecord("invoices_components", data["id"].ToString(), updatedData, (ok) => {
					if (ok) ctx.ReloadCurrentUI();
				}));
			});
			return true;
		}

		// Иначе - редактируем накладную
		ctx.OpenEditModal("arrival_invoices", data, (updatedData) => {
			ctx.StartCoroutine(ctx.Api().UpdateRecord("arrival_invoices", data["id"].ToString(), updatedData, (ok) => {
				if (ok) ctx.ReloadCurrentUI();
			}));
		});
		return true;
	}
}


