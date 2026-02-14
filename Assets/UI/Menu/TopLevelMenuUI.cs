using System;
using System.Collections.Generic;
using Controllers.Menu;
using Models.Menu;
using Services.Menu;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UI.Menu
{
	/// <summary>
	/// UI-обвязка: 8 верхнеуровневых кнопок. При выборе раздела показывает его дочерние элементы
	/// на том же наборе кнопок (подменяет текст и коллбеки). Лист вызывает SelectTable.

	/// Требования по сцене:
	/// - m_MenuButtons: ссылка на существующий MenuButtons (для SelectTable)
	/// - m_ButtonPrefab: префаб кнопки подпунктов
	/// - m_ButtonsParent: контейнер для набора кнопок подпунктов
	/// - m_BackButton: кнопка "Назад"
	/// </summary>
	public class TopLevelMenuUI : MonoBehaviour
	{
		#region Serialized Fields
		[SerializeField] private MenuButtons m_MenuButtons;
		[SerializeField] private Transform m_ButtonsParent;
		[SerializeField] private GameObject m_ButtonPrefab;
		[SerializeField] private Button m_BackButton;
		#endregion

		#region Private Fields
		[SerializeField] private MenuDefinitionService m_DefinitionService;
		[SerializeField] private MenuNavigationController m_Navigation;
		private readonly List<GameObject> m_Spawned = new List<GameObject>();
		#endregion

		#region Unity Lifecycle
		private void Awake()
		{
			if (m_DefinitionService == null) m_DefinitionService = gameObject.AddComponent<MenuDefinitionService>();
			if (m_Navigation == null) m_Navigation = gameObject.AddComponent<MenuNavigationController>();
		}

		private void Start()
		{
			var root = m_DefinitionService.BuildRoot();
			m_Navigation.SetRoot(root);
            // Не рендерим сразу — показываем подпункты только после клика по разделу
            if (m_ButtonsParent != null) m_ButtonsParent.gameObject.SetActive(false);
			if (m_BackButton != null)
			{
				m_BackButton.onClick.RemoveAllListeners();
				m_BackButton.onClick.AddListener(OnBackClicked);
                m_BackButton.gameObject.SetActive(false);
            }
		}
		#endregion

		#region Rendering
		private void ClearButtons()
		{
			for (int i = 0; i < m_Spawned.Count; i++)
			{
				if (m_Spawned[i] != null)
					Destroy(m_Spawned[i]);
			}
			m_Spawned.Clear();
		}

		private void RenderCurrent()
		{
			ClearButtons();
			var current = m_Navigation.GetCurrent();
			if (current == null) return;
			// Если текущий — ROOT, показываем 8 разделов (его дети)
			var nodes = current.Children;
			for (int i = 0; i < nodes.Count; i++)
			{
				CreateButtonFor(nodes[i]);
			}
			if (m_BackButton != null)
				m_BackButton.gameObject.SetActive(m_Navigation.CanGoBack());
            if (m_ButtonsParent != null)
                m_ButtonsParent.gameObject.SetActive(true);

            // Принудительно пересобираем лэйаут, чтобы кнопки не налезали друг на друга
            if (m_ButtonsParent != null)
            {
                Canvas.ForceUpdateCanvases();
                var rt = m_ButtonsParent as RectTransform;
                if (rt != null)
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rt);
                Canvas.ForceUpdateCanvases();
            }
		}

		private void CreateButtonFor(MenuNode _node)
		{
            var go = Instantiate(m_ButtonPrefab, m_ButtonsParent);
			go.SetActive(true);
			var btn = go.GetComponent<Button>();
            // Поддержка Text и TextMeshProUGUI
            var txt = go.GetComponentInChildren<Text>();
            if (txt != null) txt.text = _node.DisplayName;
            var tmp = go.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp != null) tmp.text = _node.DisplayName;

            // Нормализуем трансформ для лэйаута
            var rt = go.transform as RectTransform;
            if (rt != null)
            {
                rt.anchoredPosition3D = Vector3.zero;
                rt.localScale = Vector3.one;
            }
			btn.onClick.RemoveAllListeners();
            if (_node.IsLeaf)
			{
                btn.onClick.AddListener(() =>
                {
                    m_MenuButtons.SelectTable(_node.TableName);
                    // Лист — скрываем список подпунктов
                    if (m_ButtonsParent != null) m_ButtonsParent.gameObject.SetActive(false);
                    if (m_BackButton != null) m_BackButton.gameObject.SetActive(false);
                    ClearButtons();
                });
			}
			else
			{
                btn.onClick.AddListener(() => { m_Navigation.EnterChild(_node); RenderCurrent(); });
			}
			m_Spawned.Add(go);
		}
		#endregion

		#region Actions
		private void OnBackClicked()
		{
			m_Navigation.GoBack();
            if (m_Navigation.IsAtRoot())
            {
                ClearButtons();
                if (m_ButtonsParent != null) m_ButtonsParent.gameObject.SetActive(false);
                if (m_BackButton != null) m_BackButton.gameObject.SetActive(false);
            }
            else
            {
                RenderCurrent();
            }
		}
		#endregion

		#region Public API
		// Вызывай эти методы с верхних 8 кнопок
		public void OpenSection(int _index)
		{
			EnsureAtRoot();
			var root = m_Navigation.GetCurrent();
			if (root == null) return;
			if (_index < 0 || _index >= root.Children.Count) return;
			var node = (MenuNode)root.Children[_index];
			// Особый кейс: Лицензии — сразу открываем таблицу и не показываем список
			if (node.IsLeaf && node.TableName == "license")
			{
				m_MenuButtons.SelectTable("license");
				if (m_ButtonsParent != null) m_ButtonsParent.gameObject.SetActive(false);
				if (m_BackButton != null) m_BackButton.gameObject.SetActive(false);
				ClearButtons();
				return;
			}
			m_Navigation.EnterChild(node);
			RenderCurrent();
		}

		public void CloseSection()
		{
			OnBackClicked();
		}
		#endregion

		#region Helpers
		private void EnsureAtRoot()
		{
			while (m_Navigation.CanGoBack())
				m_Navigation.GoBack();
		}
		#endregion
	}
}


