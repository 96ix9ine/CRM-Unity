using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class NotificationManager : MonoBehaviour {
    public static NotificationManager Instance { get; private set; }
    public GameObject notificationToastPrefab;

    public string currentUserId;
    private List<NotificationData> notifications = new();
    private HashSet<string> readIds = new();
    private Queue<NotificationData> pendingToasts = new Queue<NotificationData>();

    public event System.Action OnNotificationsChanged;

    void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }

    public void SetUser(string userId) {
        // Не подключаемся повторно к тому же пользователю
        if (currentUserId == userId)
        {
            Debug.Log("Пользователь уже подключен, пропускаем повторное подключение");
            return;
        }
        
        currentUserId = userId;
        notifications = NotificationStorageService.Load(userId);
        readIds = new HashSet<string>();
        
        // Автоматически подключаем WebSocket при входе пользователя
        var wsObj = GameObject.FindObjectOfType<NotificationWebSocket>();
        if (wsObj != null) {
            wsObj.Connect(userId);
            Debug.Log("WebSocket автоматически подключен при входе пользователя");
        }
    }

    public void Logout() {
        // Очищаем данные пользователя
        currentUserId = null;
        notifications.Clear();
        readIds.Clear();
        
        // Закрываем WebSocket соединение
        var wsObj = GameObject.FindObjectOfType<NotificationWebSocket>();
        if (wsObj != null) {
            wsObj.Disconnect();
            Debug.Log("WebSocket соединение закрыто при выходе");
        }
        
        OnNotificationsChanged?.Invoke();
    }

    public void AddNotification(string message, NotificationType type = NotificationType.Info) {
        if (string.IsNullOrEmpty(currentUserId)) return;
        var data = new NotificationData {
            Id = System.Guid.NewGuid().ToString(),
            UserId = currentUserId,
            Message = message,
            Time = System.DateTime.Now,
            Type = type
        };
        notifications.Add(data);
        NotificationStorageService.Save(currentUserId, notifications);
        ShowToast(data);
        OnNotificationsChanged?.Invoke();
    }

    public void MarkAllAsRead() {
        foreach (var notif in notifications)
            readIds.Add(notif.Id);
        OnNotificationsChanged?.Invoke();
    }

    public void ClearNotifications() {
        notifications.Clear();
        NotificationStorageService.Save(currentUserId, notifications);
        readIds.Clear();
        OnNotificationsChanged?.Invoke();
    }

    public List<NotificationData> GetNotifications() => notifications;

    private void ShowToast(NotificationData data) {
        var parentGO = GameObject.Find("NotificationsContainer");
        Transform parent = parentGO != null ? parentGO.transform : null;
        if (parent == null) {
            Debug.LogWarning("NotificationsContainer не найден на сцене! Кладём в очередь.");
            pendingToasts.Enqueue(data);
            return;
        }
        var go = Instantiate(notificationToastPrefab, parent);
        var toast = go.GetComponent<NotificationToastUI>();
        toast.Setup(data);
    }

    public void ShowPendingToasts() {
        var parentGO = GameObject.Find("NotificationsContainer");
        Transform parent = parentGO != null ? parentGO.transform : null;
        if (parent == null) return;
        while (pendingToasts.Count > 0) {
            var data = pendingToasts.Dequeue();
            var go = Instantiate(notificationToastPrefab, parent);
            var toast = go.GetComponent<NotificationToastUI>();
            toast.Setup(data);
        }
    }

    public bool HasUnread() {
        foreach (var notif in notifications)
            if (!readIds.Contains(notif.Id))
                return true;
        return false;
    }
} 