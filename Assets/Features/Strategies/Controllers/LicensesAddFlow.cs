using System.Collections.Generic;

public class LicensesAddFlow : IAddFlowStrategy
{
	public bool HandleAdd(ActionsButtonsMainMenu ctx)
	{
		if (ctx == null) return false;
		var table = ctx.GetCurrentTable();
		if (table != "license") return false;

		var selected = ctx.GetEffectiveSelectedRow();
		// Если не выбран тип — сначала добавляем license_types
		if (selected == null || (selected.ContainsKey("nodeType") && selected["nodeType"].ToString() != "license_types"))
		{
			ctx.OpenAddModal("license_types", (typeData) => {
				ctx.StartCoroutine(ctx.Api().AddRecordWithResponse("license_types", typeData, (ok, createdType) => {
					if (ok) ctx.ReloadCurrentUI();
				}));
			});
			return true;
		}

		// Выбран тип — открываем форму лицензий c префиллом licenseTypeId
		var initial = new Dictionary<string, object>();
		if (selected.ContainsKey("id")) initial["licenseTypeId"] = selected["id"].ToString();
		ctx.OpenAddModalWithData("license", initial, (licData) => { // API-таблица: 'license'
			if (!licData.ContainsKey("licenseTypeId") && selected.ContainsKey("id"))
				licData["licenseTypeId"] = selected["id"].ToString();
			ctx.StartCoroutine(ctx.Api().AddRecordWithResponse("license", licData, (okLic, createdLic) => {
				if (okLic) ctx.ReloadCurrentUI();
			}));
		});
		return true;
	}
	
	public bool HandleEdit(ActionsButtonsMainMenu ctx, Dictionary<string, object> data)
	{
		if (ctx == null) return false;
		var currentTable = ctx.GetCurrentTable();
		if (currentTable != "license") return false;

		// Роутинг по nodeType: позволяем редактировать и тип лицензии, и саму лицензию
		string nodeType = data != null && data.ContainsKey("nodeType") ? data["nodeType"].ToString() : null;
		string tableName = "license";
		if (!string.IsNullOrEmpty(nodeType))
		{
			if (nodeType == "license_types") tableName = "license_types";
			else if (nodeType == "license") tableName = "license";
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


