using UnityEngine;
using UnityEngine.EventSystems;
using Core.TreeView.Controllers;
using UI.Selection;

namespace UI.TreeView.Views
{
	/// <summary>
	/// Вешается на фон/контент ScrollRect дерева. При клике по пустому месту снимает выделение.
	/// Чтобы событие приходило сюда, у Image/Graphic должен быть включен Raycast Target,
	/// а элементы списка перекрывают этот фон и получат клик первыми.
	/// </summary>
	public class TreeViewDeselectOnBackgroundClick : MonoBehaviour, IPointerClickHandler
	{
        [SerializeField] private TreeViewController databaseTreeViewController; // для DatabaseTreeView

		public void OnPointerClick(PointerEventData eventData)
		{
			// Если клик пришёл сюда, значит не попали по элементу (Button) — снимаем выделение
			if (databaseTreeViewController != null)
			{
				databaseTreeViewController.DeselectAllNodes();
				databaseTreeViewController.Refresh();
			}
			SelectionRegistry.ClearAll();
		}
	}
}


