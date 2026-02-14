using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Core.TreeView.Controllers;
using System;
using System.Linq; // Added for .Select()

/// <summary>
/// Компонент для управления пагинацией как для обычных таблиц, так и для дерева (TreeView).
/// Поддерживает:
/// - Пагинацию для ListItems (обычные таблицы)
/// - Синхронизированную пагинацию для всех полей (FieldTreeNode) в TreeView
/// - Управление страницами, количеством записей, кнопками и выпадающим списком
/// 
/// СВЯЗИ С ДРУГИМИ КОМПОНЕНТАМИ:
/// - ListItems: обновляет данные через UpdateTableWithData для обычных таблиц
/// - TreeViewController: синхронизирует пагинацию для иерархических таблиц
/// - SearchField: получает отфильтрованные данные через SetFilteredRows
/// - DatabaseTreeView: управляет пагинацией через SetFieldNode
/// 
/// ЛОГИКА РАБОТЫ:
/// - Для обычных таблиц: пагинирует данные в ListItems
/// - Для иерархических таблиц: синхронизирует пагинацию всех полей в TreeView
/// - При смене режима: автоматически переключается между типами пагинации
/// </summary>
public class PaginationListItems : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button forceLeftButton;        // Кнопка "Первая страница"
    [SerializeField] private Button leftButton;             // Кнопка "Предыдущая страница"
    [SerializeField] private Button rightButton;            // Кнопка "Следующая страница"
    [SerializeField] private Button forceRightButton;       // Кнопка "Последняя страница"
    [SerializeField] private TMPro.TMP_Dropdown dropdown;   // Выпадающий список для выбора количества элементов
    [SerializeField] private GameObject numberPagePrefab;   // Префаб кнопки номера страницы (неактивный)
    [SerializeField] private Transform numberPagesContainer; // Контейнер для кнопок страниц (PaginationListItemsPanel)

    [Header("Ссылки")]
    [SerializeField] private ListItems listItems;           // Ссылка на компонент обычных таблиц
    [SerializeField] private TreeViewController treeViewController; // Ссылка на контроллер дерева

    // Данные для обычных таблиц
    private List<Dictionary<string, object>> allRows = new List<Dictionary<string, object>>();
    private int currentPage = 0;
    private int itemsPerPage = 20; // Изменено значение по умолчанию
    private int totalPages = 1;

    // Данные для TreeView (иерархические таблицы)
    private Core.Database.Models.FieldTreeNode currentFieldNode; // Текущее выбранное поле (для обратной совместимости)
    private int fieldTotalPages = 1;

    /// <summary>
    /// Список всех FieldTreeNode текущей таблицы для синхронизации пагинации в TreeView
    /// Используется для одновременной пагинации всех полей в иерархических таблицах
    /// </summary>
    private List<Core.Database.Models.FieldTreeNode> cachedFieldNodes = new List<Core.Database.Models.FieldTreeNode>();
    
    /// <summary>
    /// Текущая страница для всех полей в TreeView
    /// Синхронизируется между всеми FieldTreeNode в cachedFieldNodes
    /// </summary>
    private int treeFieldsCurrentPage = 0;
    
    /// <summary>
    /// Количество записей на странице для всех полей в TreeView
    /// Синхронизируется между всеми FieldTreeNode в cachedFieldNodes
    /// </summary>
    private int treeFieldsItemsPerPage = 20;

    // --- УНИВЕРСАЛЬНАЯ СИНХРОНИЗИРОВАННАЯ ПАГИНАЦИЯ ДЛЯ ВСЕХ УЗЛОВ НА УРОВНЕ ---
    private List<Core.TreeView.Models.ITreeNode> pagedNodesOnLevel = new List<Core.TreeView.Models.ITreeNode>();

    /// <summary>
    /// Инициализация компонента
    /// Настраивает обработчики событий для кнопок и выпадающего списка
    /// </summary>
    void Start()
    {
        if (leftButton != null) leftButton.onClick.AddListener(PrevPage);
        if (rightButton != null) rightButton.onClick.AddListener(NextPage);
        if (forceLeftButton != null) forceLeftButton.onClick.AddListener(FirstPage);
        if (forceRightButton != null) forceRightButton.onClick.AddListener(LastPage);
        if (dropdown != null) 
        {
            dropdown.onValueChanged.AddListener(OnDropdownChanged);
            // Устанавливаем значение по умолчанию
            for (int i = 0; i < dropdown.options.Count; i++)
            {
                if (dropdown.options[i].text.Trim() == "20")
                {
                    dropdown.value = i;
                    break;
                }
            }
            // Синхронизируем состояние с UI
            OnDropdownChanged(dropdown.value);
        }
    }

    /// <summary>
    /// Устанавливает все строки для пагинации обычных таблиц
    /// Вызывается из ListItems при загрузке новых данных
    /// 
    /// ПАРАМЕТРЫ:
    /// - rows: полный список строк таблицы для пагинации
    /// 
    /// ЛОГИКА:
    /// - Сбрасывает текущую страницу на 0
    /// - Очищает данные TreeView при переключении на обычную таблицу
    /// - Обновляет пагинацию
    /// </summary>
    public void SetAllRows(List<Dictionary<string, object>> rows)
    {
        allRows = rows ?? new List<Dictionary<string, object>>();
        currentPage = 0;
        currentFieldNode = null; // Сброс пагинации по полю при обычной таблице
        treeFieldsCurrentPage = 0; // сброс для TreeView
        cachedFieldNodes.Clear(); // сброс для TreeView
        pagedNodesOnLevel.Clear(); // сброс для TreeView
        UpdatePagination();
        if (dropdown != null && listItems != null && currentFieldNode == null && cachedFieldNodes.Count == 0)
            OnDropdownChanged(dropdown.value);
    }

    /// <summary>
    /// Обработчик изменения выпадающего списка количества элементов на странице
    /// Синхронизирует размер страницы для всех режимов пагинации
    /// 
    /// ПАРАМЕТРЫ:
    /// - value: индекс выбранной опции в выпадающем списке
    /// 
    /// ЛОГИКА:
    /// - Парсит значение из текста опции
    /// - Для TreeView: синхронизирует размер страницы всех полей
    /// - Для обычных таблиц: обновляет пагинацию
    /// - Сбрасывает текущую страницу на 0 при изменении размера
    /// </summary>
    public void OnDropdownChanged(int value)
    {
        // Debug.Log($"[Pagination] OnDropdownChanged value={value} (dropdown={dropdown?.options[value].text})");
        if (dropdown != null)
        {
            int parsed = 20; // Значение по умолчанию изменено
            var option = dropdown.options[value].text.Trim();
            if (!int.TryParse(option, out parsed) || parsed <= 0)
                parsed = 20;
            itemsPerPage = parsed;
            currentPage = 0;
            // Debug.Log($"[Pagination] itemsPerPage set to {itemsPerPage}");
            if (pagedNodesOnLevel.Count > 0)
            {
                foreach (var n in pagedNodesOnLevel)
                {
                    n.ItemsPerPage = itemsPerPage;
                    n.CurrentPage = 0;
                    // Debug.Log($"[Pagination] Set node {n.Name} ItemsPerPage={n.ItemsPerPage} CurrentPage={n.CurrentPage} Children={n.Children?.Count}");
                }
                UpdatePaginationForLevel();
            }
            else
            {
                UpdatePagination();
            }
            // Вызов только здесь!
            if (treeViewController != null)
                treeViewController.RefreshAllPagedGroups();
        }
    }

    /// <summary>
    /// Обновляет пагинацию для текущего режима (таблица/дерево/значения поля)
    /// Определяет тип пагинации и вызывает соответствующий метод обновления
    /// 
    /// ПРИОРИТЕТЫ:
    /// 1. Обычная таблица (listItems != null, currentFieldNode == null, cachedFieldNodes.Count == 0)
    /// 2. Пагинация для значений поля (currentFieldNode != null)
    /// 3. Пагинация по корню дерева (treeViewController != null, cachedFieldNodes.Count == 0)
    /// 4. Пагинация для всех полей в TreeView (cachedFieldNodes.Count > 0)
    /// </summary>
    private void UpdatePagination()
    {
        // Обычная таблица (приоритетно)
        if (listItems != null && currentFieldNode == null && cachedFieldNodes.Count == 0)
        {
            totalPages = Mathf.Max(1, Mathf.CeilToInt((float)allRows.Count / itemsPerPage));
            currentPage = Mathf.Clamp(currentPage, 0, totalPages - 1);
            ShowCurrentPage();
            UpdatePageButtons();
            return;
        }

        // Пагинация для значений поля
        if (currentFieldNode != null)
        {
            itemsPerPage = currentFieldNode.ItemsPerPage;
            fieldTotalPages = currentFieldNode.GetTotalPages();
            currentFieldNode.CurrentPage = Mathf.Clamp(currentFieldNode.CurrentPage, 0, fieldTotalPages - 1);
            UpdatePageButtons();
            return;
        }

        // Пагинация по корню дерева
        if (treeViewController != null && cachedFieldNodes.Count == 0)
        {
            var info = treeViewController.GetPaginationInfo();
            totalPages = Mathf.Max(1, Mathf.CeilToInt((float)info.total / itemsPerPage));
            currentPage = Mathf.Clamp(currentPage, 0, totalPages - 1);
            treeViewController.SetPagination(currentPage, itemsPerPage);
            UpdatePageButtons();
            return;
        }

        // Пагинация для всех полей в TreeView
        if (cachedFieldNodes.Count > 0)
        {
            UpdatePaginationForField();
        }
    }

    /// <summary>
    /// Показывает текущую страницу для обычной таблицы (ListItems)
    /// Вычисляет диапазон строк для текущей страницы и обновляет отображение
    /// 
    /// ЛОГИКА:
    /// - Вычисляет начальный и конечный индексы для текущей страницы
    /// - Проверяет границы массива и корректирует при необходимости
    /// - Обновляет ListItems с данными текущей страницы
    /// </summary>
    private void ShowCurrentPage()
    {
        // Обычная таблица: всегда обновляем, если listItems != null и currentFieldNode == null
        if (listItems != null && currentFieldNode == null && cachedFieldNodes.Count == 0)
        {
            if (itemsPerPage <= 0) itemsPerPage = 20;
            int start = currentPage * itemsPerPage;
            int end = Mathf.Min(start + itemsPerPage, allRows.Count);
            
            if (start >= allRows.Count) 
            {
                start = 0;
                currentPage = 0;
            }
            
            if (end < start) end = start;
            
            var pageRows = (end > start) ? allRows.GetRange(start, end - start) : new List<Dictionary<string, object>>();
            // Debug.LogError($"[Pagination] Показываем строки: {start} - {end} из {allRows.Count}");
            listItems.UpdateTableWithData(pageRows);
        }
    }

    /// <summary>
    /// Обновляет кнопки пагинации и их обработчики для всех режимов
    /// Создает кнопки номеров страниц и настраивает их состояние
    /// 
    /// ЛОГИКА:
    /// - Определяет количество страниц и текущую страницу в зависимости от режима
    /// - Создает кнопки для видимых страниц (максимум 3)
    /// - Добавляет троеточие и последнюю страницу при необходимости
    /// - Настраивает состояние кнопок навигации (включены/выключены)
    /// </summary>
    private void UpdatePageButtons()
    {
        int pages = 1;
        int currentPageIndex = 0;
        
        if (currentFieldNode != null)
        {
            pages = fieldTotalPages;
            currentPageIndex = currentFieldNode.CurrentPage;
        }
        else if (cachedFieldNodes.Count > 0)
        {
            pages = fieldTotalPages;
            currentPageIndex = treeFieldsCurrentPage;
        }
        else
        {
            pages = totalPages;
            currentPageIndex = currentPage;
        }
        
        // Удаляем все клоны, кроме префаба
        for (int i = numberPagesContainer.childCount - 1; i >= 0; i--)
        {
            var child = numberPagesContainer.GetChild(i);
            if (child.gameObject != numberPagePrefab)
                Destroy(child.gameObject);
        }
        numberPagePrefab.SetActive(false);

        int maxVisible = 3;
        int startPage = Mathf.Clamp(currentPageIndex, 0, Mathf.Max(0, pages - maxVisible));
        int endPage = Mathf.Min(startPage + maxVisible, pages);

        // Обновляем состояние кнопок навигации
        if (forceLeftButton != null) forceLeftButton.interactable = currentPageIndex > 0;
        if (leftButton != null) leftButton.interactable = currentPageIndex > 0;
        if (rightButton != null) rightButton.interactable = currentPageIndex < pages - 1;
        if (forceRightButton != null) forceRightButton.interactable = currentPageIndex < pages - 1;

        for (int i = startPage; i < endPage; i++)
        {
            var pageObj = Instantiate(numberPagePrefab, numberPagesContainer);
            pageObj.SetActive(true);

            var txt = pageObj.GetComponentInChildren<UnityEngine.UI.Text>();
            if (txt != null)
                txt.text = (i + 1).ToString();

            int pageIndex = i;
            var btn = pageObj.GetComponent<UnityEngine.UI.Button>();
            btn.onClick.RemoveAllListeners();
            
            if (currentFieldNode != null || cachedFieldNodes.Count > 0)
                btn.onClick.AddListener(() => { treeFieldsCurrentPage = pageIndex; UpdatePaginationForField(); });
            else
                btn.onClick.AddListener(() => { currentPage = pageIndex; UpdatePagination(); });

            btn.interactable = (currentFieldNode != null || cachedFieldNodes.Count > 0)
                ? (pageIndex != treeFieldsCurrentPage)
                : (pageIndex != currentPage);
        }

        // Если есть скрытые страницы — показываем троеточие и последнюю страницу
        if (endPage < pages)
        {
            // Кнопка с троеточием (неактивная)
            var dotsObj = Instantiate(numberPagePrefab, numberPagesContainer);
            dotsObj.SetActive(true);
            var dotsTxt = dotsObj.GetComponentInChildren<UnityEngine.UI.Text>();
            if (dotsTxt != null)
                dotsTxt.text = "...";
            var dotsBtn = dotsObj.GetComponentInChildren<UnityEngine.UI.Button>();
            dotsBtn.interactable = false;

            // Последняя страница
            var lastObj = Instantiate(numberPagePrefab, numberPagesContainer);
            lastObj.SetActive(true);
            var lastTxt = lastObj.GetComponentInChildren<UnityEngine.UI.Text>();
            if (lastTxt != null)
                lastTxt.text = pages.ToString();
            
            int lastPageIndex = pages - 1;
            var lastBtn = lastObj.GetComponentInChildren<UnityEngine.UI.Button>();
            lastBtn.onClick.RemoveAllListeners();
            
            if (currentFieldNode != null || cachedFieldNodes.Count > 0)
                lastBtn.onClick.AddListener(() => { treeFieldsCurrentPage = lastPageIndex; UpdatePaginationForField(); });
            else
                lastBtn.onClick.AddListener(() => { currentPage = lastPageIndex; UpdatePagination(); });
            
            lastBtn.interactable = (currentFieldNode != null || cachedFieldNodes.Count > 0)
                ? (lastPageIndex != treeFieldsCurrentPage)
                : (lastPageIndex != currentPage);
        }
    }

    /// <summary>
    /// Переход на следующую страницу (синхронизировано для всех полей в TreeView)
    /// Увеличивает номер текущей страницы и обновляет отображение
    /// </summary>
    public void NextPage()
    {
        if (pagedNodesOnLevel.Count > 0)
        {
            foreach (var n in pagedNodesOnLevel) n.CurrentPage++;
            UpdatePaginationForLevel();
            if (treeViewController != null)
                treeViewController.Refresh();
            return;
        }
        if (currentFieldNode != null || cachedFieldNodes.Count > 0)
        {
            treeFieldsCurrentPage++;
            UpdatePaginationForField();
            return;
        }
        if (currentPage < totalPages - 1)
        {
            currentPage++;
            UpdatePagination();
        }
    }

    /// <summary>
    /// Переход на предыдущую страницу (синхронизировано для всех полей в TreeView)
    /// Уменьшает номер текущей страницы и обновляет отображение
    /// </summary>
    public void PrevPage()
    {
        if (pagedNodesOnLevel.Count > 0)
        {
            foreach (var n in pagedNodesOnLevel) n.CurrentPage--;
            if (pagedNodesOnLevel.Count > 0 && pagedNodesOnLevel[0].CurrentPage < 0)
                foreach (var n in pagedNodesOnLevel) n.CurrentPage = 0;
            UpdatePaginationForLevel();
            if (treeViewController != null)
                treeViewController.Refresh();
            return;
        }
        if (currentFieldNode != null || cachedFieldNodes.Count > 0)
        {
            treeFieldsCurrentPage--;
            if (treeFieldsCurrentPage < 0) treeFieldsCurrentPage = 0;
            UpdatePaginationForField();
            return;
        }
        if (currentPage > 0)
        {
            currentPage--;
            UpdatePagination();
        }
    }

    /// <summary>
    /// Переход на первую страницу (синхронизировано для всех полей в TreeView)
    /// Устанавливает номер страницы в 0 и обновляет отображение
    /// </summary>
    public void FirstPage()
    {
        if (pagedNodesOnLevel.Count > 0)
        {
            foreach (var n in pagedNodesOnLevel) n.CurrentPage = 0;
            UpdatePaginationForLevel();
            if (treeViewController != null)
                treeViewController.Refresh();
            return;
        }
        if (currentFieldNode != null || cachedFieldNodes.Count > 0)
        {
            treeFieldsCurrentPage = 0;
            UpdatePaginationForField();
            return;
        }
        currentPage = 0;
        UpdatePagination();
    }

    /// <summary>
    /// Переход на последнюю страницу (синхронизировано для всех полей в TreeView)
    /// Вычисляет номер последней страницы и обновляет отображение
    /// </summary>
    public void LastPage()
    {
        if (pagedNodesOnLevel.Count > 0)
        {
            int maxChildren = 1;
            foreach (var n in pagedNodesOnLevel)
                if (n.Children != null && n.Children.Count > maxChildren)
                    maxChildren = n.Children.Count;
            int totalPages = (int)Math.Ceiling((double)maxChildren / itemsPerPage);
            foreach (var n in pagedNodesOnLevel) n.CurrentPage = totalPages - 1;
            UpdatePaginationForLevel();
            if (treeViewController != null)
                treeViewController.Refresh();
            return;
        }
        if (currentFieldNode != null || cachedFieldNodes.Count > 0)
        {
            // Считаем totalPages по максимальному количеству значений
            int maxValues = 1;
            foreach (var node in cachedFieldNodes)
            {
                if (node.Children.Count > maxValues)
                    maxValues = node.Children.Count;
            }
            int totalPages = (int)Math.Ceiling((double)maxValues / treeFieldsItemsPerPage);
            treeFieldsCurrentPage = totalPages - 1;
            UpdatePaginationForField();
            return;
        }
        currentPage = totalPages - 1;
        UpdatePagination();
    }

    /// <summary>
    /// Устанавливает выбранное поле для TreeView и синхронизирует пагинацию для всех полей текущей таблицы
    /// Вызывается из DatabaseTreeView при выборе поля в иерархической таблице
    /// 
    /// ПАРАМЕТРЫ:
    /// - node: FieldTreeNode выбранного поля или null для сброса
    /// 
    /// ЛОГИКА:
    /// - Собирает все FieldTreeNode текущей таблицы в cachedFieldNodes
    /// - Синхронизирует текущую страницу и размер страницы для всех полей
    /// - Обновляет пагинацию для TreeView
    /// </summary>
    public void SetFieldNode(Core.Database.Models.FieldTreeNode node)
    {
        // Найти все FieldTreeNode текущей таблицы
        cachedFieldNodes.Clear();
        if (node != null && treeViewController != null)
        {
            // Получаем корневой узел и ищем таблицу
            var root = treeViewController.GetProvider()?.Root;
            if (root != null && root.Children.Count > 0)
            {
                var tableNode = root.Children[0]; // Первый ребенок корня - это таблица
                foreach (var sibling in tableNode.Children)
                {
                    if (sibling is Core.Database.Models.FieldTreeNode ftn)
                        cachedFieldNodes.Add(ftn);
                }
            }
        }
        // Если выбрали поле — синхронизируем всем текущие значения
        if (cachedFieldNodes.Count > 0)
        {
            foreach (var ftn in cachedFieldNodes)
            {
                ftn.CurrentPage = treeFieldsCurrentPage;
                ftn.ItemsPerPage = treeFieldsItemsPerPage;
            }
        }
        currentFieldNode = node; // Для обратной совместимости
        UpdatePaginationForField();
    }

    /// <summary>
    /// Синхронизирует текущую страницу и размер страницы для всех FieldTreeNode
    /// Вызывается при изменении размера страницы в выпадающем списке
    /// </summary>
    private void SyncAllFieldNodesPagination()
    {
        foreach (var node in cachedFieldNodes)
        {
            node.CurrentPage = treeFieldsCurrentPage;
            node.ItemsPerPage = treeFieldsItemsPerPage;
        }
    }

    /// <summary>
    /// Обновляет пагинацию для всех полей в TreeView, пересчитывает общее число страниц и обновляет UI
    /// Вызывается при изменении страницы или размера страницы для иерархических таблиц
    /// 
    /// ЛОГИКА:
    /// - Вычисляет максимальное количество значений среди всех полей
    /// - Синхронизирует текущую страницу и размер страницы для всех FieldTreeNode
    /// - Обновляет TreeViewController для перерисовки дерева
    /// - Обновляет кнопки пагинации
    /// </summary>
    private void UpdatePaginationForField()
    {
        if (cachedFieldNodes.Count == 0 || treeViewController == null) return;
        // Считаем общее число страниц по максимальному количеству значений среди всех полей
        int maxValues = 1;
        foreach (var node in cachedFieldNodes)
        {
            if (node.Children.Count > maxValues)
                maxValues = node.Children.Count;
        }

        treeFieldsItemsPerPage = (cachedFieldNodes.Count > 0) ? cachedFieldNodes[0].ItemsPerPage : 20;
        int totalPages = (int)Math.Ceiling((double)maxValues / treeFieldsItemsPerPage);
        treeFieldsCurrentPage = Mathf.Clamp(treeFieldsCurrentPage, 0, totalPages - 1);
        // Синхронизируем всем
        foreach (var node in cachedFieldNodes)
        {
            node.CurrentPage = treeFieldsCurrentPage;
            node.ItemsPerPage = treeFieldsItemsPerPage;
        }

        fieldTotalPages = totalPages;
        treeViewController.Refresh();
        UpdatePageButtons();
    }

    /// <summary>
    /// Устанавливает отфильтрованные строки для пагинации
    /// Вызывается из SearchField при применении поиска к обычным таблицам
    /// 
    /// ПАРАМЕТРЫ:
    /// - rows: отфильтрованные строки для пагинации
    /// 
    /// ЛОГИКА:
    /// - Заменяет allRows на отфильтрованные данные
    /// - Сбрасывает текущую страницу на 0
    /// - Очищает данные TreeView
    /// - Обновляет пагинацию
    /// </summary>
    public void SetFilteredRows(List<Dictionary<string, object>> rows)
    {
        allRows = rows ?? new List<Dictionary<string, object>>();
        currentPage = 0;
        currentFieldNode = null;
        UpdatePagination();
    }

    /// <summary>
    /// Сбрасывает пагинацию на первую страницу
    /// Вызывается при необходимости сброса состояния пагинации
    /// </summary>
    public void ResetPagination()
    {
        currentPage = 0;
        treeFieldsCurrentPage = 0;
        UpdatePagination();
    }

    /// <summary>
    /// Полный сброс всей пагинации при переключении таблицы или режима
    /// Вызывается при смене таблицы для очистки всех состояний пагинации
    /// 
    /// ЛОГИКА:
    /// - Сбрасывает все счетчики страниц
    /// - Очищает ссылки на поля TreeView
    /// - Очищает кэш FieldTreeNode
    /// - Обновляет пагинацию
    /// </summary>
    public void ResetAllPagination()
    {
        currentPage = 0;
        treeFieldsCurrentPage = 0;
        currentFieldNode = null;
        cachedFieldNodes.Clear();
        pagedNodesOnLevel.Clear();
        UpdatePagination();
        if (dropdown != null)
            OnDropdownChanged(dropdown.value);
    }

    // --- УНИВЕРСАЛЬНАЯ СИНХРОНИЗИРОВАННАЯ ПАГИНАЦИЯ ДЛЯ ВСЕХ УЗЛОВ НА УРОВНЕ ---
    public void SetPagedNodesOnLevel(List<Core.TreeView.Models.ITreeNode> nodes)
    {
        pagedNodesOnLevel = nodes ?? new List<Core.TreeView.Models.ITreeNode>();
        // Debug.Log($"[Pagination] SetPagedNodesOnLevel: count={pagedNodesOnLevel.Count} names=[{string.Join(", ", pagedNodesOnLevel.Select(n => n.Name))}]");
        foreach (var n in pagedNodesOnLevel)
        {
            // n.CurrentPage = 0; // УБРАНО! Не сбрасываем страницу при каждом Refresh
            n.ItemsPerPage = itemsPerPage;
            // Debug.Log($"[Pagination] Set node {n.Name} ItemsPerPage={n.ItemsPerPage} CurrentPage={n.CurrentPage} Children={n.Children?.Count}");
        }
        UpdatePaginationForLevel();
    }
    private void UpdatePaginationForLevel()
    {
        // Debug.Log($"[Pagination] UpdatePaginationForLevel: pagedNodesOnLevel.Count={pagedNodesOnLevel.Count}");
        if (pagedNodesOnLevel.Count == 0) return;
        // totalPages = максимум страниц среди всех узлов
        int maxChildren = 1;
        foreach (var n in pagedNodesOnLevel)
            if (n.Children != null && n.Children.Count > maxChildren)
                maxChildren = n.Children.Count;
        int totalPages = (int)Math.Ceiling((double)maxChildren / itemsPerPage);
        int currentPage = pagedNodesOnLevel.Count > 0 ? pagedNodesOnLevel[0].CurrentPage : 0;
        // Debug.Log($"[Pagination] totalPages={totalPages} currentPage={currentPage} itemsPerPage={itemsPerPage}");
        // Кнопки
        UpdatePageButtonsForLevel(totalPages, currentPage);
        // UI
        // if (treeViewController != null)
        //     treeViewController.Refresh();
        // if (treeViewController != null)
        //     treeViewController.RefreshAllPagedGroups();
    }
    private void UpdatePageButtonsForLevel(int pages, int currentPageIndex)
    {
        for (int i = numberPagesContainer.childCount - 1; i >= 0; i--)
        {
            var child = numberPagesContainer.GetChild(i);
            if (child.gameObject != numberPagePrefab)
                Destroy(child.gameObject);
        }
        numberPagePrefab.SetActive(false);
        int maxVisible = 3;
        int startPage = Mathf.Clamp(currentPageIndex, 0, Math.Max(0, pages - maxVisible));
        int endPage = Math.Min(startPage + maxVisible, pages);
        if (forceLeftButton != null) forceLeftButton.interactable = currentPageIndex > 0;
        if (leftButton != null) leftButton.interactable = currentPageIndex > 0;
        if (rightButton != null) rightButton.interactable = currentPageIndex < pages - 1;
        if (forceRightButton != null) forceRightButton.interactable = currentPageIndex < pages - 1;
        for (int i = startPage; i < endPage; i++)
        {
            var pageObj = Instantiate(numberPagePrefab, numberPagesContainer);
            pageObj.SetActive(true);
            var txt = pageObj.GetComponentInChildren<UnityEngine.UI.Text>();
            if (txt != null)
                txt.text = (i + 1).ToString();
            int pageIndex = i;
            var btn = pageObj.GetComponent<UnityEngine.UI.Button>();
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => {
                foreach (var n in pagedNodesOnLevel) n.CurrentPage = pageIndex;
                UpdatePaginationForLevel();
                if (treeViewController != null)
                    treeViewController.Refresh();
            });
            btn.interactable = (pageIndex != currentPageIndex);
        }
        if (endPage < pages)
        {
            var dotsObj = Instantiate(numberPagePrefab, numberPagesContainer);
            dotsObj.SetActive(true);
            var dotsTxt = dotsObj.GetComponentInChildren<UnityEngine.UI.Text>();
            if (dotsTxt != null)
                dotsTxt.text = "...";
            var dotsBtn = dotsObj.GetComponentInChildren<UnityEngine.UI.Button>();
            dotsBtn.interactable = false;
            var lastObj = Instantiate(numberPagePrefab, numberPagesContainer);
            lastObj.SetActive(true);
            var lastTxt = lastObj.GetComponentInChildren<UnityEngine.UI.Text>();
            if (lastTxt != null)
                lastTxt.text = pages.ToString();
            int lastPageIndex = pages - 1;
            var lastBtn = lastObj.GetComponentInChildren<UnityEngine.UI.Button>();
            lastBtn.onClick.RemoveAllListeners();
            lastBtn.onClick.AddListener(() => {
                foreach (var n in pagedNodesOnLevel) n.CurrentPage = lastPageIndex;
                UpdatePaginationForLevel();
                if (treeViewController != null)
                    treeViewController.Refresh();
            });
            lastBtn.interactable = (lastPageIndex != currentPageIndex);
        }
    }
    // --- END ---
}