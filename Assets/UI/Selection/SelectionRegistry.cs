using System;
using System.Collections.Generic;

namespace UI.Selection
{
	/// <summary>
	/// Простой реестр глобального сброса выделений.
	/// Любые компоненты, у которых есть логика снятия выделения, регистрируют колбэк.
	/// Глобальный клик по пустому месту вызывает ClearAll().
	/// </summary>
	public static class SelectionRegistry
	{
		private static readonly List<Action> deselectCallbacks = new List<Action>();

		public static void RegisterDeselect(Action callback)
		{
			if (callback == null) return;
			if (!deselectCallbacks.Contains(callback))
				deselectCallbacks.Add(callback);
		}

		public static void UnregisterDeselect(Action callback)
		{
			if (callback == null) return;
			deselectCallbacks.Remove(callback);
		}

		public static void ClearAll()
		{
			for (int i = 0; i < deselectCallbacks.Count; i++)
			{
				try { deselectCallbacks[i]?.Invoke(); } catch { }
			}
		}
	}
}


