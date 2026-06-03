using TMPro;
using UnityEngine;

public class HUDController : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private GameObject hudRoot;

    [Header("Notifications")]
    [SerializeField] private GameObject clueNotification;
    [SerializeField] private TextMeshProUGUI clueNotificationText;

    private int pendingClues;

    private bool journalOpen;
    private bool dialogueOpen;

    private void Start()
    {
        TrySubscribe();
        ResetClueNotifications();
    }

    private void OnEnable()
    {
        DialogueManager.OnDialogueStateChanged += HandleDialogue;
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
        DialogueManager.OnDialogueStateChanged -= HandleDialogue;

        if (UIManager.Instance != null)
            UIManager.Instance.OnJournalStateChanged -= HandleJournal;
    }

    private void HandleJournal(bool open)
    {
        journalOpen = open;
        RefreshHUD();

        if (open)
            ResetClueNotifications();
    }

    private void HandleDialogue(bool open)
    {
        dialogueOpen = open;
        RefreshHUD();
    }

    private void RefreshHUD()
    {
        if (hudRoot != null)
            hudRoot.SetActive(!journalOpen && !dialogueOpen);
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