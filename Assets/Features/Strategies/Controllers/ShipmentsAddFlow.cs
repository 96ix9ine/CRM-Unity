using System.Collections.Generic;

public class ShipmentsAddFlow : IAddFlowStrategy
{
	public bool HandleAdd(ActionsButtonsMainMenu ctx)
	{
		if (ctx == null) return false;
		var table = ctx.GetCurrentTable();
		if (table != "shipments") return false;

		// Открываем модалку по кастомной стратегии (с полем стенда)
		var strategy = new ShipmentsModalStrategy(ctx);
		ctx.OpenModalWithStrategy(strategy, false, null);
		return true;
	}

	public bool HandleEdit(ActionsButtonsMainMenu ctx, Dictionary<string, object> data)
	{
		if (ctx == null) return false;
		var table = ctx.GetCurrentTable();
		if (table != "shipments") return false;

		// Если выбран узел стенда (дочерний узел в дереве отгрузок) — открываем стандартную модалку стенда
		if (data != null && data.ContainsKey("nodeType") && data["nodeType"] != null && data["nodeType"].ToString() == "stands")
		{
			ctx.OpenEditModal("stands", data, (updated) => {
				if (data.ContainsKey("id"))
				{
					ctx.StartCoroutine(ctx.Api().UpdateRecord("stands", data["id"], updated, (ok) => {
						ctx.ReloadCurrentUI();
					}));
				}
			});
			return true;
		}

		// Редактирование самой отгрузки — загружаем полную строку и передаём в стратегию
		if (data != null && data.ContainsKey("id") && data["id"] != null)
		{
			string id = data["id"].ToString();
			ctx.StartCoroutine(ctx.Api().GetRowById("shipments", id, (fullRow) => {
				var payload = fullRow ?? data;
				var strategy = new ShipmentsModalStrategy(ctx);
				ctx.OpenModalWithStrategy(strategy, true, payload);
			}));
		}
		else
		{
			var strategy = new ShipmentsModalStrategy(ctx);
			ctx.OpenModalWithStrategy(strategy, true, data);
		}
		return true;
	}
}
