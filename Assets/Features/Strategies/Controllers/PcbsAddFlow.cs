using System.Collections.Generic;
using UnityEngine;

public class PcbsAddFlow : IAddFlowStrategy
{
	public bool HandleAdd(ActionsButtonsMainMenu ctx)
	{
		if (ctx == null) return false;
		var table = ctx.GetCurrentTable();
		if (table != "pcbs") return false;

		var selected = ctx.GetEffectiveSelectedRow();
		string nodeType = selected != null && selected.ContainsKey("nodeType") ? selected["nodeType"].ToString() : null;
		string selectedId = selected != null && selected.ContainsKey("id") ? selected["id"].ToString() : null;
		string selectedParentId = selected != null && selected.ContainsKey("parentId") ? selected["parentId"].ToString() : null;

		// Если выбрана подкатегория (parentId != null) — создаём плату
		if (!string.IsNullOrEmpty(nodeType) && nodeType == "pcbs_subcategories" && !string.IsNullOrEmpty(selectedId))
		{
			var initial = new Dictionary<string, object> { { "parentId", selectedId } };
			ctx.OpenAddModalWithData("pcbs", initial, (pcbData) => {
				if (!pcbData.ContainsKey("parentId")) pcbData["parentId"] = selectedId;
				ctx.StartCoroutine(ctx.Api().AddRecordWithResponse("pcbs", pcbData, (ok, created) => {
					if (ok) ctx.ReloadCurrentUI();
				}));
			});
			return true;
		}

		// Если выбрана плата (parentId != null) — создаём плату в той же подкатегории
		if (!string.IsNullOrEmpty(nodeType) && nodeType == "pcbs" && !string.IsNullOrEmpty(selectedParentId))
		{
			var initial = new Dictionary<string, object> { { "parentId", selectedParentId } };
			ctx.OpenAddModalWithData("pcbs", initial, (pcbData) => {
				if (!pcbData.ContainsKey("parentId")) pcbData["parentId"] = selectedParentId;
				ctx.StartCoroutine(ctx.Api().AddRecordWithResponse("pcbs", pcbData, (ok, created) => {
					if (ok) ctx.ReloadCurrentUI();
				}));
			});
			return true;
		}

		// Если выбрана корневая категория (parentId == null или отсутствует) — создаём подкатегорию
		if (!string.IsNullOrEmpty(nodeType) && nodeType == "pcbs_categories" && !string.IsNullOrEmpty(selectedId) && (string.IsNullOrEmpty(selectedParentId) || !selected.ContainsKey("parentId")))
		{
			var initial = new Dictionary<string, object> { { "parentId", selectedId } };
			ctx.OpenAddModalWithData("pcbs", initial, (subData) => {
				if (!subData.ContainsKey("parentId")) subData["parentId"] = selectedId;
				ctx.StartCoroutine(ctx.Api().AddRecordWithResponse("pcbs", subData, (ok, created) => {
					if (ok) ctx.ReloadCurrentUI();
				}));
			});
			return true;
		}

		// Иначе — создаём корневую категорию (parentId = null)
		var rootInitial = new Dictionary<string, object> { { "parentId", null } };
		ctx.OpenAddModalWithData("pcbs", rootInitial, (catData) => {
			if (!catData.ContainsKey("parentId")) catData["parentId"] = null;
			ctx.StartCoroutine(ctx.Api().AddRecordWithResponse("pcbs", catData, (ok, created) => {
				if (ok) ctx.ReloadCurrentUI();
			}));
		});
		return true;
	}

	public bool HandleEdit(ActionsButtonsMainMenu ctx, Dictionary<string, object> data)
	{
		if (ctx == null) return false;
		var table = ctx.GetCurrentTable();
		if (table != "pcbs") return false;

		// Определяем реальную таблицу для редактирования по nodeType
		string nodeType = data.ContainsKey("nodeType") ? data["nodeType"].ToString() : null;
		string editTable = "pcbs"; // по умолчанию для pcbs

		switch (nodeType)
		{
			case "pcbs_components":
				editTable = "pcbs_components";
				break;
			case "pcbs_categories":
			case "pcbs_subcategories":
			case "pcbs":
				editTable = "pcbs"; // все эти типы хранятся в таблице pcbs
				break;
			default:
				editTable = "pcbs"; // если nodeType неизвестен, используем pcbs
				break;
		}

		ctx.OpenEditModal(editTable, data, (updatedData) => {
			ctx.StartCoroutine(ctx.Api().UpdateRecord(editTable, data["id"].ToString(), updatedData, (ok) => {
				if (ok) ctx.ReloadCurrentUI();
			}));
		});
		return true;
	}

	/// <summary>
	/// Обработчик добавления печатной платы (ПП)
	/// Открывает модальное окно для добавления новой печатной платы
	/// </summary>
	public bool HandleAddPcb(ActionsButtonsMainMenu ctx)
	{
		if (ctx == null) return false;
		var table = ctx.GetCurrentTable();
		if (table != "pcbs") return false;

		var initial = new Dictionary<string, object>();
		var effectiveRow = ctx.GetEffectiveSelectedRow();

		// Если есть выбранная строка, используем её как родителя
		if (effectiveRow != null && effectiveRow.ContainsKey("id"))
		{
			string nodeType = effectiveRow.ContainsKey("nodeType") ? effectiveRow["nodeType"].ToString() : null;
			string parentId = null;

			// Определяем parentId в зависимости от типа выбранного узла
			if (nodeType == "pcbs_subcategories")
			{
				parentId = effectiveRow["id"].ToString();
			}
			else if (nodeType == "pcbs" && effectiveRow.ContainsKey("parentId"))
			{
				parentId = effectiveRow["parentId"].ToString();
			}
			else if (nodeType == "pcbs_categories" && (!effectiveRow.ContainsKey("parentId") || string.IsNullOrEmpty(effectiveRow["parentId"]?.ToString())))
			{
				parentId = effectiveRow["id"].ToString();
			}

			if (!string.IsNullOrEmpty(parentId))
			{
				initial["parentId"] = parentId;
			}
		}

		ctx.OpenAddModalWithData("pcbs", initial, (pcbData) => {
			if (!pcbData.ContainsKey("parentId") && initial.ContainsKey("parentId"))
			{
				pcbData["parentId"] = initial["parentId"];
			}
			ctx.StartCoroutine(ctx.Api().AddRecordWithResponse("pcbs", pcbData, (ok, created) => {
				if (ok) ctx.ReloadCurrentUI();
			}));
		});
		return true;
	}

	/// <summary>
	/// Обработчик добавления компонента к печатной плате
	/// Добавляет компонент к выбранной печатной плате через pcbs_components
	/// </summary>
	public bool HandleAddComponent(ActionsButtonsMainMenu ctx)
	{
		if (ctx == null) return false;
		var table = ctx.GetCurrentTable();
		if (table != "pcbs") return false;

		var effectiveRow = ctx.GetEffectiveSelectedRow();

		// Проверяем, что выбрана печатная плата (nodeType == "pcbs")
		if (effectiveRow == null)
		{
			Debug.LogWarning("Для добавления компонента необходимо выбрать печатную плату");
			return false;
		}

		string nodeType = effectiveRow.ContainsKey("nodeType") ? effectiveRow["nodeType"].ToString() : null;
		if (nodeType != "pcbs")
		{
			Debug.LogWarning("Компоненты можно добавлять только к печатным платам");
			return false;
		}

		string pcbId = effectiveRow.ContainsKey("id") ? effectiveRow["id"].ToString() : null;
		if (string.IsNullOrEmpty(pcbId))
		{
			Debug.LogWarning("Не удалось получить ID печатной платы");
			return false;
		}

		// Открываем модальное окно для добавления pcbs_components с предустановленным pcbId
		var initial = new Dictionary<string, object> { { "pcbId", pcbId } };
		ctx.OpenAddModalWithData("pcbs_components", initial, (componentData) => {
			if (!componentData.ContainsKey("pcbId"))
			{
				componentData["pcbId"] = pcbId;
			}
			ctx.StartCoroutine(ctx.Api().AddRecordWithResponse("pcbs_components", componentData, (ok, created) => {
				if (ok) ctx.ReloadCurrentUI();
			}));
		});
		return true;
	}
}


