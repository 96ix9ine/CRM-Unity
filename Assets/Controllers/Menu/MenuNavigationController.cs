using System.Collections.Generic;
using Models.Menu;
using UnityEngine;

namespace Controllers.Menu
{
	/// <summary>
	/// Управляет навигацией по узлам меню (история/стек и текущее состояние).
	/// </summary>
	public class MenuNavigationController : MonoBehaviour
	{
		[System.NonSerialized]
		private MenuNode m_Root;
		private readonly Stack<MenuNode> m_Stack = new Stack<MenuNode>();

		public void SetRoot(MenuNode _root)
		{
			m_Root = _root;
			m_Stack.Clear();
		}

		public MenuNode GetCurrent()
		{
			if (m_Root == null) return null;
			return m_Stack.Count == 0 ? m_Root : m_Stack.Peek();
		}

		public void EnterChild(MenuNode _child)
		{
			m_Stack.Push(_child);
		}

		public bool CanGoBack()
		{
			return m_Stack.Count > 0;
		}

		public void GoBack()
		{
			if (m_Stack.Count > 0)
				m_Stack.Pop();
		}

		public bool IsAtRoot()
		{
			return m_Stack.Count == 0;
		}
	}
}


