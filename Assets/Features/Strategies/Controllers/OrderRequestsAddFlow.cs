using System.Collections.Generic;

public class OrderRequestsAddFlow : IAddFlowStrategy
{
	public bool HandleAdd(ActionsButtonsMainMenu ctx)
	{
		if (ctx == null) return false;
		var table = ctx.GetCurrentTable();
		if (table != "order_requests") return false;

		ctx.OpenAddModal("order_requests", (data) => {
			// Белый список полей и нормализация значений перед отправкой
			var payload = new Dictionary<string, object>();
			AddIfPresent(payload, data, "title");
			AddIfPresent(payload, data, "comment");
			AddIfPresentNormalizedDate(payload, data, "requestDatetime");
			AddIfPresentNormalizedDate(payload, data, "executionDatetime");
			AddIfPresentInt(payload, data, "factoryId");
			AddIfPresentInt(payload, data, "standId");
			AddIfPresentInt(payload, data, "employeeCreatorId");

			ctx.StartCoroutine(ctx.Api().AddRecordWithResponse("order_requests", payload, (ok, created) => {
				if (ok) ctx.ReloadCurrentUI();
			}));
		});
		return true;
	}
	
	public bool HandleEdit(ActionsButtonsMainMenu ctx, Dictionary<string, object> data)
	{
		if (ctx == null) return false;
		var table = ctx.GetCurrentTable();
		if (table != "order_requests") return false;

		ctx.OpenEditModal("order_requests", data, (updatedData) => {
			ctx.StartCoroutine(ctx.Api().UpdateRecord("order_requests", data["id"].ToString(), updatedData, (ok) => {
				if (ok) ctx.ReloadCurrentUI();
			}));
		});
		return true;
	}

	private void AddIfPresent(Dictionary<string, object> dst, Dictionary<string, object> src, string key)
	{
		if (src != null && src.TryGetValue(key, out var val) && val != null)
			dst[key] = val;
	}

	private void AddIfPresentInt(Dictionary<string, object> dst, Dictionary<string, object> src, string key)
	{
		if (src == null || !src.TryGetValue(key, out var val) || val == null) return;
		if (val is int)
		{
			dst[key] = (int)val;
			return;
		}
		if (val is long)
		{
			dst[key] = (int)(long)val;
			return;
		}
		if (val is string s && int.TryParse(s, out var parsed))
		{
			dst[key] = parsed;
			return;
		}
		// Игнорируем некорректные значения, чтобы не сломать бэкенд
	}

	private void AddIfPresentNormalizedDate(Dictionary<string, object> dst, Dictionary<string, object> src, string key)
	{
		if (src == null || !src.TryGetValue(key, out var val) || val == null) return;
		if (val is System.DateTime dt)
		{
			dst[key] = dt.ToString("yyyy-MM-dd");
			return;
		}
		if (val is string s)
		{
			// Пытаемся распарсить и отдать нормализованный формат, иначе отправляем как есть
			if (System.DateTime.TryParse(s, out var parsed))
			{
				dst[key] = parsed.ToString("yyyy-MM-dd");
				return;
			}
			dst[key] = s;
			return;
		}
	}
}


