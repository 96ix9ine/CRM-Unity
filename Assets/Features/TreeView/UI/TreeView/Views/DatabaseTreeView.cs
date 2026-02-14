using Core.Database.Models;
using Core.Database.Providers;
using Core.TreeView.Controllers;
using Core.TreeView.Models;
using System.Collections.Generic;
using UnityEngine;
using UI.Selection;
using Newtonsoft.Json;

namespace UI.TreeView.Views
{
    /// <summary>
    /// Компонент для отображения дерева базы данных с поддержкой интеграции с пагинацией (PaginationListItems).
    /// При выборе поля включает синхронизированную пагинацию для всех полей.
    /// </summary>
    public class DatabaseTreeView : MonoBehaviour
    {
        [SerializeField] private TreeViewController treeViewController;    // Контроллер дерева
        [SerializeField] private DatabaseTreeViewProvider treeViewProvider;// Провайдер данных
        [SerializeField] private PaginationListItems paginationListItems; // Добавить ссылку на пагинацию
        
        /// <summary>
        /// Событие выбора таблицы
        /// </summary>
        public event System.Action<string> OnTableSelected;
        
        /// <summary>
        /// Событие выбора поля таблицы
        /// </summary>
        public event System.Action<string, string> OnFieldSelected;

        /// <summary>
        /// Событие выбора строки (значения) для CRUD
        /// </summary>
        public event System.Action<Dictionary<string, object>> OnRowSelected;

        private void Start()
        {
            if (treeViewController != null && paginationListItems != null)
                treeViewController.SetPaginationListItems(paginationListItems);
            if (treeViewController != null && treeViewProvider != null)
            {
                treeViewController.Initialize(treeViewProvider);
                treeViewController.OnNodeSelected += HandleNodeSelected;
            }
            // Регистрируем глобальный сброс выделения
            SelectionRegistry.RegisterDeselect(ClearSelection);
        }

        /// <summary>
        /// Обработчик выбора узла в дереве. При выборе поля включает пагинацию для всех полей, при выборе таблицы сбрасывает пагинацию по полю.
        /// </summary>
        /// <param name="node">Выбранный узел</param>
        private void HandleNodeSelected(ITreeNode node)
        {
            if (treeViewController != null)
                treeViewController.DeselectAllNodes();
            
            // Выделяем выбранный узел
            if (node != null)
                node.IsSelected = true;
            
            // Обновляем визуальное отображение выбранного элемента
            if (treeViewController != null)
                treeViewController.Refresh();
            
            if (node is TableTreeNode tableNode)
            {
                OnTableSelected?.Invoke(tableNode.TableName);
                if (paginationListItems != null)
                    paginationListItems.SetFieldNode(null); // Сброс пагинации по полю
                GlobalSelectionState.CurrentTableName = tableNode.TableName;
                GlobalSelectionState.SelectedRow = null;
            }
            else if (node is FieldTreeNode fieldNode)
            {
                OnFieldSelected?.Invoke(fieldNode.TableName, fieldNode.FieldName);
                if (paginationListItems != null)
                    paginationListItems.SetFieldNode(fieldNode); // Включить пагинацию по значениям поля
                GlobalSelectionState.CurrentTableName = fieldNode.TableName;
                GlobalSelectionState.SelectedRow = null;
            }
            else if (node is FieldValueTreeNode valueNode)
            {
                // Передаем всю строку, если она есть в Data
                if (valueNode.Data is Dictionary<string, object> fullRow)
                {
                    OnRowSelected?.Invoke(fullRow);
                    GlobalSelectionState.SelectedRow = fullRow;
                    GlobalSelectionState.CurrentTableName = valueNode.TableName;
                }
                else
                {
                    var fallbackRow = new Dictionary<string, object>();
                    fallbackRow["id"] = valueNode.Value;
                    fallbackRow["fieldName"] = valueNode.FieldName;
                    fallbackRow["tableName"] = valueNode.TableName;
                    OnRowSelected?.Invoke(fallbackRow);
                    GlobalSelectionState.SelectedRow = fallbackRow;
                    GlobalSelectionState.CurrentTableName = valueNode.TableName;
                }
            }
            // Debug.LogError("[TreeView] Data for modal: " + JsonConvert.SerializeObject(node.Data));

            // Универсальный вызов OnRowSelected для любого узла
            if (OnRowSelected != null)
            {
                var row = new Dictionary<string, object>();
                // Сначала копируем все данные из node.Data, включая id из бэкенда
                if (node.Data is Dictionary<string, object> dataDict)
                {
                    foreach (var kv in dataDict)
                        row[kv.Key] = kv.Value;
                }
                // Устанавливаем базовые поля, только если их нет в данных
                if (!row.ContainsKey("id"))
                    row["id"] = node.Id;
                if (!row.ContainsKey("name"))
                    row["name"] = node.Name;
                // Устанавливаем nodeType из данных, если есть, иначе из типа узла
                if (!row.ContainsKey("nodeType"))
                {
                    if (node.Data is Dictionary<string, object> data && data.ContainsKey("nodeType"))
                        row["nodeType"] = data["nodeType"];
                    else
                        row["nodeType"] = node.GetType().Name;
                }
                OnRowSelected(row);
            }
        }

        /// <summary>
        /// Обновляет отображение дерева
        /// </summary>
        public void Refresh()
        {
            treeViewProvider?.Refresh();
            treeViewController?.Refresh();
        }

        /// <summary>
        /// Показывает дерево только для выбранной таблицы
        /// </summary>
        public void ShowTableTree(string tableName)
        {
            if (treeViewProvider != null && treeViewController != null)
            {
                treeViewProvider.OnDataLoaded += HandleProviderDataLoaded;
                treeViewProvider.ShowTableTree(tableName);
            }
        }

        /// <summary>
        /// Обработчик события загрузки данных провайдером
        /// </summary>
        private void HandleProviderDataLoaded()
        {
            if (treeViewProvider != null)
            {
                treeViewProvider.OnDataLoaded -= HandleProviderDataLoaded;
            }

            if (treeViewController != null)
            {
                treeViewController.Refresh();
            }
        }

        /// <summary>
        /// Очищает дерево
        /// </summary>
        public void Clear()
        {
            treeViewProvider?.Clear();
            treeViewController?.Refresh();
        }

        /// <summary>
        /// Полный сброс выбора в дереве БД
        /// </summary>
        public void ClearSelection()
        {
            if (treeViewController != null)
            {
                treeViewController.DeselectAllNodes();
                treeViewController.Refresh();
            }
            GlobalSelectionState.Clear();
        }

        public void SetAllowedTablesForRole(HashSet<string> tables)
        {
            treeViewProvider.SetAllowedTablesForRole(tables);
            Refresh();
        }

        public void SetCustomTreeRoot(ITreeNode root)
        {
            if (treeViewController != null)
            {
                treeViewController.SetCustomRoot(root);
                treeViewController.Refresh();
            }
        }
    }
} 
