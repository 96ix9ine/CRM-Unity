using System;

[Serializable]
public class NotificationData {
    public string Id; // guid
    public string UserId;
    public string Message;
    public DateTime Time;
    public NotificationType Type;
}

public enum NotificationType {
    Info, Success, Warning, Error
} 