using System.Collections.Generic;
using System;

namespace Core.TreeView.Models
{
    /// <summary>
    /// Базовая реализация узла дерева (TreeView).
    /// Предоставляет стандартную функциональность для работы с иерархическими данными.
    /// 
    /// ИСПОЛЬЗОВАНИЕ:
    /// - Базовая реализация интерфейса ITreeNode
    /// - Наследуется DatabaseTreeNode для специализированной работы с БД
    /// - Используется как основа для всех типов узлов в дереве
    /// 
    /// СВЯЗИ:
    /// - ITreeNode: реализует интерфейс узла
    /// - DatabaseTreeNode: наследует для работы с БД
    /// - TreeViewController: использует для отображения
    /// 
    /// ОСНОВНЫЕ ВОЗМОЖНОСТИ:
    /// - Управление иерархической структурой
    /// - Состояние развернутости и выбора
    /// - Хранение дополнительных данных
    /// - Базовые операции с дочерними элементами
    /// </summary>
    public class TreeNode : ITreeNode
    {
        /// <summary>
        /// Уникальный идентификатор узла
        /// Используется для идентификации узла в дереве
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// Отображаемое имя узла
        /// Используется для отображения в UI
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Дочерние узлы
        /// Определяет иерархическую структуру дерева
        /// </summary>
        public List<ITreeNode> Children { get; private set; }

        /// <summary>
        /// Флаг развернутости узла
        /// Определяет, отображаются ли дочерние элементы
        /// </summary>
        public bool IsExpanded { get; set; }

        /// <summary>
        /// Флаг выбора узла
        /// Определяет, выбран ли узел пользователем
        /// </summary>
        public bool IsSelected { get; set; }

        /// <summary>
        /// Дополнительные данные узла
        /// Может содержать любые данные, связанные с узлом
        /// </summary>
        public object Data { get; set; }

        public int CurrentPage { get; set; } = 0;
        public int ItemsPerPage { get; set; } = 20;
        public virtual int GetTotalPages()
        {
            if (Children == null || Children.Count == 0) return 1;
            return (int)Math.Ceiling((double)Children.Count / ItemsPerPage);
        }
        public virtual List<ITreeNode> GetPageChildren()
        {
            if (Children == null || Children.Count == 0) return new List<ITreeNode>();
            int start = CurrentPage * ItemsPerPage;
            return Children.GetRange(start, Math.Min(ItemsPerPage, Children.Count - start));
        }

        /// <summary>
        /// Создает новый узел дерева
        /// 
        /// ПАРАМЕТРЫ:
        /// - id: уникальный идентификатор узла
        /// - name: отображаемое имя узла
        /// - data: дополнительные данные узла (опционально)
        /// 
        /// ЛОГИКА:
        /// - Инициализирует все свойства узла
        /// - Создает пустой список дочерних элементов
        /// - Устанавливает начальные состояния (не развернут, не выбран)
        /// </summary>
        public TreeNode(int id, string name, object data = null)
        {
            Id = id;
            Name = name;
            Data = data;
            Children = new List<ITreeNode>();
            IsExpanded = false;
            IsSelected = false;
        }

        /// <summary>
        /// Добавляет дочерний узел
        /// Расширяет иерархическую структуру дерева
        /// 
        /// ПАРАМЕТРЫ:
        /// - child: дочерний узел для добавления
        /// 
        /// ЛОГИКА:
        /// - Добавляет узел в конец списка дочерних элементов
        /// - Не изменяет состояние развернутости родителя
        /// </summary>
        public virtual void AddChild(ITreeNode child)
        {
            if (child != null)
            {
                Children.Add(child);
            }
        }

        /// <summary>
        /// Удаляет дочерний узел
        /// Сокращает иерархическую структуру дерева
        /// 
        /// ПАРАМЕТРЫ:
        /// - child: дочерний узел для удаления
        /// 
        /// ВОЗВРАЩАЕТ:
        /// - true, если узел был найден и удален
        /// - false, если узел не найден
        /// 
        /// ЛОГИКА:
        /// - Ищет узел в списке дочерних элементов
        /// - Удаляет только первое вхождение
        /// - Не удаляет вложенные узлы рекурсивно
        /// </summary>
        public virtual bool RemoveChild(ITreeNode child)
        {
            if (child != null)
            {
                return Children.Remove(child);
            }
            return false;
        }

        /// <summary>
        /// Очищает все дочерние узлы
        /// Удаляет всю иерархию под текущим узлом
        /// 
        /// ЛОГИКА:
        /// - Очищает список дочерних элементов
        /// - Вложенные узлы удаляются автоматически (сборщик мусора)
        /// - Узел становится листовым
        /// </summary>
        public virtual void ClearChildren()
        {
            Children.Clear();
        }

        /// <summary>
        /// Возвращает строковое представление узла
        /// Используется для отладки и логирования
        /// 
        /// ВОЗВРАЩАЕТ:
        /// - Строку с информацией об узле (ID, имя, количество детей)
        /// </summary>
        public override string ToString()
        {
            return $"TreeNode[Id={Id}, Name='{Name}', Children={Children.Count}]";
        }
    }
} 