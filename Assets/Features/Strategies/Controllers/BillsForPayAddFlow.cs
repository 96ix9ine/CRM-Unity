using System.Collections.Generic;

public class BillsForPayAddFlow : IAddFlowStrategy
{
	public bool HandleAdd(ActionsButtonsMainMenu ctx)
	{
		if (ctx == null) return false;
		var table = ctx.GetCurrentTable();
		if (table != "bills_for_pay") return false;

		ctx.OpenAddModal("bills_for_pay", (data) => {
			ctx.StartCoroutine(ctx.Api().AddRecordWithResponse("bills_for_pay", data, (ok, created) => {
				if (ok) ctx.ReloadCurrentUI();
			}));
		});
		return true;
	}
	
	public bool HandleEdit(ActionsButtonsMainMenu ctx, Dictionary<string, object> data)
	{
		if (ctx == null) return false;
		var table = ctx.GetCurrentTable();
		if (table != "bills_for_pay") return false;

		ctx.OpenEditModal("bills_for_pay", data, (updatedData) => {
			ctx.StartCoroutine(ctx.Api().UpdateRecord("bills_for_pay", data["id"].ToString(), updatedData, (ok) => {
				if (ok) ctx.ReloadCurrentUI();
			}));
		});
		return true;
	}
}


