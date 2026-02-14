using System;
using System.Collections.Generic;
using System.Linq;
using Core.TreeView.Interfaces;
using Core.TreeView.Models;
using Core.TreeView.Components;
using UnityEngine;

namespace Core.TreeView.Controllers
{
    /// <summary>
    /// Контроллер для управления отображением и пагинацией дерева (TreeView).
    /// Поддерживает рекурсивное создание элементов, работу с провайдером и синхронизацию с PaginationListItems.
    /// 
    /// СВЯЗИ С ДРУГИМИ КОМПОНЕНТАМИ:
    /// - ITreeViewProvider: получает данные для отображения
    /// - TreeViewItem: создает и управляет элементами дерева
    /// - PaginationListItems: синхронизирует пагинацию
    /// - DatabaseTreeView: уведомляет о выборе узлов
    /// 
    /// ЛОГИКА РАБОТЫ:
    /// - Создает элементы дерева рекурсивно
    /// - Управляет пагинацией корневых узлов
    /// - Обрабатывает события выбора и разворачивания
    /// - Синхронизируется с провайдером данных
    /// </summary>
    public class TreeViewController : MonoBehaviour
    {
        [SerializeField] private Transform contentParent;    // Контейнер для элементов дерева
        [SerializeField] private GameObject itemPrefab;      // Префаб элемента дерева
        
        private ITreeViewProvider _provider;                // Провайдер данных
        private List<ITreeNode> _visibleNodes;              // Список видимых узлов
        private Dictionary<int, GameObject> _itemInstances; // Словарь созданных элементов
        
        private ITreeNode customRoot = null;
        public void SetCustomRoot(ITreeNode root)
        {
            customRoot = root;
        }

        /// <summary>
        /// Событие выбора узла
        /// Вызывается при выборе элемента дерева
        /// </summary>
        public event Action<ITreeNode> OnNodeSelected;
        
        /// <summary>
        /// Событие разворачивания узла
        /// Вызывается при разворачивании элемента
        /// </summary>
        public event Action<ITreeNode> OnNodeExpanded;
        
        /// <summary>
        /// Событие сворачивания узла
        /// Вызывается при сворачивании элемента
        /// </summary>
        public event Action<ITreeNode> OnNodeCollapsed;

        /// <summary>
        /// Текущая страница для пагинации корневых узлов
        /// Используется для отображения части узлов при большом количестве
        /// </summary>
        private int currentPage = 0;
        
        /// <summary>
        /// Количество элементов на странице
        /// Определяет сколько корневых узлов отображается одновременно
        /// </summary>
        private int itemsPerPage = 20;

        // --- ДОБАВЛЕНО: ссылка на PaginationListItems для синхронизации пагинации ---
        private PaginationListItems paginationListItems;
        // --- END ---

        private List<ITreeNode> lastPagedNodes = null; // защита от лишних вызовов

        /// <summary>
        /// Инициализация компонента
        /// Создает коллекции для управления элементами
        /// </summary>
        private void Awake()
        {
            _itemInstances = new Dictionary<int, GameObject>();
            _visibleNodes = new List<ITreeNode>();
        }

        /// <summary>
        /// Инициализирует контроллер с указанным провайдером данных
        /// Основной метод инициализации контроллера
        /// 
        /// ПАРАМЕТРЫ:
        /// - provider: провайдер данных для дерева
        /// 
        /// ЛОГИКА:
        /// - Сохраняет ссылку на провайдер
        /// - Подписывается на события загрузки данных
        /// - Выполняет первоначальное обновление
        /// </summary>
        public void Initialize(ITreeViewProvider provider)
        {
            _provider = provider;
            if (_provider != null)
            {
                _provider.OnDataLoaded += Refresh; // Подписываемся на событие загрузки данных
                Refresh(); // Изначальное обновление при инициализации
                // Синхронизируем пагинацию с UI
                if (paginationListItems != null)
                {
                    var dropdown = typeof(PaginationListItems).GetField("dropdown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(paginationListItems) as TMPro.TMP_Dropdown;
                    if (dropdown != null)
                        paginationListItems.GetType().GetMethod("OnDropdownChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(paginationListItems, new object[] { dropdown.value });
                }
            }
        }

        /// <summary>
        /// Устанавливает параметры пагинации
        /// Используется для управления отображением корневых узлов
        /// 
        /// ПАРАМЕТРЫ:
        /// - page: номер страницы (начиная с 0)
        /// - perPage: количество элементов на странице
        /// </summary>
        public void SetPagination(int page, int perPage)
        {
            currentPage = Mathf.Max(0, page);
            itemsPerPage = Mathf.Max(1, perPage);
            Refresh();
        }

        /// <summary>
        /// Получает информацию о пагинации
        /// Используется PaginationListItems для синхронизации
        /// 
        /// ВОЗВРАЩАЕТ:
        /// - Кортеж с текущей страницей, размером страницы и общим количеством
        /// </summary>
        public (int page, int perPage, int total) GetPaginationInfo()
        {
            int total = _provider != null ? _provider.GetVisibleNodesCount() : 0;
            return (currentPage, itemsPerPage, total);
        }

        /// <summary>
        /// Получает провайдер данных
        /// Используется для доступа к провайдеру из других компонентов
        /// 
        /// ВОЗВРАЩАЕТ:
        /// - Текущий провайдер данных или null
        /// </summary>
        public ITreeViewProvider GetProvider()
        {
            return _provider;
        }

        /// <summary>
        /// Обновляет отображение дерева
        /// Основной метод для перерисовки дерева
        /// 
        /// ЛОГИКА:
        /// - Отключает исходный TreeViewItem, если он есть
        /// - Очищаем старые элементы
        /// - Получаем нужную страницу видимых узлов
        /// - Создаем элементы рекурсивно
        /// </summary>
        public void Refresh()
        {
            if (_provider == null && customRoot == null) return;

            // Проверяем, что itemPrefab не уничтожен
            if (itemPrefab == null)
            {
                Debug.LogError("TreeViewController: itemPrefab is null, cannot refresh");
                return;
            }

            // Удаляем все дочерние элементы из contentParent
            for (int i = contentParent.childCount - 1; i >= 0; i--)
            {
                var child = contentParent.GetChild(i);
                if (child != null)
                    DestroyImmediate(child.gameObject);
            }

            // Очищаем старые элементы
            foreach (var go in _itemInstances.Values)
            {
                if (go != null)
                    Destroy(go);
            }
            _itemInstances.Clear();

            // Получаем только нужную страницу видимых узлов
            List<ITreeNode> roots;
            if (customRoot != null)
                roots = new List<ITreeNode> { customRoot };
            else
                roots = _provider.GetVisibleNodesPage(currentPage * itemsPerPage, itemsPerPage);
            foreach (var root in roots)
                CreateItemRecursive(root, contentParent);

            // После обновления дерева синхронизируем пагинацию
            if (paginationListItems != null)
            {
                var dropdown = typeof(PaginationListItems).GetField("dropdown", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.GetValue(paginationListItems) as TMPro.TMP_Dropdown;
                if (dropdown != null)
                    paginationListItems.GetType().GetMethod("OnDropdownChanged", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.Invoke(paginationListItems, new object[] { dropdown.value });
            }
        }

        /// <summary>
        /// Рекурсивно создаёт элементы дерева с учетом пагинации для FieldTreeNode
        /// Основной метод создания элементов дерева
        /// 
        /// ПАРАМЕТРЫ:
        /// - node: узел для создания элемента
        /// - parent: родительский Transform для размещения
        /// 
        /// ЛОГИКА:
        /// - Создает GameObject из префаба
        /// - Инициализирует TreeViewItem с узлом
        /// - Подписывается на события элемента
        /// - Рекурсивно создает дочерние элементы
        /// - Для FieldTreeNode использует пагинацию значений
        /// </summary>
        private void CreateItemRecursive(ITreeNode node, Transform parent)
        {
            // Проверяем, что itemPrefab не уничтожен
            if (itemPrefab == null)
            {
                Debug.LogError("TreeViewController: itemPrefab is null in CreateItemRecursive");
                return;
            }

            // Если parent (например, ChildrenContainer) неактивен — активируем его временно
            bool wasInactive = !parent.gameObject.activeSelf;
            if (wasInactive)
                parent.gameObject.SetActive(true);

            var itemObj = Instantiate(itemPrefab, parent);
            // Debug.LogError($"[TreeViewController] Instantiated: {itemObj.name}");
            itemObj.SetActive(true); // Явно активируем
            var item = itemObj.GetComponent<TreeViewItem>();
            if (item != null)
            {
                item.Initialize(node);
                item.OnSelected += HandleItemSelected;
                item.OnExpanded += HandleItemExpanded;
                item.OnCollapsed += HandleItemCollapsed;
            }
            _itemInstances[node.Id] = itemObj;

            // Рекурсивно создаём дочерние элементы, если узел развернут
            if (node.IsExpanded && node.Children != null && node.Children.Count > 0)
            {
                var childrenContainer = (item != null && item.ChildrenContainer != null) ? item.ChildrenContainer : itemObj.transform;
                // Пагинация только для групп (у которых есть дети)
                if (paginationListItems != null && node.Children.Any(child => child.Children != null && child.Children.Count > 0))
                {
                    paginationListItems.SetPagedNodesOnLevel(node.Children.Where(child => child.Children != null && child.Children.Count > 0).ToList());
                }
                var childrenToShow = node.GetPageChildren();
                // Debug.Log($"[TreeView] CreateItemRecursive: node={node.Name} childrenToShow={childrenToShow.Count} (Children={node.Children?.Count})");
                foreach (var child in childrenToShow)
                    CreateItemRecursive(child, childrenContainer);
                for (int i = 0; i < childrenContainer.childCount; i++)
                    childrenContainer.GetChild(i).gameObject.SetActive(true);
            }
            // Возвращаем контейнер в исходное состояние
            if (wasInactive)
                parent.gameObject.SetActive(false);
            
        }

        /// <summary>
        /// Обработчик выбора элемента
        /// Вызывается при выборе элемента дерева
        /// 
        /// ПАРАМЕТРЫ:
        /// - node: выбранный узел
        /// 
        /// ЛОГИКА:
        /// - Выбирает узел в провайдере
        /// - Уведомляет подписчиков о выборе
        /// </summary>
        private void HandleItemSelected(ITreeNode node)
        {
            _provider.SelectNode(node);
            OnNodeSelected?.Invoke(node);
        }

        /// <summary>
        /// Обработчик разворачивания элемента
        /// Вызывается при разворачивании элемента дерева
        /// 
        /// ПАРАМЕТРЫ:
        /// - node: развернутый узел
        /// 
        /// ЛОГИКА:
        /// - Разворачивает узел в провайдере
        /// - Уведомляет подписчиков о разворачивании
        /// </summary>
        private void HandleItemExpanded(ITreeNode node)
        {
            _provider.ExpandNode(node);
            OnNodeExpanded?.Invoke(node);
        }

        /// <summary>
        /// Обработчик сворачивания элемента
        /// Вызывается при сворачивании элемента дерева
        /// 
        /// ПАРАМЕТРЫ:
        /// - node: свернутый узел
        /// 
        /// ЛОГИКА:
        /// - Сворачивает узел в провайдере
        /// - Уведомляет подписчиков о сворачивании
        /// - Обновляет отображение для синхронного сворачивания
        /// </summary>
        private void HandleItemCollapsed(ITreeNode node)
        {
            _provider.CollapseNode(node);
            OnNodeCollapsed?.Invoke(node);
            Refresh(); // Возвращаем вызов Refresh для синхронного сворачивания
        }

        /// <summary>
        /// Сбрасывает выделение всех узлов
        /// Используется для сброса выделения при переключении режима дерева
        /// </summary>
        public void DeselectAllNodes()
        {
            if (_provider == null) return;
            var roots = _provider.GetVisibleNodesPage(0, int.MaxValue);
            foreach (var root in roots)
                DeselectRecursive(root);
        }

        private void DeselectRecursive(ITreeNode node)
        {
            node.IsSelected = false;
            if (node.Children != null)
                foreach (var child in node.Children)
                    DeselectRecursive(child);
        }

        // Получить список id раскрытых узлов
        public List<int> GetExpandedNodeIds()
        {
            var result = new List<int>();
            if (_provider == null) return result;
            var roots = _provider.GetVisibleNodesPage(0, int.MaxValue);
            foreach (var root in roots)
                CollectExpandedIdsRecursive(root, result);
            return result;
        }
        private void CollectExpandedIdsRecursive(ITreeNode node, List<int> ids)
        {
            if (node.IsExpanded)
                ids.Add(node.Id);
            if (node.Children != null)
                foreach (var child in node.Children)
                    CollectExpandedIdsRecursive(child, ids);
        }

        // Восстановить раскрытие по списку id
        public void SetExpandedNodeIds(List<int> ids)
        {
            if (_provider == null || ids == null) return;
            var roots = _provider.GetVisibleNodesPage(0, int.MaxValue);
            foreach (var root in roots)
                SetExpandedRecursive(root, ids);
            Refresh();
        }
        private void SetExpandedRecursive(ITreeNode node, List<int> ids)
        {
            node.IsExpanded = ids.Contains(node.Id);
            if (node.Children != null)
                foreach (var child in node.Children)
                    SetExpandedRecursive(child, ids);
        }

        // Получить id выбранного узла (первого найденного)
        public int? GetSelectedNodeId()
        {
            if (_provider == null) return null;
            var roots = _provider.GetVisibleNodesPage(0, int.MaxValue);
            foreach (var root in roots)
            {
                var id = FindSelectedIdRecursive(root);
                if (id != null) return id;
            }
            return null;
        }
        private int? FindSelectedIdRecursive(ITreeNode node)
        {
            if (node.IsSelected) return node.Id;
            if (node.Children != null)
                foreach (var child in node.Children)
                {
                    var id = FindSelectedIdRecursive(child);
                    if (id != null) return id;
                }
            return null;
        }

        // Восстановить выбор по id
        public void SetSelectedNodeId(int id)
        {
            if (_provider == null) return;
            var roots = _provider.GetVisibleNodesPage(0, int.MaxValue);
            foreach (var root in roots)
                SetSelectedRecursive(root, id);
            Refresh();
        }
        private void SetSelectedRecursive(ITreeNode node, int id)
        {
            node.IsSelected = node.Id == id;
            if (node.Children != null)
                foreach (var child in node.Children)
                    SetSelectedRecursive(child, id);
        }

        // Получить путь по name до выбранного узла (по id)
        public List<string> GetPathToSelectedNodeByName(int selectedId)
        {
            var roots = _provider != null ? _provider.GetVisibleNodesPage(0, int.MaxValue) : null;
            if (roots == null) return null;
            foreach (var root in roots)
            {
                var path = new List<string>();
                if (FindPathRecursiveByName(root, selectedId, path))
                {
                    path.Reverse();
                    return path;
                }
            }
            return null;
        }
        private bool FindPathRecursiveByName(ITreeNode node, int selectedId, List<string> path)
        {
            if (node == null) return false;
            if (node.Id == selectedId)
            {
                path.Add(node.Name);
                return true;
            }
            if (node.Children != null)
            {
                foreach (var child in node.Children)
                {
                    if (FindPathRecursiveByName(child, selectedId, path))
                    {
                        path.Add(node.Name);
                        return true;
                    }
                }
            }
            return false;
        }
        // Раскрыть дерево по пути (по name)
        public void ExpandPathByNames(List<string> path)
        {
            var roots = _provider != null ? _provider.GetVisibleNodesPage(0, int.MaxValue) : null;
            if (roots == null || path == null) return;
            foreach (var root in roots)
                ExpandPathRecursiveByName(root, path, 0);
        }
        private void ExpandPathRecursiveByName(ITreeNode node, List<string> path, int level)
        {
            if (node == null || path == null || level >= path.Count) return;
            if (node.Name == path[level])
            {
                node.IsExpanded = true;
                if (node.Children != null && level + 1 < path.Count)
                {
                    foreach (var child in node.Children)
                        ExpandPathRecursiveByName(child, path, level + 1);
                }
            }
        }

        public void SetPaginationListItems(PaginationListItems items)
        {
            paginationListItems = items;
        }

        public void RefreshAllPagedGroups()
        {
            if (_provider == null) return;
            var roots = _provider.GetVisibleNodesPage(0, int.MaxValue);
            foreach (var root in roots)
                RefreshPagedGroupsRecursive(root);
            Refresh();
        }
        private void RefreshPagedGroupsRecursive(ITreeNode node)
        {
            if (node.Children != null && node.Children.Count > 0)
            {
                if (paginationListItems != null && node.Children.Any(child => child.Children != null && child.Children.Count > 0))
                    paginationListItems.SetPagedNodesOnLevel(node.Children.Where(child => child.Children != null && child.Children.Count > 0).ToList());
                foreach (var child in node.Children)
                    RefreshPagedGroupsRecursive(child);
            }
        }
    }
} 