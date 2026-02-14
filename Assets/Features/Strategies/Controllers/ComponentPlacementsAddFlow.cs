using System.Collections.Generic;

public class ComponentPlacementsAddFlow : IAddFlowStrategy
{
	public bool HandleAdd(ActionsButtonsMainMenu ctx)
	{
		if (ctx == null) return false;
		var table = ctx.GetCurrentTable();
		if (table != "component_placements") return false;

		var selected = ctx.GetEffectiveSelectedRow();
		// Если не выбран тип — сначала добавляем component_placement_type
		if (selected == null || !selected.ContainsKey("nodeType") || selected["nodeType"].ToString() != "component_placement_types")
		{
			ctx.OpenAddModal("component_placement_type", (typeData) => {
				ctx.StartCoroutine(ctx.Api().AddRecordWithResponse("component_placement_type", typeData, (ok, createdType) => {
					if (ok) ctx.ReloadCurrentUI();
				}));
			});
			return true;
		}

		// Выбран тип — открываем форму component_placements c префиллом placementTypeId
		var initial = new Dictionary<string, object>();
		if (selected.ContainsKey("id"))
		{
			var typeId = selected["id"].ToString();
			initial["placementTypeId"] = typeId;
		}
		ctx.OpenAddModalWithData("component_placements", initial, (placementData) => {
			// Гарантируем placementTypeId перед отправкой
			if (!placementData.ContainsKey("placementTypeId") && selected.ContainsKey("id"))
				placementData["placementTypeId"] = selected["id"].ToString();

			ctx.StartCoroutine(ctx.Api().AddRecordWithResponse("component_placements", placementData, (okPlacement, createdPlacement) => {
				if (okPlacement) ctx.ReloadCurrentUI();
			}));
		});
		return true;
	}

	public bool HandleEdit(ActionsButtonsMainMenu ctx, Dictionary<string, object> data)
	{
		if (ctx == null) return false;
		var currentTable = ctx.GetCurrentTable();
		if (currentTable != "component_placements") return false;

		// Роутинг по nodeType: позволяем редактировать и тип размещения, и само размещение
		string nodeType = data != null && data.ContainsKey("nodeType") ? data["nodeType"].ToString() : null;
		string tableName = "component_placements";
		if (!string.IsNullOrEmpty(nodeType))
		{
			if (nodeType == "component_placement_types") tableName = "component_placement_type";
			else if (nodeType == "component_placements") tableName = "component_placements";
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