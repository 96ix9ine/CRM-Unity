using System;
using System.Globalization;
using Core.TreeView.Models;
using Core.Database.Models;
using UnityEngine;
using UnityEngine.UI;

namespace Core.TreeView.Components
{
    /// <summary>
    /// Компонент для отображения элемента дерева
    /// Управляет визуальным представлением и взаимодействием с узлом
    /// 
    /// СВЯЗИ С ДРУГИМИ КОМПОНЕНТАМИ:
    /// - TreeViewController: получает события выбора и разворачивания
    /// - ITreeNode: отображает данные узла
    /// - DatabaseTreeNode: специализированное отображение для узлов БД
    /// 
    /// ЛОГИКА РАБОТЫ:
    /// - Отображает имя узла с учетом типа (таблица, поле, значение)
    /// - Управляет состоянием развернутости (иконки, видимость детей)
    /// - Обрабатывает клики для выбора и разворачивания
    /// - Форматирует даты для полей с датами
    /// </summary>
    public class TreeViewItem : MonoBehaviour
    {
        [SerializeField] private Button expandButton;        // Кнопка разворачивания
        [SerializeField] private Button selectButton;        // Кнопка выбора
        [SerializeField] private Text nameText;              // Текст имени
        [SerializeField] private Image background;           // Фон элемента
        [SerializeField] private RawImage treeViewItemIcon;  // Иконка превью (опционально)
        [SerializeField] private Transform childrenContainer;// Контейнер для дочерних элементов
        [SerializeField] private Image expandIcon;              // Иконка для expand/collapse
        [SerializeField] private Sprite expandedSprite;         // Спрайт для раскрытого состояния
        [SerializeField] private Sprite collapsedSprite;        // Спрайт для свернутого состояния
        [SerializeField] private Color normalColor = Color.white; // Цвет обычного фона
        [SerializeField] private Color selectedColor = new Color(0.8f, 0.9f, 1f); // Цвет выделения
        [SerializeField] private Color componentShortageColor = new Color(1f, 0.8f, 0.8f); // Розовый цвет для задач с недостатком компонентов
        
        private ITreeNode _node;                            // Текущий узел
        
        /// <summary>
        /// Событие выбора элемента
        /// Вызывается при клике на элемент
        /// </summary>
        public event Action<ITreeNode> OnSelected;
        
        /// <summary>
        /// Событие разворачивания элемента
        /// Вызывается при разворачивании узла
        /// </summary>
        public event Action<ITreeNode> OnExpanded;
        
        /// <summary>
        /// Событие сворачивания элемента
        /// Вызывается при сворачивании узла
        /// </summary>
        public event Action<ITreeNode> OnCollapsed;

        /// <summary>
        /// Публичный доступ к контейнеру для дочерних элементов
        /// Используется TreeViewController для размещения дочерних элементов
        /// </summary>
        public Transform ChildrenContainer => childrenContainer;

        /// <summary>
        /// Инициализация компонента
        /// Настраивает обработчики событий для кнопок
        /// </summary>
        private void Awake()
        {
            if (selectButton != null)
                selectButton.onClick.AddListener(HandleExpandClick);
        }

        /// <summary>
        /// Инициализирует элемент с указанным узлом
        /// Вызывается TreeViewController при создании элемента
        /// 
        /// ПАРАМЕТРЫ:
        /// - node: узел для отображения
        /// </summary>
        public void Initialize(ITreeNode node)
        {
            _node = node;
            UpdateView(node);
        }

        /// <summary>
        /// Обновляет визуальное представление элемента
        /// Вызывается при изменении состояния узла
        /// 
        /// ПАРАМЕТРЫ:
        /// - node: узел для обновления
        /// 
        /// ЛОГИКА:
        /// - Отображает имя узла с учетом типа (таблица, поле, значение)
        /// - Устанавливает цвет фона в зависимости от выбора
        /// - Показывает/скрывает кнопку разворачивания
        /// - Обновляет иконку разворачивания
        /// - Управляет видимостью контейнера дочерних элементов
        /// </summary>
        public void UpdateView(ITreeNode node)
        {
            _node = node;
            
            // Для FieldTreeNode отображаем имя поля
            if (node is FieldTreeNode fieldNode)
            {
                if (nameText != null)
                    nameText.text = fieldNode.DisplayName ?? fieldNode.FieldName;
            }
            else if (node is TableTreeNode tableNode)
            {
                if (nameText != null)
                    nameText.text = tableNode.DisplayName ?? tableNode.TableName;
            }
            else if (node is FieldValueTreeNode valueNode)
            {
                if (nameText != null)
                {
                    // Если имя поля содержит date, форматируем
                    if (valueNode.FieldName != null && valueNode.FieldName.ToLower().Contains("date"))
                        nameText.text = FormatDateOnly(valueNode.Value);
                    else
                        nameText.text = valueNode.Value;
                }
            }
            else
            {
                if (nameText != null)
                    nameText.text = node.Name;
            }

            if (background != null)
            {
                // Проверяем, есть ли недостаток компонентов для stand_tasks
                bool hasComponentShortage = CheckComponentShortage(node);
                
                if (node.IsSelected)
                    background.color = selectedColor;
                else if (hasComponentShortage)
                    background.color = componentShortageColor;
                else
                    background.color = normalColor;
            }
            
            if (expandButton != null)
            {
                expandButton.gameObject.SetActive(node.Children.Count > 0 && !(node is FieldValueTreeNode));
            }
            
            if (expandIcon != null && expandedSprite != null && collapsedSprite != null)
                expandIcon.sprite = node.IsExpanded ? expandedSprite : collapsedSprite;
            
            if (childrenContainer != null)
            {
                bool show = node.IsExpanded && node.Children.Count > 0;
                childrenContainer.gameObject.SetActive(show);
                for (int i = 0; i < childrenContainer.childCount; i++)
                    childrenContainer.GetChild(i).gameObject.SetActive(show);
            }

            // Превью-иконка для узлов таблиц/значений, если есть поле image/photo/icon
            TrySetNodePreview(node);
        }

        /// <summary>
        /// Проверяет, есть ли недостаток компонентов для задач
        /// Анализирует данные узла на наличие поля hasEnoughComponents
        /// 
        /// ПАРАМЕТРЫ:
        /// - node: узел для проверки
        /// 
        /// ВОЗВРАЩАЕТ:
        /// - true если есть недостаток компонентов (hasEnoughComponents = false)
        /// - false если компонентов достаточно или это не задача
        /// </summary>
        private bool CheckComponentShortage(ITreeNode node)
        {
            try
            {
                // Проверяем, что у узла есть данные
                if (node?.Data == null) return false;
                
                // Проверяем данные узла как Dictionary
                if (node.Data is System.Collections.Generic.Dictionary<string, object> dataDict)
                {
                    // Проверяем nodeType и hasEnoughComponents для stand_tasks
                    if (dataDict.ContainsKey("nodeType") && dataDict["nodeType"].ToString() == "stand_tasks")
                    {
                        if (dataDict.ContainsKey("hasEnoughComponents"))
                        {
                            bool hasEnough = System.Convert.ToBoolean(dataDict["hasEnoughComponents"]);
                            return !hasEnough; // Возвращаем true если компонентов НЕ хватает
                        }
                        else
                        {
                        }
                    }
                    
                    // Проверяем current_tasks на наличие дочерних задач с недостатком компонентов
                    if (dataDict.ContainsKey("nodeType") && dataDict["nodeType"].ToString() == "current_tasks")
                    {
                        // Рекурсивно проверяем дочерние элементы
                        foreach (var child in node.Children)
                        {
                            if (CheckComponentShortage(child))
                            {
                                return true;
                            }
                        }
                    }
                }
                
                // Альтернативная проверка через строковое представление данных
                string dataString = node.Data?.ToString() ?? "";
                
                if (dataString.Contains("\"nodeType\":\"stand_tasks\"") && 
                    dataString.Contains("\"hasEnoughComponents\":false"))
                {
                    return true; // Есть недостаток компонентов
                }
                
                // Проверяем current_tasks через строковое представление
                if (dataString.Contains("\"nodeType\":\"current_tasks\""))
                {
                    // Рекурсивно проверяем дочерние элементы
                    foreach (var child in node.Children)
                    {
                        if (CheckComponentShortage(child))
                        {
                            return true;
                        }
                    }
                }
                
                // Проверяем имя узла на наличие ключевых слов
                string nodeName = node.Name ?? "";
                if (nodeName.Contains("stand_tasks") || nodeName.Contains("Задача стенда"))
                {
                    // Если в имени есть "stand_tasks" или "Задача стенда", проверяем данные более тщательно
                    if (dataString.Contains("\"hasEnoughComponents\":false"))
                    {
                        return true; // Есть недостаток компонентов
                    }
                }
                else if (nodeName.Contains("current_tasks") || nodeName.Contains("Задача:"))
                {
                    // Рекурсивно проверяем дочерние элементы для current_tasks
                    foreach (var child in node.Children)
                    {
                        if (CheckComponentShortage(child))
                        {
                            return true;
                        }
                    }
                }
            }
            catch (System.Exception ex)
            {
                // В случае ошибки парсинга, не подсвечиваем
            }
            
            return false; // По умолчанию не подсвечиваем
        }
        
        /// <summary>
        /// Обработчик клика по кнопке (выделение + expand/collapse)
        /// Основной обработчик взаимодействия с элементом
        /// 
        /// ЛОГИКА:
        /// - Вызывает событие выбора узла
        /// - Переключает состояние развернутости
        /// - Вызывает соответствующие события (OnExpanded/OnCollapsed)
        /// </summary>
        private void HandleExpandClick()
        {
            // Debug.LogError($"[TreeView] HandleExpandClick: {_node.Name} ({_node.GetType().Name})");
            if (_node == null) return;
            OnSelected?.Invoke(_node);
            if (_node.IsExpanded)
            {
                _node.IsExpanded = false;
                OnCollapsed?.Invoke(_node);
            }
            else
            {
                _node.IsExpanded = true;
                OnExpanded?.Invoke(_node);
            }
        }

        /// <summary>
        /// Очистка при уничтожении объекта
        /// Удаляет обработчики событий для предотвращения утечек памяти
        /// </summary>
        private void OnDestroy()
        {
            if (selectButton != null)
                selectButton.onClick.RemoveListener(HandleExpandClick);
        }

        /// <summary>
        /// Форматирует дату в формате yyyy-MM-dd
        /// Используется для отображения дат в удобном формате
        /// 
        /// ПАРАМЕТРЫ:
        /// - value: строка с датой в формате yyyy-MM-dd
        /// 
        /// ВОЗВРАЩАЕТ:
        /// - Отформатированную дату или исходную строку при ошибке
        /// </summary>
        private string FormatDateOnly(string value)
        {
            DateTime dt;
            if (DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out dt))
                return dt.ToString("yyyy-MM-dd");
            return value;
        }

        private void TrySetNodePreview(ITreeNode node)
        {
            if (treeViewItemIcon == null) return;
            try
            {
                var data = node?.Data as System.Collections.Generic.Dictionary<string, object>;
                if (data == null) return;
                string table = data.ContainsKey("nodeType") && data["nodeType"] != null ? data["nodeType"].ToString() : null;
                string id = data.ContainsKey("id") && data["id"] != null ? data["id"].ToString() : null;
                if (string.IsNullOrEmpty(table) || string.IsNullOrEmpty(id)) return;
                var api = GameObject.FindObjectOfType<TableApiService>();
                if (api == null) return;
                // Запросим связанные изображения и возьмём первое
                api.StartCoroutine(LoadIconCoroutine(api, table, id));
            }
            catch {}
        }

        private System.Collections.IEnumerator LoadIconCoroutine(TableApiService api, string table, string id)
        {
            System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, object>> list = null;
            yield return api.GetJson<System.Collections.Generic.List<System.Collections.Generic.Dictionary<string, object>>>($"/images/byTarget/{table}/{id}", (l) => list = l);
            if (list == null || list.Count == 0 || treeViewItemIcon == null) yield break;
            var row = list[0];
            string imgId = row.ContainsKey("id") && row["id"] != null ? row["id"].ToString() : null;
            if (string.IsNullOrEmpty(imgId)) yield break;
            byte[] bytes = null;
            yield return api.GetBytes($"/images/{imgId}", (b) => bytes = b);
            if (bytes == null || bytes.Length == 0 || treeViewItemIcon == null) yield break;
            var tex = new Texture2D(2,2,TextureFormat.RGBA32,false);
            if (tex.LoadImage(bytes))
            {
                treeViewItemIcon.texture = tex; var c = treeViewItemIcon.color; c.a = 1f; treeViewItemIcon.color = c;
            }
        }
    }
} 