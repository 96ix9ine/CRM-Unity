using UnityEngine;
using TMPro;

/// <summary>
/// Компонент для отображения названия таблицы в модальном окне
/// Используется для разделения полей разных таблиц
/// </summary>
public class ModalTableNameContainer : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text tableNameText;        // Текст с названием таблицы
    
    /// <summary>
    /// Устанавливает название таблицы в контейнер
    /// </summary>
    /// <param name="tableName">Название таблицы</param>
    public void SetTableName(string tableName)
    {
        if (tableNameText != null)
        {
            string displayName = TableNameService.GetTableDisplayName(tableName);
            tableNameText.text = $"{displayName}";
        }
    }
    
    /// <summary>
    /// Устанавливает кастомный текст
    /// </summary>
    /// <param name="text">Текст для отображения</param>
    public void SetCustomText(string text)
    {
        if (tableNameText != null)
        {
            tableNameText.text = text;
        }
    }
}
