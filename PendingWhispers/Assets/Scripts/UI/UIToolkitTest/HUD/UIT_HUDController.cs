using System.Collections;
using Inventory.Model;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UIElements;

[RequireComponent(typeof(UIDocument))]
public class UIT_HUDController : MonoBehaviour
{
    // Nombres del UXML
    private const string HudRootName = "HUD_Root";
    private const string NotebookButtonName = "HUD_Notebook_Button";
    private const string NotebookNotifName = "HUD_Notebook_Notification";
    private const string NotebookNotifTextName = "T_AddedClues_Notification";
    private const string MapButtonName = "HUD_Map_Button";
    private const string AbilityButtonName = "HUD_Ability_Button";
    private const string GameNotifName = "HUD_GameNotification";
    private const string GameNotifTextName = "T_GameNotification";

    // Clase USS que activa las transiciones
    private const string VisibleClass = "visible";

    [Header("UI Document")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Progress Flags")]
    [SerializeField] private FlagSO unlockCatacombsFlag;
    [SerializeField] private FlagSO catacombsEnteredFlag;

    [Header("Event Channels")]
    [SerializeField] private TestimonyEventChannelSO onTestimonyRegistered;
    [SerializeField] private BoolEventChannelSO dialogueStateChannel;
    [SerializeField] private BoolEventChannelSO journalStateChannel;

    [Header("Button Actions")]
    [SerializeField] private UnityEvent onNotebookClicked;
    [SerializeField] private UnityEvent onMapClicked;
    [SerializeField] private UnityEvent onAbilityClicked;

    [Header("Detection Button")]
    [SerializeField] private Color cooldownColor = Color.gray;

    [Header("Game Notification")]
    [SerializeField] private float gameNotificationDuration = 3f;
    [SerializeField] private BoolEventChannelSO pauseStateChannel;
    private bool pauseOpen;

    // Elementos
    private VisualElement hudRoot;
    private Button notebookButton;
    private VisualElement clueNotification;
    private Label clueNotificationText;
    private Button mapButton;
    private Button abilityButton;
    private VisualElement gameNotification;
    private Label gameNotificationText;

    private int pendingClues;
    private bool journalOpen;
    private bool dialogueOpen;
    private bool isDetectionCooldown;

    private Coroutine gameNotificationRoutine;

    private void Reset()
    {
        uiDocument = GetComponent<UIDocument>();
    }

    private void Awake()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        // UIDocument reconstruye el árbol al activarse: hay que volver a buscar los elementos
        BindUI();

        dialogueStateChannel.OnRaised += HandleDialogue;
        journalStateChannel.OnRaised += HandleJournal;
        SpectralDetectionSystem.OnCooldownStateChanged += HandleDetectionCooldown;

        if (onTestimonyRegistered != null)
            onTestimonyRegistered.OnRaised += HandleTestimonyRegistered;

        if (GameProgress.Instance != null)
            GameProgress.Instance.OnFlagAdded += OnFlagAdded;

        UIGameEvents.OnEvidenceRegistered += HandleEvidenceRegistered;
        if (pauseStateChannel != null) pauseStateChannel.OnRaised += HandlePause;

        ApplyCurrentState();
    }

    private void OnDisable()
    {
        UnbindUI();

        dialogueStateChannel.OnRaised -= HandleDialogue;
        journalStateChannel.OnRaised -= HandleJournal;
        SpectralDetectionSystem.OnCooldownStateChanged -= HandleDetectionCooldown;

        if (onTestimonyRegistered != null)
            onTestimonyRegistered.OnRaised -= HandleTestimonyRegistered;

        if (GameProgress.Instance != null)
            GameProgress.Instance.OnFlagAdded -= OnFlagAdded;

        UIGameEvents.OnEvidenceRegistered -= HandleEvidenceRegistered;

        if (gameNotificationRoutine != null)
        {
            StopCoroutine(gameNotificationRoutine);
            gameNotificationRoutine = null;
        }
        if (pauseStateChannel != null) pauseStateChannel.OnRaised -= HandlePause;
    }

    private void Start()
    {
        ResetClueNotifications();
        HandleDetectionCooldown(false);
        ApplyCurrentState();
    }
    private void HandlePause(bool open) { pauseOpen = open; RefreshHUD(); }

    // ---------------- BINDING ----------------

    private void BindUI()
    {
        VisualElement root = uiDocument.rootVisualElement;

        hudRoot = root.Q<VisualElement>(HudRootName);
        notebookButton = root.Q<Button>(NotebookButtonName);
        clueNotification = root.Q<VisualElement>(NotebookNotifName);
        clueNotificationText = root.Q<Label>(NotebookNotifTextName);
        mapButton = root.Q<Button>(MapButtonName);
        abilityButton = root.Q<Button>(AbilityButtonName);
        gameNotification = root.Q<VisualElement>(GameNotifName);
        gameNotificationText = root.Q<Label>(GameNotifTextName);

        // Los contenedores ocupan toda la pantalla: que solo los botones capturen clics,
        // para no bloquear el point-and-click del mundo.
        root.Query<VisualElement>().ForEach(e =>
        {
            if (e is not Button)
                e.pickingMode = PickingMode.Ignore;
        });

        if (notebookButton != null) notebookButton.clicked += OnNotebookClicked;
        if (mapButton != null) mapButton.clicked += OnMapClicked;
        if (abilityButton != null) abilityButton.clicked += OnAbilityClicked;
    }

    private void UnbindUI()
    {
        if (notebookButton != null) notebookButton.clicked -= OnNotebookClicked;
        if (mapButton != null) mapButton.clicked -= OnMapClicked;
        if (abilityButton != null) abilityButton.clicked -= OnAbilityClicked;
    }

    private void OnNotebookClicked() => onNotebookClicked?.Invoke();
    private void OnMapClicked() => onMapClicked?.Invoke();
    private void OnAbilityClicked() => onAbilityClicked?.Invoke();

    private void ApplyCurrentState()
    {
        RefreshHUD();
        RefreshClueNotification();
        ApplyDetectionCooldown();
        RefreshMapButton();
        RefreshDetectionAvailability();
    }

    // ---------------- DETECTION UI ----------------

    private void HandleDetectionCooldown(bool isCooldown)
    {
        isDetectionCooldown = isCooldown;
        ApplyDetectionCooldown();
    }

    private void ApplyDetectionCooldown()
    {
        if (abilityButton == null)
            return;

        abilityButton.SetEnabled(!isDetectionCooldown);

        // En cooldown se fuerza el tinte; al terminar se limpia el inline
        // para que vuelvan a mandar el USS y su :hover.
        abilityButton.style.unityBackgroundImageTintColor = isDetectionCooldown
            ? new StyleColor(cooldownColor)
            : new StyleColor(StyleKeyword.Null);
    }

    private void RefreshDetectionAvailability()
    {
        if (abilityButton == null)
            return;

        bool available = catacombsEnteredFlag == null
                         || (GameProgress.Instance != null && GameProgress.Instance.HasFlag(catacombsEnteredFlag));

        abilityButton.style.display = available ? DisplayStyle.Flex : DisplayStyle.None;
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
        hudRoot.style.display = (!journalOpen && !dialogueOpen && !pauseOpen) ? DisplayStyle.Flex : DisplayStyle.None;
        if (hudRoot != null)
            hudRoot.style.display = (!journalOpen && !dialogueOpen) ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // ---------------- CLUES ----------------

    private void HandleTestimonyRegistered(TestimonyEntry entry) => AddClueNotification();

    private void HandleEvidenceRegistered(ItemSO item) => AddClueNotification();

    public void AddClueNotification()
    {
        pendingClues++;
        RefreshClueNotification();
    }

    public void ResetClueNotifications()
    {
        pendingClues = 0;
        RefreshClueNotification();
    }

    private void RefreshClueNotification()
    {
        bool hasClues = pendingClues > 0;

        clueNotification?.EnableInClassList(VisibleClass, hasClues);

        if (clueNotificationText != null)
            clueNotificationText.text = hasClues ? pendingClues.ToString() : string.Empty;
    }

    // ---------------- MAP ----------------

    private void OnFlagAdded(FlagSO flag)
    {
        if (flag == unlockCatacombsFlag)
            RefreshMapButton();

        if (flag == catacombsEnteredFlag)
            RefreshDetectionAvailability();
    }

    private void RefreshMapButton()
    {
        if (mapButton == null || GameProgress.Instance == null)
            return;

        mapButton.SetEnabled(GameProgress.Instance.HasFlag(unlockCatacombsFlag));
    }

    // ---------------- GAME NOTIFICATION ----------------

    public void ShowGameNotification(string message)
    {
        if (gameNotification == null)
            return;

        if (gameNotificationText != null)
            gameNotificationText.text = message;

        if (gameNotificationRoutine != null)
            StopCoroutine(gameNotificationRoutine);

        gameNotificationRoutine = StartCoroutine(GameNotificationRoutine());
    }

    private IEnumerator GameNotificationRoutine()
    {
        gameNotification.AddToClassList(VisibleClass);
        yield return new WaitForSeconds(gameNotificationDuration);
        gameNotification.RemoveFromClassList(VisibleClass);
        gameNotificationRoutine = null;
    }
}