using TMPro;
using UnityEngine;

public class HUDController : MonoBehaviour
{
    [Header("Notifications")]
    [SerializeField] private GameObject clueNotification;
    [SerializeField] private TextMeshProUGUI clueNotificationText;

    private int pendingClues;

    private void Start()
    {
        TrySubscribe();
        ResetClueNotifications();
    }

    private void TrySubscribe()
    {
        if (UIManager.Instance == null)
        {
            Invoke(nameof(TrySubscribe), 0.1f);
            return;
        }

        UIManager.Instance.OnJournalStateChanged += HandleJournal;
    }

    private void OnDisable()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.OnJournalStateChanged -= HandleJournal;
    }

    private void HandleJournal(bool open)
    {
        gameObject.SetActive(!open);

        if (open)
            ResetClueNotifications();
    }

    public void AddClueNotification()
    {
        pendingClues++;

        if (clueNotification != null)
            clueNotification.SetActive(true);

        if (clueNotificationText != null)
            clueNotificationText.text = pendingClues.ToString();
    }

    public void ResetClueNotifications()
    {
        pendingClues = 0;

        if (clueNotification != null)
            clueNotification.SetActive(false);

        if (clueNotificationText != null)
            clueNotificationText.text = string.Empty;
    }
}