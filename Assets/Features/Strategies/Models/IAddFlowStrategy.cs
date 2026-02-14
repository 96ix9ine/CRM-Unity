using System.Collections.Generic;

public interface IAddFlowStrategy
{
	bool HandleAdd(ActionsButtonsMainMenu ctx);
	bool HandleEdit(ActionsButtonsMainMenu ctx, Dictionary<string, object> data);
}


