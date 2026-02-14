using System;
using UnityEngine;
using NativeWebSocket;
using Newtonsoft.Json;

public class NotificationWebSocket : MonoBehaviour
{
    private WebSocket ws;
    private string userId;
    private bool isConnecting = false; // добавлено

    void Awake() {
        DontDestroyOnLoad(gameObject);
    }

    public void Connect(string userId)
    {
        // Не подключаемся повторно к тому же пользователю
        if (this.userId == userId && ws != null && (ws.State == NativeWebSocket.WebSocketState.Open || ws.State == NativeWebSocket.WebSocketState.Connecting || isConnecting))
        {
            Debug.Log("WebSocket уже подключен или подключается к этому пользователю, пропускаем повторное подключение");
            return;
        }

        // Закрываем предыдущее соединение если есть
        if (ws != null)
        {
            Debug.Log("Закрываем предыдущее WebSocket соединение");
            try { ws.Close(); } catch {}
            ws = null;
        }

        this.userId = userId;
        string wsUrl = $"ws://localhost:3000/ws?userId={userId}";
        ws = new WebSocket(wsUrl);

        isConnecting = true;
        ws.OnOpen += () => { Debug.Log("WebSocket открыт"); isConnecting = false; };
        ws.OnError += (e) => { Debug.LogError("WebSocket ошибка: " + e); isConnecting = false; };
        ws.OnClose += (e) => { Debug.Log("WebSocket закрыт: " + e); isConnecting = false; };

        ws.OnMessage += (bytes) =>
        {
            var msg = System.Text.Encoding.UTF8.GetString(bytes);
            Debug.Log("WebSocket сообщение: " + msg);

            try
            {
                var notif = JsonConvert.DeserializeObject<NotificationMsg>(msg);
                Debug.Log($"Пушим уведомление: {notif.message}, {notif.type}");
                if (NotificationManager.Instance != null)
                {
                    Debug.Log("NotificationManager.Instance найден, вызываю AddNotification");
                    NotificationManager.Instance.AddNotification(notif.message, ParseType(notif.type));
                }
                else
                    Debug.LogError("NotificationManager.Instance == null");
            }
            catch (Exception ex)
            {
                Debug.LogError("Ошибка парсинга уведомления: " + ex.Message);
            }
        };

        ws.Connect();
    }

    public void Disconnect()
    {
        if (ws != null)
        {
            Debug.Log("Закрываем WebSocket соединение");
            try { ws.Close(); } catch {}
            ws = null;
            userId = null;
            isConnecting = false;
        }
    }

    private NotificationType ParseType(string type)
    {
        switch (type?.ToLower())
        {
            case "success": return NotificationType.Success;
            case "warning": return NotificationType.Warning;
            case "error": return NotificationType.Error;
            case "task_assigned": return NotificationType.Success;
            case "task_started": return NotificationType.Info;
            case "task_completed": return NotificationType.Success;
            case "subtask_completed": return NotificationType.Info;
            default: return NotificationType.Info;
        }
    }

    private void Update()
    {
        ws?.DispatchMessageQueue();
    }

    private void OnDestroy()
    {
        ws?.Close();
    }

    private void OnApplicationQuit()
    {
        Debug.Log("Приложение закрывается, отключаем WebSocket");
        Disconnect();
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            Debug.Log("Приложение приостановлено, отключаем WebSocket");
            Disconnect();
        }
        else
        {
            // Приложение возобновлено - переподключаемся если есть userId
            if (!string.IsNullOrEmpty(userId) && ws == null)
            {
                Debug.Log("Приложение возобновлено, переподключаем WebSocket");
                Connect(userId);
            }
        }
    }

    class NotificationMsg
    {
        public string message;
        public string type;
    }
} 