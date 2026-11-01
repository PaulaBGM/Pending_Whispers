using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// =========================================================
// Menú de pausa (UI Toolkit).
// Flujo: Pausa -> Save and exit -> ¿Seguro? -> Sí -> guardar -> mensaje -> menú principal
//                                           -> No -> vuelve a Pausa
//
// Las animaciones las define el USS con :disabled, así que mostrar un panel
// es SetEnabled(true) y ocultarlo SetEnabled(false). Las transiciones de
// UI Toolkit y su scheduler van en tiempo real, no les afecta Time.timeScale = 0.
// =========================================================

[RequireComponent(typeof(UIDocument))]
public class UIT_PauseMenuController : MonoBehaviour
{
    // Nombres del UXML
    private const string RootName = "PauseSaveMenu_Root";
    private const string PausePanelName = "VE_PauseSaveMenu_BG";
    private const string ConfirmPanelName = "VE_ConfirmationMenu_BG";
    private const string FinalPanelName = "VE_FinalConfirmationMenu_BG";
    private const string ResumeButtonName = "BT_Up_PauseSave";
    private const string SaveExitButtonName = "BT_Down_PauseSave";
    private const string YesButtonName = "BT_Up_Confirmation";
    private const string NoButtonName = "BT_Down_Confirmation";
    private const string FinalLabelName = "T_FinalConfirmation";

    [Header("UI Document")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Event Channels")]
    [Tooltip("Se lanza true/false al abrir/cerrar la pausa (lo escucha el HUD).")]
    [SerializeField] private BoolEventChannelSO pauseStateChannel;
    [Tooltip("Con el diario abierto no se puede pausar.")]
    [SerializeField] private BoolEventChannelSO journalStateChannel;

#if ENABLE_INPUT_SYSTEM
    [Header("Input")]
    [Tooltip("Opcional. Acción de pausa/atrás (p. ej. Escape). También puedes llamar a HandleBackInput() desde tu sistema de input.")]
    [SerializeField] private InputActionReference pauseAction;
#endif

    [Header("Save & Exit")]
    [SerializeField] private int saveSlot = 0;
    [SerializeField] private string mainMenuSceneName = "MainMenu";
    [SerializeField] private string savedText = "You saved the game";
    [SerializeField] private string saveFailedText = "The game could not be saved";

    [Header("Timing (segundos, tiempo real)")]
    [Tooltip("Lo que tarda en salir un panel antes de que entre el siguiente.")]
    [SerializeField] private float panelTransitionTime = 0.6f;
    [Tooltip("Tiempo que se muestra el mensaje de guardado antes de salir.")]
    [SerializeField] private float finalMessageTime = 1.5f;

    // Elementos
    private VisualElement root;
    private VisualElement pausePanel;
    private VisualElement confirmPanel;
    private VisualElement finalPanel;
    private Button resumeButton;
    private Button saveExitButton;
    private Button yesButton;
    private Button noButton;
    private Label finalLabel;

    public bool IsPaused { get; private set; }

    private float timeScaleBeforePause = 1f;
    private bool isBusy;            // transición o guardado en curso
    private bool journalOpen;
    private int journalClosedFrame = -1;

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
        BindUI();

        if (journalStateChannel != null)
            journalStateChannel.OnRaised += HandleJournal;

        SceneManager.sceneLoaded += HandleSceneLoaded;

#if ENABLE_INPUT_SYSTEM
        if (pauseAction != null)
        {
            pauseAction.action.performed += OnPauseActionPerformed;
            pauseAction.action.Enable();
        }
#endif
    }

    private void OnDisable()
    {
        UnbindUI();

        if (journalStateChannel != null)
            journalStateChannel.OnRaised -= HandleJournal;

        SceneManager.sceneLoaded -= HandleSceneLoaded;

#if ENABLE_INPUT_SYSTEM
        if (pauseAction != null)
            pauseAction.action.performed -= OnPauseActionPerformed;
#endif
    }

    private void OnDestroy()
    {
        // Nunca dejar el juego congelado si este objeto desaparece en pausa.
        if (IsPaused)
            Time.timeScale = 1f;
    }

    // =========================================================
    // BINDING
    // =========================================================

    private void BindUI()
    {
        VisualElement documentRoot = uiDocument.rootVisualElement;

        root = documentRoot.Q<VisualElement>(RootName);
        pausePanel = documentRoot.Q<VisualElement>(PausePanelName);
        confirmPanel = documentRoot.Q<VisualElement>(ConfirmPanelName);
        finalPanel = documentRoot.Q<VisualElement>(FinalPanelName);
        resumeButton = documentRoot.Q<Button>(ResumeButtonName);
        saveExitButton = documentRoot.Q<Button>(SaveExitButtonName);
        yesButton = documentRoot.Q<Button>(YesButtonName);
        noButton = documentRoot.Q<Button>(NoButtonName);
        finalLabel = documentRoot.Q<Label>(FinalLabelName);

        // Estado inicial: todo oculto y sin bloquear clics.
        HideImmediate(pausePanel);
        HideImmediate(confirmPanel);
        HideImmediate(finalPanel);

        if (root != null)
            root.style.display = DisplayStyle.None;

        if (resumeButton != null) resumeButton.clicked += Resume;
        if (saveExitButton != null) saveExitButton.clicked += OnSaveAndExitClicked;
        if (yesButton != null) yesButton.clicked += OnYesClicked;
        if (noButton != null) noButton.clicked += OnNoClicked;
    }

    private void UnbindUI()
    {
        if (resumeButton != null) resumeButton.clicked -= Resume;
        if (saveExitButton != null) saveExitButton.clicked -= OnSaveAndExitClicked;
        if (yesButton != null) yesButton.clicked -= OnYesClicked;
        if (noButton != null) noButton.clicked -= OnNoClicked;
    }

    // =========================================================
    // INPUT
    // =========================================================

#if ENABLE_INPUT_SYSTEM
    private void OnPauseActionPerformed(InputAction.CallbackContext _) => HandleBackInput();
#endif

    /// <summary>Escape / atrás: abre la pausa, la cierra o cancela la confirmación.</summary>
    public void HandleBackInput()
    {
        if (isBusy)
            return;

        if (confirmPanel != null && confirmPanel.enabledSelf)
        {
            OnNoClicked();
            return;
        }

        if (IsPaused)
            Resume();
        else
            Pause();
    }

    private void HandleJournal(bool open)
    {
        journalOpen = open;

        // Si el mismo Escape cerró el diario, que no abra también la pausa.
        if (!open)
            journalClosedFrame = Time.frameCount;
    }

    // =========================================================
    // PAUSE / RESUME
    // =========================================================

    public void Pause()
    {
        if (IsPaused || isBusy || root == null)
            return;

        if (journalOpen || journalClosedFrame == Time.frameCount)
            return;

        // Si el menú persiste entre escenas, que no se pueda pausar en el menú principal.
        if (SceneManager.GetActiveScene().name == mainMenuSceneName)
            return;

        IsPaused = true;

        timeScaleBeforePause = Time.timeScale;
        Time.timeScale = 0f;

        root.style.display = DisplayStyle.Flex;
        ShowPanel(pausePanel);

        if (pauseStateChannel != null)
            pauseStateChannel.Raise(true);
    }

    public void Resume()
    {
        if (!IsPaused || isBusy)
            return;

        IsPaused = false;
        Time.timeScale = timeScaleBeforePause;

        HidePanel(pausePanel);

        root.schedule.Execute(() =>
        {
            if (!IsPaused)
                root.style.display = DisplayStyle.None;
        }).StartingIn(ToMs(panelTransitionTime));

        if (pauseStateChannel != null)
            pauseStateChannel.Raise(false);
    }

    // =========================================================
    // SAVE & EXIT
    // =========================================================

    private void OnSaveAndExitClicked()
    {
        if (isBusy)
            return;

        SwitchPanel(pausePanel, confirmPanel);
    }

    private void OnNoClicked()
    {
        if (isBusy)
            return;

        SwitchPanel(confirmPanel, pausePanel);
    }

    private void OnYesClicked()
    {
        if (isBusy)
            return;

        bool saved = SaveSystem.Instance != null && SaveSystem.Instance.Save(saveSlot);

        if (finalLabel != null)
            finalLabel.text = saved ? savedText : saveFailedText;

        SwitchPanel(confirmPanel, finalPanel, onShown: () =>
        {
            root.schedule.Execute(() =>
            {
                if (saved)
                    ExitToMainMenu();
                else
                    SwitchPanel(finalPanel, pausePanel); // si falla, vuelve a la pausa
            }).StartingIn(ToMs(finalMessageTime));
        });
    }

    private void ExitToMainMenu()
    {
        // isBusy se queda a true: no se acepta más input mientras cambia la escena.
        // El mensaje sigue visible durante la transición y el menú se cierra
        // del todo en HandleSceneLoaded, cuando la nueva escena ya está cargada.
        Time.timeScale = 1f;

        if (SceneController.Instance != null)
            SceneController.Instance.LoadScene(mainMenuSceneName);
        else
            SceneManager.LoadScene(mainMenuSceneName);
    }

    // =========================================================
    // SCENE CHANGES
    // =========================================================

    // Si este objeto sobrevive al cambio de escena (DontDestroyOnLoad o hijo de
    // un objeto persistente), al cargar cualquier escena el menú vuelve a su estado cerrado.
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        ResetToClosed();
    }

    private void ResetToClosed()
    {
        bool wasOpen = IsPaused || isBusy;

        IsPaused = false;
        isBusy = false;

        HideImmediate(pausePanel);
        HideImmediate(confirmPanel);
        HideImmediate(finalPanel);

        if (root != null)
            root.style.display = DisplayStyle.None;

        if (!wasOpen)
            return;

        Time.timeScale = 1f;

        // Para que un HUD persistente no se quede oculto.
        if (pauseStateChannel != null)
            pauseStateChannel.Raise(false);
    }

    // =========================================================
    // PANELS
    // =========================================================

    // Los paneles comparten espacio en el layout, así que primero sale uno
    // y después entra el otro (si estuvieran los dos visibles se redimensionarían).
    private void SwitchPanel(VisualElement from, VisualElement to, Action onShown = null)
    {
        isBusy = true;
        HidePanel(from);

        root.schedule.Execute(() =>
        {
            ShowPanel(to);

            if (onShown != null)
                onShown();
            else
                isBusy = false;
        }).StartingIn(ToMs(panelTransitionTime));
    }

    private static void ShowPanel(VisualElement panel)
    {
        if (panel == null)
            return;

        // Si ya está en el layout (p. ej. se estaba ocultando), basta con activarlo.
        if (panel.resolvedStyle.display == DisplayStyle.Flex)
        {
            panel.style.display = DisplayStyle.Flex;
            panel.SetEnabled(true);
            return;
        }

        // Si venía de display:none, hay que esperar a que se calcule su estilo
        // (:disabled) antes de activarlo; si no, la transición no se reproduce.
        panel.style.display = DisplayStyle.Flex;

        EventCallback<GeometryChangedEvent> onLayout = null;
        onLayout = _ =>
        {
            panel.UnregisterCallback(onLayout);
            panel.SetEnabled(true);
        };
        panel.RegisterCallback(onLayout);
    }

    private void HidePanel(VisualElement panel)
    {
        if (panel == null)
            return;

        panel.SetEnabled(false);

        // Tras la animación de salida se quita del layout.
        panel.schedule.Execute(() =>
        {
            if (!panel.enabledSelf)
                panel.style.display = DisplayStyle.None;
        }).StartingIn(ToMs(panelTransitionTime));
    }

    private static void HideImmediate(VisualElement panel)
    {
        if (panel == null)
            return;

        panel.SetEnabled(false);
        panel.style.display = DisplayStyle.None;
    }

    private static long ToMs(float seconds) => Mathf.RoundToInt(seconds * 1000f);
}