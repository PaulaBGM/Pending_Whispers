using Inventory.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HUDController : MonoBehaviour
{
    [Header("HUD")]
    [SerializeField] private GameObject hudRoot;
    [SerializeField] private Button mapButton;
    [SerializeField] private FlagSO unlockCatacombsFlag;

    [Header("Notifications")]
    [SerializeField] private TestimonyEventChannelSO onTestimonyRegistered;
    [SerializeField] private BoolEventChannelSO dialogueStateChannel;
    [SerializeField] private BoolEventChannelSO journalStateChannel; // nuevo
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
        ResetClueNotifications();

        HandleDetectionCooldown(false);
        RefreshMapButton();
    }

    private void OnEnable()
    {
        dialogueStateChannel.OnRaised += HandleDialogue;
        journalStateChannel.OnRaised += HandleJournal; // antes: SubscribeToUIManager()
        SpectralDetectionSystem.OnCooldownStateChanged += HandleDetectionCooldown;
        if (onTestimonyRegistered != null)
            onTestimonyRegistered.OnRaised += HandleTestimonyRegistered;
        if (GameProgress.Instance != null)
            GameProgress.Instance.OnFlagAdded += OnFlagAdded;

        UIGameEvents.OnEvidenceRegistered += HandleEvidenceRegistered;
    }

    private void HandleEvidenceRegistered(ItemSO item) => AddClueNotification();

    private void OnDisable()
    {
        dialogueStateChannel.OnRaised -= HandleDialogue;
        journalStateChannel.OnRaised -= HandleJournal; // antes: desuscripción de UIManager
        SpectralDetectionSystem.OnCooldownStateChanged -= HandleDetectionCooldown;

        if (onTestimonyRegistered != null)
            onTestimonyRegistered.OnRaised -= HandleTestimonyRegistered;

        if (GameProgress.Instance != null)
            GameProgress.Instance.OnFlagAdded -= OnFlagAdded;

        UIGameEvents.OnEvidenceRegistered -= HandleEvidenceRegistered;
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

    private void HandleTestimonyRegistered(TestimonyEntry entry)
    {
        AddClueNotification();
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

    private void OnFlagAdded(FlagSO flag)
    {
        if (flag == unlockCatacombsFlag)
            RefreshMapButton();
    }

    private void RefreshMapButton()
    {
        if (mapButton != null)
            mapButton.interactable = GameProgress.Instance.HasFlag(unlockCatacombsFlag);
    }
}