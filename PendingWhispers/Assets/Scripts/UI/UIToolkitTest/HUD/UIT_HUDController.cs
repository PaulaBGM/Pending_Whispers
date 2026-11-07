using System.Collections;
using System.Collections.Generic;
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

    // Clases USS que activan las transiciones
    private const string VisibleClass = "visible";
    private const string BumpClass = "bump";

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

    [Header("Clue Notifications (lengüeta)")]
    [Tooltip("A partir de este número se muestra 'N+'.")]
    [SerializeField] private int maxDisplayedClues = 9;
    [Tooltip("Duración del pulso (.bump) cuando sube el contador.")]
    [SerializeField] private float bumpDuration = 0.2f;

    [Header("Pruebas (inventario)")]
    [Tooltip("Tipos de objeto que cuentan como 'cosa nueva' al entrar en el inventario.")]
    [SerializeField] private List<ItemType> notifiedItemTypes = new() { ItemType.Clue, ItemType.Item };

    [Header("Game Notification")]
    [SerializeField] private float gameNotificationDuration = 3f;
    [Tooltip("Lo que dura la transición de salida en el USS antes de ocultarla con display: none.")]
    [SerializeField] private float gameNotificationHideTime = 0.3f;
    [Tooltip("{0} = nombre de la persona.")]
    [SerializeField] private string testimonyMessage = "New testimony: {0}";
    [Tooltip("{0} = nombre de la prueba.")]
    [SerializeField] private string evidenceMessage = "New evidence: {0}";

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

    // Mensajes que esperan a que el HUD vuelva a verse (p. ej. testimonios recogidos durante un diálogo).
    private readonly Queue<string> gameNotificationQueue = new();

    // Objetos que ya estaban en el inventario: solo los que no estén aquí cuentan como nuevos.
    private readonly HashSet<ItemSO> knownItems = new();
    private InventorySO watchedInventory;

    private Coroutine gameNotificationRoutine;
    private string currentGameNotification;
    private Coroutine mapRevealRoutine;
    private IVisualElementScheduledItem bumpReset;

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
        WatchInventory();

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
        UnwatchInventory();

        if (gameNotificationRoutine != null)
        {
            StopCoroutine(gameNotificationRoutine);
            gameNotificationRoutine = null;
        }
        currentGameNotification = null;

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
        // Por si InventoryRuntime no existía aún en OnEnable.
        WatchInventory();
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

        // Como el HUDController antiguo (SetActive): la lengüeta empieza escondida.
        // Se usa display en vez de clases USS para que no dependa de estilos.
        if (clueNotification != null)
        {
            clueNotification.style.visibility = Visibility.Visible;
            clueNotification.style.display = DisplayStyle.None;

            // En el UXML la lengüeta va ANTES que el botón del cuaderno, así que el botón se pinta
            // encima y la tapa. Se pasa al final de su contenedor para que se dibuje por delante.
            clueNotification.BringToFront();
        }

        if (gameNotification != null)
        {
            gameNotification.style.display = DisplayStyle.None;
            gameNotification.RemoveFromClassList(VisibleClass);
        }

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

        // Abrir el diario = el jugador ya ha ido a ver lo nuevo: se reinicia la cuenta
        // y se descartan los avisos que estaban esperando (ya no aportan nada).
        if (open)
        {
            ResetClueNotifications();
            gameNotificationQueue.Clear();
            HideGameNotificationImmediate();
        }

        RefreshHUD();
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

        // Igual con los avisos: los testimonios llegan durante el diálogo.
        TryPlayNextGameNotification();
    }

    // ---------------- CLUES (lengüeta + contador) ----------------

    private void HandleTestimonyRegistered(TestimonyEntry entry)
    {
        Debug.Log($"[HUD] Nuevo testimonio: {entry.Name}");
        AddClueNotification();

        // Un diálogo suele registrar varias frases importantes de la misma persona: el aviso
        // sale una sola vez por persona (no se repite si ya está en pantalla o en cola).
        ShowGameNotification(string.Format(testimonyMessage, entry.Name), skipIfAlreadyPending: true);
    }

    private void HandleEvidenceRegistered(ItemSO item) => RegisterNewItem(item);

    /// <summary>
    /// Una prueba nueva. Llega por el inventario (siempre) o por UIGameEvents (si algo lo lanza);
    /// knownItems evita contarla dos veces.
    /// </summary>
    private void RegisterNewItem(ItemSO item)
    {
        if (item == null || !knownItems.Add(item))
            return;

        if (!notifiedItemTypes.Contains(item.ItemType))
            return;

        Debug.Log($"[HUD] Nueva prueba: {item.Name}");
        AddClueNotification();
        ShowGameNotification(string.Format(evidenceMessage, item.Name));
    }

    // ---------------- INVENTORY WATCH ----------------

    private void WatchInventory()
    {
        if (watchedInventory != null)
            return;

        InventorySO inventory = InventoryRuntime.Instance != null ? InventoryRuntime.Instance.GetInventory() : null;
        if (inventory == null)
            return;

        watchedInventory = inventory;

        // Lo que ya hay al entrar en la escena (partida cargada, escena anterior) no es nuevo.
        knownItems.Clear();
        foreach (var kvp in inventory.GetCurrentInventoryState())
        {
            if (!kvp.Value.IsEmpty && kvp.Value.item != null)
                knownItems.Add(kvp.Value.item);
        }

        watchedInventory.OnInventoryUpdated += HandleInventoryUpdated;
    }

    private void UnwatchInventory()
    {
        if (watchedInventory != null)
            watchedInventory.OnInventoryUpdated -= HandleInventoryUpdated;

        watchedInventory = null;
    }

    private void HandleInventoryUpdated(Dictionary<int, InventoryItem> state)
    {
        if (state == null)
            return;

        // Al cargar partida el inventario se rellena de golpe: no son pistas nuevas.
        bool restoring = SaveSystem.Instance != null && SaveSystem.Instance.IsRestoring;

        foreach (var kvp in state)
        {
            if (kvp.Value.IsEmpty || kvp.Value.item == null)
                continue;

            if (restoring)
                knownItems.Add(kvp.Value.item);
            else
                RegisterNewItem(kvp.Value.item);
        }
    }

    public void AddClueNotification()
    {
        // Si el diario está abierto, el jugador ya lo está viendo: no cuenta como pendiente.
        if (journalOpen)
            return;

        pendingClues++;
        RefreshClueNotification();

        if (clueNotification == null)
        {
            Debug.LogWarning($"[HUD] No se encontró '{NotebookNotifName}' en el UXML: la lengüeta no se puede mostrar.", this);
            return;
        }

        // Diagnóstico: un frame después (con el layout ya calculado) dice cómo se está pintando.
        clueNotification.schedule.Execute(LogClueNotificationState).StartingIn(50);
        PlayBump();
    }

    public void ResetClueNotifications()
    {
        pendingClues = 0;
        RefreshClueNotification();
    }

    private void RefreshClueNotification()
    {
        bool hasClues = pendingClues > 0;

        // Equivalente a clueNotification.SetActive(hasClues) del HUD antiguo.
        if (clueNotification != null)
        {
            clueNotification.style.display = hasClues ? DisplayStyle.Flex : DisplayStyle.None;
            clueNotification.EnableInClassList(VisibleClass, hasClues); // por si el USS quiere animarla
        }

        if (clueNotificationText != null)
        {
            clueNotificationText.text = !hasClues
                ? string.Empty
                : pendingClues > maxDisplayedClues
                    ? $"{maxDisplayedClues}+"
                    : pendingClues.ToString();
        }
    }

    private void LogClueNotificationState()
    {
        if (clueNotification == null)
            return;

        var s = clueNotification.resolvedStyle;
        string chain = "";
        for (var e = clueNotification; e != null; e = e.parent)
        {
            var rs = e.resolvedStyle;
            if (rs.display == DisplayStyle.None || rs.visibility == Visibility.Hidden || rs.opacity <= 0.01f)
                chain += $"\n   - '{e.name}' display={rs.display} visibility={rs.visibility} opacity={rs.opacity}";
        }

        Debug.Log($"[HUD] Lengüeta: display={s.display}, visibility={s.visibility}, opacity={s.opacity}, " +
                  $"tamaño={s.width}x{s.height}, posición={clueNotification.worldBound.position}, texto='{clueNotificationText?.text}', " +
                  $"clases=[{string.Join(" ", clueNotification.GetClasses())}]" +
                  (chain.Length > 0 ? $"\n  Elementos que la ocultan:{chain}" : "\n  Ningún padre la oculta."), this);
    }

    /// <summary>Pulso de la lengüeta al sumar una pista (clase .bump un instante).</summary>
    private void PlayBump()
    {
        if (clueNotification == null || bumpDuration <= 0f)
            return;

        bumpReset?.Pause();
        clueNotification.AddToClassList(BumpClass);
        bumpReset = clueNotification.schedule
            .Execute(() => clueNotification.RemoveFromClassList(BumpClass))
            .StartingIn((long)(bumpDuration * 1000f));
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

    /// <summary>
    /// Pone un aviso en cola. Se muestran de uno en uno, y solo con el HUD visible:
    /// los que llegan durante un diálogo esperan a que termine.
    /// </summary>
    public void ShowGameNotification(string message) => ShowGameNotification(message, false);

    public void ShowGameNotification(string message, bool skipIfAlreadyPending)
    {
        if (gameNotification == null || string.IsNullOrWhiteSpace(message))
            return;

        if (skipIfAlreadyPending && (message == currentGameNotification || gameNotificationQueue.Contains(message)))
            return;

        gameNotificationQueue.Enqueue(message);
        TryPlayNextGameNotification();
    }

    private void TryPlayNextGameNotification()
    {
        if (gameNotificationRoutine != null || gameNotificationQueue.Count == 0 || !IsHudVisible)
            return;

        gameNotificationRoutine = StartCoroutine(GameNotificationRoutine(gameNotificationQueue.Dequeue()));
    }

    private IEnumerator GameNotificationRoutine(string message)
    {
        currentGameNotification = message;

        if (gameNotificationText != null)
            gameNotificationText.text = message;

        // display primero, la clase un frame después: si no, la transición de entrada no se ve.
        gameNotification.style.display = DisplayStyle.Flex;
        gameNotification.SetEnabled(true);
        yield return null;
        gameNotification.AddToClassList(VisibleClass);

        yield return new WaitForSeconds(gameNotificationDuration);

        gameNotification.RemoveFromClassList(VisibleClass);
        yield return new WaitForSeconds(gameNotificationHideTime);

        gameNotification.style.display = DisplayStyle.None;
        gameNotificationRoutine = null;
        currentGameNotification = null;

        TryPlayNextGameNotification();
    }

    private void HideGameNotificationImmediate()
    {
        if (gameNotificationRoutine != null)
        {
            StopCoroutine(gameNotificationRoutine);
            gameNotificationRoutine = null;
        }

        currentGameNotification = null;

        if (gameNotification == null)
            return;

        gameNotification.RemoveFromClassList(VisibleClass);
        gameNotification.style.display = DisplayStyle.None;
    }
}