using System.Collections.Generic;

public class OrganizationsAddFlow : IAddFlowStrategy
{
	public bool HandleAdd(ActionsButtonsMainMenu ctx)
	{
		if (ctx == null) return false;
		var table = ctx.GetCurrentTable();
		if (table != "organizations") return false;

		var selected = ctx.GetEffectiveSelectedRow();
		// Если не выбран тип — сначала добавляем organization_types
		if (selected == null || (selected.ContainsKey("nodeType") && selected["nodeType"].ToString() != "organization_types"))
		{
			ctx.OpenAddModal("organization_types", (typeData) => {
				ctx.StartCoroutine(ctx.Api().AddRecordWithResponse("organization_types", typeData, (ok, createdType) => {
					if (ok) ctx.ReloadCurrentUI();
				}));
			});
			return true;
		}

		// Выбран тип — открываем форму организаций c префиллом parentId и organizationTypeId
		var initial = new Dictionary<string, object>();
		if (selected.ContainsKey("id"))
		{
			var typeId = selected["id"].ToString();
			initial["parentId"] = typeId;
			initial["organizationTypeId"] = typeId;
		}
		ctx.OpenAddModalWithData("organizations", initial, (orgData) => {
			// Гарантируем значения перед отправкой
			if (!orgData.ContainsKey("parentId") && selected.ContainsKey("id"))
				orgData["parentId"] = selected["id"].ToString();
			if (!orgData.ContainsKey("organizationTypeId") && selected.ContainsKey("id"))
				orgData["organizationTypeId"] = selected["id"].ToString();

			ctx.StartCoroutine(ctx.Api().AddRecordWithResponse("organizations", orgData, (okOrg, createdOrg) => {
				if (okOrg) ctx.ReloadCurrentUI();
			}));
		});
		return true;
	}
	
	public bool HandleEdit(ActionsButtonsMainMenu ctx, Dictionary<string, object> data)
	{
		if (ctx == null) return false;
		var currentTable = ctx.GetCurrentTable();
		if (currentTable != "organizations") return false;

		// Роутинг по nodeType: позволяем редактировать и тип организации, и саму организацию
		string nodeType = data != null && data.ContainsKey("nodeType") ? data["nodeType"].ToString() : null;
		string tableName = "organizations";
		if (!string.IsNullOrEmpty(nodeType))
		{
			if (nodeType == "organization_types") tableName = "organization_types";
			else if (nodeType == "organizations") tableName = "organizations";
		}

		if (!data.ContainsKey("id") || data["id"] == null) return false; // без id редактировать нечего

		ctx.OpenEditModal(tableName, data, (updatedData) => {
			ctx.StartCoroutine(ctx.Api().UpdateRecord(tableName, data["id"].ToString(), updatedData, (ok) => {
				if (ok) ctx.ReloadCurrentUI();
			}));
		});
		return true;
	}
}


