using System.Collections.Generic;

namespace UI.Selection
{
	/// <summary>
	/// Глобальное состояние выбора для модалок/кнопок действий.
	/// </summary>
	public static class GlobalSelectionState
	{
		/// <summary>
		/// Последняя выбранная строка (из списка/дерева). null, если нет.
		/// </summary>
		public static Dictionary<string, object> SelectedRow;

		/// <summary>
		/// Имя текущей таблицы (если применимо).
		/// </summary>
		public static string CurrentTableName;

		public static void Clear()
		{
			SelectedRow = null;
			CurrentTableName = null;
		}
	}
}


