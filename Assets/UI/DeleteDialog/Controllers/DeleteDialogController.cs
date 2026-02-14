using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Компонент диалога подтверждения
/// Используется для подтверждения важных действий
/// Теперь поддерживает контекст удаления: таблица, nodeType, id и произвольные поля
/// </summary>
public class DeleteDialogController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject dialogPanel;    // Панель диалога
    [SerializeField] private Text messageText;          // Текст сообщения
    [SerializeField] private Button yesButton;          // Кнопка "Да"
    [SerializeField] private Button noButton;           // Кнопка "Нет"
    [SerializeField] private Transform detailsContainer; // Контейнер для данных
    [SerializeField] private GameObject detailTextPrefab; // Префаб для строки данных

    public event Action<bool> OnConfirm;                // Событие подтверждения (обратная совместимость)
    // Новое событие: подтверждение с контекстом
    public event Action<bool, DeleteContext> OnConfirmWithContext;

    // Текущий контекст удаления
    private DeleteContext currentContext;

    private void Start()
    {
        yesButton.onClick.AddListener(OnYesButtonClicked);
        noButton.onClick.AddListener(OnNoButtonClicked);
        dialogPanel.SetActive(false);
    }

    /// <summary>
    /// Открывает диалог с указанным сообщением и данными (обратная совместимость)
    /// </summary>
    public void OpenWithData(string message, Dictionary<string, object> data)
    {
        currentContext = null; // контекст не задан в этом режиме
        messageText.text = message;
        RenderDetails(data);
        dialogPanel.SetActive(true);
        PopupUI.Instance.Show();
    }

    /// <summary>
    /// Открывает диалог подтверждения удаления с контекстом (таблица, id, nodeType, доп. поля)
    /// </summary>
    public void OpenDelete(string message, string tableName, object id, string nodeType = null, Dictionary<string, object> extra = null)
    {
        currentContext = new DeleteContext
        {
            TableName = tableName,
            NodeType = nodeType,
            Id = id?.ToString(),
            Extra = extra ?? new Dictionary<string, object>()
        };

        // Формируем данные для отображения (без служебных полей: Тип узла, Таблица, ID)
        var view = new Dictionary<string, object>();
        if (currentContext.Extra != null)
        {
            foreach (var kv in currentContext.Extra)
            {
                // Не дублируем ключи
                if (!view.ContainsKey(kv.Key)) view[kv.Key] = kv.Value;
            }
        }

        messageText.text = message;
        RenderDetails(view);
        dialogPanel.SetActive(true);
        PopupUI.Instance.Show();
    }

    // Универсальный рендер деталей в список
    private void RenderDetails(Dictionary<string, object> data)
    {
        if (detailsContainer != null && detailTextPrefab != null)
        {
            // Оставляем только эталон (первый дочерний объект)
            for (int i = detailsContainer.childCount - 1; i > 0; i--)
                Destroy(detailsContainer.GetChild(i).gameObject);

            // Эталон всегда скрыт
            if (detailsContainer.childCount > 0)
                detailsContainer.GetChild(0).gameObject.SetActive(false);

            if (data != null)
            {
                foreach (var kvp in data)
                {
                    var go = Instantiate(detailTextPrefab, detailsContainer);
                    go.SetActive(true);
                    var txt = go.GetComponent<Text>();
                    if (txt != null)
                        txt.text = $"{kvp.Key}: {kvp.Value}";
                }
            }
        }
    }

    /// <summary>
    /// Обработчик нажатия кнопки "Да"
    /// </summary>
    public void OnYesButtonClicked()
    {
        OnConfirm?.Invoke(true);
        if (currentContext != null)
        {
            OnConfirmWithContext?.Invoke(true, currentContext);
        }
        Close();
    }

    /// <summary>
    /// Обработчик нажатия кнопки "Нет"
    /// </summary>
    public void OnNoButtonClicked()
    {
        OnConfirm?.Invoke(false);
        if (currentContext != null)
        {
            OnConfirmWithContext?.Invoke(false, currentContext);
        }
        Close();
    }

    /// <summary>
    /// Закрывает диалог
    /// </summary>
    public void Close()
    {
        dialogPanel.SetActive(false);
        PopupUI.Instance.Hide();
        currentContext = null;
    }
}
