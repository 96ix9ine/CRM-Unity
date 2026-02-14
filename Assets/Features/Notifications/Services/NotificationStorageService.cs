using System.Collections.Generic;
using UnityEngine;

public class NotificationStorageService {
    private const string KeyPrefix = "notifications_";

    public static void Save(string userId, List<NotificationData> notifications) {
        string json = JsonUtility.ToJson(new NotificationListWrapper { list = notifications });
        PlayerPrefs.SetString(KeyPrefix + userId, json);
        PlayerPrefs.Save();
    }

    public static List<NotificationData> Load(string userId) {
        string json = PlayerPrefs.GetString(KeyPrefix + userId, "");
        if (string.IsNullOrEmpty(json)) return new List<NotificationData>();
        return JsonUtility.FromJson<NotificationListWrapper>(json).list;
    }

    [System.Serializable]
    private class NotificationListWrapper {
        public List<NotificationData> list;
    }
} 