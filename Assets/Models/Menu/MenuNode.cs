using System.Collections.Generic;
using UnityEngine;

namespace Models.Menu
{
	/// <summary>
	/// Узел дерева меню. Лист — это кнопка, открывающая таблицу. Узел с детьми — подпункт.
	/// </summary>
	[System.Serializable]
	public class MenuNode
	{
		#region Serialized Fields
		[SerializeField] private string m_DisplayName;
		[SerializeField] private string m_TableName; // null или пусто — означает подпункт
		[SerializeField] private List<MenuNode> m_Children = new List<MenuNode>();
		#endregion

		#region Public Properties
		public string DisplayName => m_DisplayName;
		public string TableName => m_TableName;
		public IReadOnlyList<MenuNode> Children => m_Children;
		public bool IsLeaf => !string.IsNullOrEmpty(m_TableName);
		#endregion

		#region Constructors
		public MenuNode(string _displayName, string _tableName = null, List<MenuNode> _children = null)
		{
			m_DisplayName = _displayName;
			m_TableName = _tableName;
			m_Children = _children ?? new List<MenuNode>();
		}
		#endregion
	}
}


