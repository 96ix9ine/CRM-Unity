using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// PopupUI — затемняет фон и блокирует любые действия вне модальных окон.
/// Используется для TableEditModal и ConfirmDialog.
/// </summary>
public class PopupUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject overlayPanel; // Панель затемнения (Image с CanvasGroup)
    [SerializeField] private CanvasGroup canvasGroup;  // CanvasGroup для управления блокировкой

    public static PopupUI Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (overlayPanel != null)
            overlayPanel.SetActive(false);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.5f; // Полупрозрачный фон
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }

    /// <summary>
    /// Показать затемнение и заблокировать все действия вне модального окна
    /// </summary>
    public void Show()
    {
        if (overlayPanel != null)
            overlayPanel.SetActive(true);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.5f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }
    }

    /// <summary>
    /// Скрыть затемнение и разблокировать действия
    /// </summary>
    public void Hide()
    {
        if (overlayPanel != null)
            overlayPanel.SetActive(false);
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.5f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
