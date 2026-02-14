using Core.TreeView.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.Database.Models
{
    /// <summary>
    /// Узел дерева для базы данных (корень, таблица, поле, значение).
    /// Базовый класс для всех узлов в иерархии базы данных.
    /// 
    /// ИСПОЛЬЗОВАНИЕ:
    /// - Создается как корневой узел для всего дерева базы данных
    /// - Содержит общие свойства для всех типов узлов
    /// - Наследуется специализированными узлами (TableTreeNode, FieldTreeNode, FieldValueTreeNode)
    /// 
    /// СВЯЗИ:
    /// - TreeNode: базовый класс для древовидной структуры
    /// - DatabaseTreeViewProvider: использует для построения дерева
    /// - TreeViewController: отображает узлы в UI
    /// </summary>
    public class DatabaseTreeNode : TreeNode
    {
        /// <summary>
        /// Создает новый узел базы данных
        /// 
        /// ПАРАМЕТРЫ:
        /// - id: уникальный идентификатор узла
        /// - name: отображаемое имя узла
        /// - data: дополнительные данные узла
        /// </summary>
        public DatabaseTreeNode(int id, string name, object data = null) : base(id, name, data)
        {
        }

        /// <summary>
        /// Создает корневой узел дерева базы данных
        /// Используется как точка входа в иерархию
        /// 
        /// ВОЗВРАЩАЕТ:
        /// - DatabaseTreeNode с id=0 и name="Database"
        /// </summary>
        public static DatabaseTreeNode CreateRoot()
        {
            return new DatabaseTreeNode(0, "Database");
        }
    }

    /// <summary>
    /// Узел таблицы (TableTreeNode) в дереве базы данных.
    /// Представляет таблицу в иерархии и содержит поля как дочерние узлы.
    /// 
    /// ИСПОЛЬЗОВАНИЕ:
    /// - Создается для каждой таблицы в базе данных
    /// - Содержит поля таблицы как дочерние узлы FieldTreeNode
    /// - Используется в DatabaseTreeViewProvider для построения иерархии
    /// 
    /// СВЯЗИ:
    /// - DatabaseTreeViewProvider: создает при загрузке таблицы
    /// - TreeViewController: обрабатывает выбор таблицы
    /// - DatabaseTreeView: уведомляет о выборе таблицы
    /// </summary>
    public class TableTreeNode : DatabaseTreeNode
    {
        /// <summary>
        /// Название таблицы (наследуется от Name)
        /// Используется для идентификации таблицы в базе данных
        /// </summary>
        public string TableName => Name;
        
        /// <summary>
        /// Локализованное отображаемое имя таблицы
        /// Используется в UI вместо технического названия таблицы
        /// </summary>
        public string DisplayName { get; set; }
        
        /// <summary>
        /// Создает новый узел таблицы
        /// 
        /// ПАРАМЕТРЫ:
        /// - id: уникальный идентификатор узла
        /// - tableName: название таблицы в базе данных
        /// </summary>
        public TableTreeNode(int id, string tableName) : base(id, tableName)
        {
        }
    }

    /// <summary>
    /// Узел поля (FieldTreeNode) с поддержкой пагинации значений.
    /// Представляет поле таблицы и содержит его значения как дочерние узлы.
    /// 
    /// ИСПОЛЬЗОВАНИЕ:
    /// - Создается для каждого поля таблицы
    /// - Содержит значения поля как дочерние узлы FieldValueTreeNode
    /// - Поддерживает пагинацию для больших объемов данных
    /// - Синхронизируется с PaginationListItems для единой пагинации всех полей
    /// 
    /// СВЯЗИ:
    /// - DatabaseTreeViewProvider: создает при загрузке полей таблицы
    /// - PaginationListItems: синхронизирует пагинацию между полями
    /// - TreeViewController: отображает значения с учетом пагинации
    /// - DatabaseTreeView: уведомляет о выборе поля
    /// </summary>
    public class FieldTreeNode : DatabaseTreeNode
    {
        /// <summary>
        /// Название поля (наследуется от Name)
        /// Используется для идентификации поля в таблице
        /// </summary>
        public string FieldName => Name;
        
        /// <summary>
        /// Название таблицы, к которой принадлежит поле
        /// Используется для связи с родительской таблицей
        /// </summary>
        public string TableName { get; }
        
        /// <summary>
        /// Локализованное отображаемое имя поля
        /// Используется в UI вместо технического названия поля
        /// </summary>
        public string DisplayName { get; set; }

        /// <summary>
        /// Создает новый узел поля
        /// 
        /// ПАРАМЕТРЫ:
        /// - id: уникальный идентификатор узла
        /// - fieldName: название поля в таблице
        /// - tableName: название таблицы, к которой принадлежит поле
        /// </summary>
        public FieldTreeNode(int id, string fieldName, string tableName) : base(id, fieldName)
        {
            TableName = tableName;
        }
    }

    /// <summary>
    /// Узел значения поля (FieldValueTreeNode) в дереве базы данных.
    /// Представляет конкретное значение поля и содержит данные строки.
    /// 
    /// ИСПОЛЬЗОВАНИЕ:
    /// - Создается для каждого значения поля
    /// - Содержит данные строки в свойстве Data
    /// - Используется для отображения конкретных записей
    /// - Обрабатывает выбор значения для CRUD операций
    /// 
    /// СВЯЗИ:
    /// - DatabaseTreeViewProvider: создает при загрузке значений полей
    /// - TreeViewController: обрабатывает выбор значения
    /// - DatabaseTreeView: уведомляет о выборе строки для CRUD
    /// - TableEditModal: получает данные для редактирования
    /// </summary>
    public class FieldValueTreeNode : DatabaseTreeNode
    {
        /// <summary>
        /// Значение поля (наследуется от Name)
        /// Отображаемое значение в дереве
        /// </summary>
        public string Value => Name;
        
        /// <summary>
        /// Название поля, к которому принадлежит значение
        /// Используется для идентификации поля
        /// </summary>
        public string FieldName { get; }
        
        /// <summary>
        /// Название таблицы, к которой принадлежит значение
        /// Используется для связи с таблицей
        /// </summary>
        public string TableName { get; }
        
        /// <summary>
        /// Создает новый узел значения поля
        /// 
        /// ПАРАМЕТРЫ:
        /// - id: уникальный идентификатор узла
        /// - value: отображаемое значение поля
        /// - fieldName: название поля, к которому принадлежит значение
        /// - tableName: название таблицы, к которой принадлежит значение
        /// </summary>
        public FieldValueTreeNode(int id, string value, string fieldName, string tableName) : base(id, value)
        {
            FieldName = fieldName;
            TableName = tableName;
        }
    }
} 