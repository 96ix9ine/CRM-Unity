using System.Collections.Generic;

public class ComponentsAddFlow : IAddFlowStrategy
{
	public bool HandleAdd(ActionsButtonsMainMenu ctx)
	{
		if (ctx == null) return false;
		var table = ctx.GetCurrentTable();
		if (table != "components") return false;

		var selected = ctx.GetEffectiveSelectedRow();

		// Проверки на null для всех обращений к selected
		if (selected == null)
		{
			// Если нет выделения - создаем корневую категорию
			return CreateRootCategory(ctx);
		}

		string nodeType = selected.ContainsKey("nodeType") ? selected["nodeType"]?.ToString() : null;
		string selectedId = selected.ContainsKey("id") ? selected["id"]?.ToString() : null;
		string selectedParentId = selected.ContainsKey("parentId") ? selected["parentId"]?.ToString() : null;

		// Если выбрана категория (components_categories) — создаём компонент или подкатегорию в зависимости от данных
		if (!string.IsNullOrEmpty(nodeType) && nodeType == "components_categories" && !string.IsNullOrEmpty(selectedId))
		{
			var categoryInitial = new Dictionary<string, object> { { "parentId", selectedId } };
			ctx.OpenAddModalWithData("components", categoryInitial, (data) => {
				if (!data.ContainsKey("parentId")) data["parentId"] = selectedId;

				// Проверяем, что это компонент (заполнены хотя бы некоторые поля компонента)
				bool isComponent = data.ContainsKey("width") && data["width"] != null ||
								  data.ContainsKey("height") && data["height"] != null ||
								  data.ContainsKey("thickness") && data["thickness"] != null ||
								  data.ContainsKey("weight") && data["weight"] != null ||
								  data.ContainsKey("material") && data["material"] != null ||
								  data.ContainsKey("placementId") && data["placementId"] != null;

				// Если это не компонент (только название), то это будет подкатегория
				if (!isComponent)
				{
					// Очищаем поля компонента для подкатегории
					data["width"] = null;
					data["height"] = null;
					data["thickness"] = null;
					data["weight"] = null;
					data["material"] = null;
					data["receiptDate"] = null;
					data["drawingReference"] = null;
					data["placementId"] = null;
				}

				ctx.StartCoroutine(ctx.Api().AddRecordWithResponse("components", data, (ok, created) => {
					if (ok) ctx.ReloadCurrentUI();
				}));
			});
			return true;
		}

		// Если выбран компонент (parentId != null) — создаём компонент в той же категории
		if (!string.IsNullOrEmpty(nodeType) && nodeType == "components" && !string.IsNullOrEmpty(selectedParentId))
		{
			var sameCategoryInitial = new Dictionary<string, object> { { "parentId", selectedParentId } };
			ctx.OpenAddModalWithData("components", sameCategoryInitial, (data) => {
				if (!data.ContainsKey("parentId")) data["parentId"] = selectedParentId;
				ctx.StartCoroutine(ctx.Api().AddRecordWithResponse("components", data, (ok, created) => {
					if (ok) ctx.ReloadCurrentUI();
				}));
			});
			return true;
		}

		// Иначе (нет nodeType/список/корень) — создаем корневую категорию
		return CreateRootCategory(ctx);
	}

	/// <summary>
	/// Создает корневую категорию компонентов
	/// </summary>
	private bool CreateRootCategory(ActionsButtonsMainMenu ctx)
	{
		// Для корневой категории parentId должен быть null
		var rootCategoryInitial = new Dictionary<string, object> { { "parentId", null } };
		ctx.OpenAddModalWithData("components", rootCategoryInitial, (data) => {
			// Убеждаемся, что для категории установлены только необходимые поля
			if (!data.ContainsKey("parentId"))
				data["parentId"] = null;

			// Очищаем поля компонента, если создается категория
			if (data.ContainsKey("width")) data["width"] = null;
			if (data.ContainsKey("height")) data["height"] = null;
			if (data.ContainsKey("thickness")) data["thickness"] = null;
			if (data.ContainsKey("weight")) data["weight"] = null;
			if (data.ContainsKey("material")) data["material"] = null;
			if (data.ContainsKey("receiptDate")) data["receiptDate"] = null;
			if (data.ContainsKey("drawingReference")) data["drawingReference"] = null;
			if (data.ContainsKey("placementId")) data["placementId"] = null;

			ctx.StartCoroutine(ctx.Api().AddRecordWithResponse("components", data, (ok, created) => {
				if (ok) ctx.ReloadCurrentUI();
			}));
		});
		return true;
	}

	/// <summary>
	/// Создает подкатегорию компонентов
	/// </summary>
	private bool CreateSubcategory(ActionsButtonsMainMenu ctx, string parentId)
	{
		var subcategoryInitial = new Dictionary<string, object> { { "parentId", parentId } };
		ctx.OpenAddModalWithData("components", subcategoryInitial, (data) => {
			if (!data.ContainsKey("parentId"))
				data["parentId"] = parentId;

			// Очищаем поля компонента, если создается подкатегория
			if (data.ContainsKey("width")) data["width"] = null;
			if (data.ContainsKey("height")) data["height"] = null;
			if (data.ContainsKey("thickness")) data["thickness"] = null;
			if (data.ContainsKey("weight")) data["weight"] = null;
			if (data.ContainsKey("material")) data["material"] = null;
			if (data.ContainsKey("receiptDate")) data["receiptDate"] = null;
			if (data.ContainsKey("drawingReference")) data["drawingReference"] = null;
			if (data.ContainsKey("placementId")) data["placementId"] = null;

			ctx.StartCoroutine(ctx.Api().AddRecordWithResponse("components", data, (ok, created) => {
				if (ok) ctx.ReloadCurrentUI();
			}));
		});
		return true;
	}

	public bool HandleEdit(ActionsButtonsMainMenu ctx, Dictionary<string, object> data)
	{
		if (ctx == null) return false;
		var table = ctx.GetCurrentTable();
		if (table != "components") return false;

		ctx.OpenEditModal("components", data, (updatedData) => {
			ctx.StartCoroutine(ctx.Api().UpdateRecord("components", data["id"].ToString(), updatedData, (ok) => {
				if (ok) ctx.ReloadCurrentUI();
			}));
		});
		return true;
	}
}


