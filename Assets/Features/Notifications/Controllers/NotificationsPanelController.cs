using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class NotificationsPanelController : MonoBehaviour
{
    public GameObject panel; // NotificationsPanel
    public Transform content; // Content внутри ScrollView
    public GameObject notificationItemPrefab; // NotificationsImage (prefab)
    public Sprite readIcon, unreadIcon;
    public Image notificationIcon;

    private void Start()
    {
        panel.SetActive(false);
        UpdateIcon();
        if (NotificationManager.Instance != null)
            NotificationManager.Instance.OnNotificationsChanged += UpdateIcon;
    }

    private void OnDestroy()
    {
        if (NotificationManager.Instance != null)
            NotificationManager.Instance.OnNotificationsChanged -= UpdateIcon;
    }

    public void OnClearNotificationsButton()
    {
        NotificationManager.Instance.ClearNotifications();
        ShowAllNotifications(); // обновляет UI
        if (panel != null) panel.SetActive(false); // Закрываем панель после очистки
    }

    public void TogglePanel()
    {
        bool isActive = panel.activeSelf;
        panel.SetActive(!isActive);
        if (!isActive)
        {
            ShowAllNotifications();
            NotificationManager.Instance.MarkAllAsRead();
            UpdateIcon();
        }
    }

    public void ShowAllNotifications()
    {
        foreach (Transform child in content)
            Destroy(child.gameObject);

        var notifications = NotificationManager.Instance.GetNotifications();
        foreach (var notif in notifications)
        {
            var go = Instantiate(notificationItemPrefab, content);
            var text = go.GetComponentInChildren<TextMeshProUGUI>();
            text.text = notif.Message;

            // Цвет фона по типу уведомления
            var bg = go.GetComponent<Image>();
            if (bg != null)
            {
                switch (notif.Type)
                {
                    case NotificationType.Success: bg.color = Color.green; break;
                    case NotificationType.Warning: bg.color = Color.yellow; break;
                    case NotificationType.Error: bg.color = Color.red; break;
                    default: bg.color = Color.gray; break;
                }
            }

            // Время уведомления
            var timeText = go.transform.Find("NotificationsTimeText")?.GetComponent<TextMeshProUGUI>();
            if (timeText != null)
                timeText.text = notif.Time.ToString("dd.MM.yyyy HH:mm:ss");
        }
    }

    public void UpdateIcon()
    {
        bool hasUnread = NotificationManager.Instance.HasUnread();
        notificationIcon.sprite = hasUnread ? unreadIcon : readIcon;
    }
} 