using System.Collections;
using Inventory.Model;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
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
    [Tooltip("Flag que desbloquea el mapa. Debe añadirse al terminar la conversación con el primer fantasma del Caso 1.")]
    [FormerlySerializedAs("unlockCatacombsFlag")]
    [SerializeField] private FlagSO mapUnlockedFlag;
    [SerializeField] private FlagSO catacombsEnteredFlag;

    [Header("Event Channels")]
    [SerializeField] private TestimonyEventChannelSO onTestimonyRegistered;
    [SerializeField] private BoolEventChannelSO dialogueStateChannel;
    [SerializeField] private BoolEventChannelSO journalStateChannel;
    [SerializeField] private BoolEventChannelSO pauseStateChannel;

    [Header("Button Actions")]
    [SerializeField] private UnityEvent onNotebookClicked;
    [SerializeField] private UnityEvent onMapClicked;
    [SerializeField] private UnityEvent onAbilityClicked;

    [Header("Detection Button")]
    [SerializeField] private Color cooldownColor = Color.gray;

    [Header("Game Notification")]
    [SerializeField] private float gameNotificationDuration = 3f;

    [Header("Map Reveal Animation")]
    [Tooltip("Espera tras reaparecer el HUD antes de mostrar el botón, para que el jugador lo vea aparecer.")]
    [SerializeField] private float mapRevealDelay = 0.25f;
    [SerializeField] private float mapRevealDuration = 0.5f;
    [SerializeField] private float mapRevealOvershoot = 1.3f;

    [Header("Map Tutorial (opcional)")]
    [SerializeField] private string mapTutorialID = "map";
    [SerializeField] private string mapTutorialTitle = "Map";
    [TextArea(2, 4)]
    [SerializeField] private string mapTutorialText = "";

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
    private bool pauseOpen;
    private bool isDetectionCooldown;

    private bool mapRevealPending;

    private Coroutine gameNotificationRoutine;
    private Coroutine mapRevealRoutine;

    private bool IsHudVisible => !journalOpen && !dialogueOpen && !pauseOpen;

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

        if (pauseStateChannel != null)
            pauseStateChannel.OnRaised += HandlePause;

        if (onTestimonyRegistered != null)
            onTestimonyRegistered.OnRaised += HandleTestimonyRegistered;

        if (GameProgress.Instance != null)
            GameProgress.Instance.OnFlagAdded += OnFlagAdded;

        UIGameEvents.OnEvidenceRegistered += HandleEvidenceRegistered;

        ApplyCurrentState();
    }

    private void OnDisable()
    {
        UnbindUI();

        dialogueStateChannel.OnRaised -= HandleDialogue;
        journalStateChannel.OnRaised -= HandleJournal;
        SpectralDetectionSystem.OnCooldownStateChanged -= HandleDetectionCooldown;

        if (pauseStateChannel != null)
            pauseStateChannel.OnRaised -= HandlePause;

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

        // Si se desactiva a mitad de la animación del mapa, se repetirá al reactivarse.
        if (mapRevealRoutine != null)
        {
            StopCoroutine(mapRevealRoutine);
            mapRevealRoutine = null;
            mapRevealPending = true;
        }
    }

    private void Start()
    {
        ResetClueNotifications();
        HandleDetectionCooldown(false);
        ApplyCurrentState();
    }

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
        RefreshMapButton();
        RefreshHUD();
        RefreshClueNotification();
        ApplyDetectionCooldown();
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

    private void HandlePause(bool open)
    {
        pauseOpen = open;
        RefreshHUD();
    }

    private void RefreshHUD()
    {
        if (hudRoot == null)
            return;

        hudRoot.style.display = IsHudVisible ? DisplayStyle.Flex : DisplayStyle.None;

        // El flag del mapa llega durante el diálogo del fantasma, con el HUD oculto.
        // La animación espera a que el HUD vuelva a verse.
        TryPlayPendingMapReveal();
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
        if (flag == mapUnlockedFlag)
        {
            mapRevealPending = true;
            RefreshMapButton();
            TryPlayPendingMapReveal();
        }

        if (flag == catacombsEnteredFlag)
            RefreshDetectionAvailability();
    }

    private bool IsMapUnlocked()
    {
        return GameProgress.Instance != null && GameProgress.Instance.HasFlag(mapUnlockedFlag);
    }

    private void RefreshMapButton()
    {
        if (mapButton == null || mapRevealRoutine != null)
            return;

        // Mientras la revelación esté pendiente, el botón sigue oculto.
        SetMapButtonVisible(IsMapUnlocked() && !mapRevealPending);
    }

    private void SetMapButtonVisible(bool visible)
    {
        // visibility (no display) para que el resto de botones del HUD no se recoloquen al aparecer.
        // Un elemento con visibility: hidden tampoco recibe clics.
        mapButton.style.visibility = visible ? Visibility.Visible : Visibility.Hidden;
        mapButton.SetEnabled(visible);
        mapButton.style.scale = new StyleScale(StyleKeyword.Null);
    }

    private void TryPlayPendingMapReveal()
    {
        if (!mapRevealPending || !IsHudVisible || mapButton == null || mapRevealRoutine != null)
            return;

        mapRevealPending = false;
        mapRevealRoutine = StartCoroutine(MapRevealRoutine());
    }

    private IEnumerator MapRevealRoutine()
    {
        mapButton.style.visibility = Visibility.Hidden;

        if (mapRevealDelay > 0f)
            yield return new WaitForSeconds(mapRevealDelay);

        SetMapScale(0f);
        mapButton.style.visibility = Visibility.Visible;
        mapButton.SetEnabled(true);

        // Fase 1 (60%): 0 -> overshoot con ease-out. Fase 2 (40%): overshoot -> 1.
        float growTime = mapRevealDuration * 0.6f;
        float settleTime = mapRevealDuration - growTime;

        yield return ScaleRoutine(0f, mapRevealOvershoot, growTime, EaseOutCubic);
        yield return ScaleRoutine(mapRevealOvershoot, 1f, settleTime, EaseInOutQuad);

        // Se limpia el inline para que vuelva a mandar el USS (p. ej. escala en :hover).
        mapButton.style.scale = new StyleScale(StyleKeyword.Null);
        mapRevealRoutine = null;

        if (!string.IsNullOrEmpty(mapTutorialText) && TutorialPopup.Instance != null)
            TutorialPopup.Instance.ShowTutorialOnce(mapTutorialID, mapTutorialTitle, mapTutorialText);
    }

    private IEnumerator ScaleRoutine(float from, float to, float duration, System.Func<float, float> ease)
    {
        float t = 0f;
        while (t < duration)
        {
            // Tiempo escalado: si se pausa el juego, la animación se congela.
            t += Time.deltaTime;
            SetMapScale(Mathf.LerpUnclamped(from, to, ease(Mathf.Clamp01(t / duration))));
            yield return null;
        }
    }

    private void SetMapScale(float s)
    {
        mapButton.style.scale = new StyleScale(new Scale(new Vector2(s, s)));
    }

    private static float EaseOutCubic(float x) => 1f - Mathf.Pow(1f - x, 3f);
    private static float EaseInOutQuad(float x) => x < 0.5f ? 2f * x * x : 1f - Mathf.Pow(-2f * x + 2f, 2f) / 2f;

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