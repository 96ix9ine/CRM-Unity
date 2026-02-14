using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using DG.Tweening;

public class NotificationToastUI : MonoBehaviour {
    public TextMeshProUGUI messageText;
    public Image background;
    public float showDuration = 5f;

    public void Setup(NotificationData data) {
        messageText.text = data.Message;
        // Цвет по типу уведомления
        switch (data.Type) {
            case NotificationType.Success: background.color = Color.green; break;
            case NotificationType.Warning: background.color = Color.yellow; break;
            case NotificationType.Error: background.color = Color.red; break;
            default: background.color = Color.gray; break;
        }
        StartCoroutine(ShowAndHide());
    }

    private IEnumerator ShowAndHide() {
        // Анимация появления (scale)
        transform.localScale = Vector3.zero;
        transform.DOScale(Vector3.one, 0.3f).SetEase(Ease.OutBack);
        yield return new WaitForSeconds(showDuration);
        // Анимация исчезновения (scale)
        var tween = transform.DOScale(Vector3.zero, 0.3f).SetEase(Ease.InBack);
        yield return tween.WaitForCompletion();
        Destroy(gameObject);
    }
} 