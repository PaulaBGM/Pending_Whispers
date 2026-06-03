using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private GameObject hudRoot;

    [Header("Notifications")]
    [SerializeField] private GameObject clueNotification;
    [SerializeField] private TextMeshProUGUI clueNotificationText;

    [Header("Detection Button")]
    [SerializeField] private Image detectionButtonImage;
    [SerializeField] private Button detectionButton;

    [SerializeField] private Color readyColor = Color.white;
    [SerializeField] private Color cooldownColor = Color.gray;

    private int pendingClues;

    private bool journalOpen;
    private bool dialogueOpen;

    private void Start()
    {
        TrySubscribe();
        ResetClueNotifications();

        HandleDetectionCooldown(false);
    }

    private void OnEnable()
    {
        DialogueManager.OnDialogueStateChanged += HandleDialogue;
        SpectralDetectionSystem.OnCooldownStateChanged += HandleDetectionCooldown;
    }

    private void OnDisable()
    {
        DialogueManager.OnDialogueStateChanged -= HandleDialogue;
        SpectralDetectionSystem.OnCooldownStateChanged -= HandleDetectionCooldown;
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

    // ---------------- DETECTION UI ----------------

    private void HandleDetectionCooldown(bool isCooldown)
    {
        if (detectionButtonImage != null)
            detectionButtonImage.color = isCooldown ? cooldownColor : readyColor;

        if (detectionButton != null)
            detectionButton.interactable = !isCooldown;
    }

    // ---------------- HUD VISIBILITY ----------------

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

    // ---------------- CLUES ----------------

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