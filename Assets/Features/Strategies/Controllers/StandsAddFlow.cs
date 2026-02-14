using System.Collections.Generic;

public class StandsAddFlow : IAddFlowStrategy
{
	public bool HandleAdd(ActionsButtonsMainMenu ctx)
	{
		if (ctx == null) return false;
		var table = ctx.GetCurrentTable();
		if (table != "stands") return false;

		var selected = ctx.GetEffectiveSelectedRow();
		string nodeType = selected != null && selected.ContainsKey("nodeType") ? selected["nodeType"].ToString() : null;
		string selectedId = selected != null && selected.ContainsKey("id") ? selected["id"].ToString() : null;
		string selectedParentId = selected != null && selected.ContainsKey("parentId") ? selected["parentId"].ToString() : null;

		// Если выбран тип стенда — создаём корневую категорию в этом типе
		if (!string.IsNullOrEmpty(nodeType) && nodeType == "stands_types" && !string.IsNullOrEmpty(selectedId))
		{
			var initial = new Dictionary<string, object> { { "parentId", null } };
			ctx.OpenAddModalWithData("stands", initial, (catData) => {
				if (!catData.ContainsKey("parentId")) catData["parentId"] = null;
				ctx.StartCoroutine(ctx.Api().AddRecordWithResponse("stands", catData, (ok, created) => {
					if (ok) ctx.ReloadCurrentUI();
				}));
			});
			return true;
		}

		// Если выбрана корневая категория (parentId == null) — создаём подкатегорию
		if (!string.IsNullOrEmpty(nodeType) && nodeType == "stands_categories" && !string.IsNullOrEmpty(selectedId) && string.IsNullOrEmpty(selectedParentId))
		{
			var initial = new Dictionary<string, object> { { "parentId", selectedId } };
			ctx.OpenAddModalWithData("stands", initial, (subData) => {
				if (!subData.ContainsKey("parentId")) subData["parentId"] = selectedId;
				ctx.StartCoroutine(ctx.Api().AddRecordWithResponse("stands", subData, (ok, created) => {
					if (ok) ctx.ReloadCurrentUI();
				}));
			});
			return true;
		}

		// Если выбрана подкатегория (parentId != null) — создаём подкатегорию в этой подкатегории (рекурсия)
		if (!string.IsNullOrEmpty(nodeType) && nodeType == "stands_categories" && !string.IsNullOrEmpty(selectedId) && !string.IsNullOrEmpty(selectedParentId))
		{
			var initial = new Dictionary<string, object> { { "parentId", selectedId } };
			ctx.OpenAddModalWithData("stands", initial, (subData) => {
				if (!subData.ContainsKey("parentId")) subData["parentId"] = selectedId;
				ctx.StartCoroutine(ctx.Api().AddRecordWithResponse("stands", subData, (ok, created) => {
					if (ok) ctx.ReloadCurrentUI();
				}));
			});
			return true;
		}

		// Если выбран стенд (parentId != null) — создаём стенд в той же подкатегории
		if (!string.IsNullOrEmpty(nodeType) && nodeType == "stands" && !string.IsNullOrEmpty(selectedParentId))
		{
			var initial = new Dictionary<string, object> { { "parentId", selectedParentId } };
			ctx.OpenAddModalWithData("stands", initial, (standData) => {
				if (!standData.ContainsKey("parentId")) standData["parentId"] = selectedParentId;
				ctx.StartCoroutine(ctx.Api().AddRecordWithResponse("stands", standData, (ok, created) => {
					if (ok) ctx.ReloadCurrentUI();
				}));
			});
			return true;
		}

		// Иначе — добавляем тип стенда (как было раньше)
		ctx.OpenAddModal("stands_types", (typeData) => {
			ctx.StartCoroutine(ctx.Api().AddRecordWithResponse("stands_types", typeData, (ok, created) => {
				if (ok) ctx.ReloadCurrentUI();
			}));
		});
		return true;
	}

	public bool HandleEdit(ActionsButtonsMainMenu ctx, Dictionary<string, object> data)
	{
		if (ctx == null) return false;
		var table = ctx.GetCurrentTable();
		if (table != "stands") return false;

		string nodeType = data != null && data.ContainsKey("nodeType") ? data["nodeType"].ToString() : null;
		string tableName = "stands"; // по умолчанию

		// Определяем таблицу для редактирования на основе типа узла
		if (nodeType == "stands_types")
		{
			tableName = "stands_types";
		}
		else if (nodeType == "stands_categories")
		{
			tableName = "stands";
		}
		else if (nodeType == "stands")
		{
			tableName = "stands";
		}

		ctx.OpenEditModal(tableName, data, (updatedData) => {
			ctx.StartCoroutine(ctx.Api().UpdateRecord(tableName, data["id"].ToString(), updatedData, (ok) => {
				if (ok) ctx.ReloadCurrentUI();
			}));
		});
		return true;
	}
}
