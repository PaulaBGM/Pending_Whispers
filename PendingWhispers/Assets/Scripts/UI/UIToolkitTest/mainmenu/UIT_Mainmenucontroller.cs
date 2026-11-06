using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Main menu controller for MainMenu_Template.uxml (UI Toolkit).
/// Start, Load and Exit are wired.
/// Keyboard/gamepad Submit is handled by UI Toolkit focus, so this class does not
/// subscribe to UIManager.OnSubmitPressed. That subscription started the game even
/// when another button had focus.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class UIT_MainMenuController : MonoBehaviour
{
    private const string StartButtonName = "BT_Start";
    private const string LoadButtonName = "BT_Load";
    private const string ExitButtonName = "BT_Exit";

    [Header("Flow")]
    [Tooltip("La partida nueva empieza en el despacho (HUB): el caso se acepta en la pizarra.")]
    [SerializeField] private string firstSceneName = "SC_Catacombs_HUB";

    [Header("New Game")]
    [Tooltip("Opcional. Vacío = no hay caso activo hasta aceptarlo en la pizarra del HUB.")]
    [SerializeField] private CaseData startingCase;

    [Header("Save")]
    [SerializeField] private int saveSlot = 0;

    private UIDocument document;
    private Button startButton;
    private Button loadButton;
    private Button exitButton;
    private bool isLoading;

    private void Awake()
    {
        document = GetComponent<UIDocument>();
    }

    // UIDocument rebuilds its visual tree every time it is enabled,
    // so element references are queried here and not in Awake.
    private void OnEnable()
    {
        VisualElement root = document.rootVisualElement;

        startButton = root.Q<Button>(StartButtonName);
        loadButton = root.Q<Button>(LoadButtonName);
        exitButton = root.Q<Button>(ExitButtonName);

        if (startButton == null || exitButton == null)
        {
            Debug.LogError("[UIT_MainMenu] BT_Start or BT_Exit not found in UXML.", this);
            return;
        }

        if (loadButton == null)
            Debug.LogWarning($"[UIT_MainMenu] {LoadButtonName} not found in UXML. Load will be unavailable.", this);

        startButton.clicked += StartGame;
        exitButton.clicked += ExitGame;

        if (loadButton != null)
            loadButton.clicked += LoadGame;

        isLoading = false;
        SetButtonsEnabled(true);

        // Next frame: SaveSystem may not have run Awake yet when this OnEnable fires,
        // so the Load state is refreshed again here, then focus is given.
        startButton.schedule.Execute(() =>
        {
            RefreshLoadButton();
            startButton.Focus();
        });
    }

    private void OnDisable()
    {
        if (startButton != null)
            startButton.clicked -= StartGame;

        if (loadButton != null)
            loadButton.clicked -= LoadGame;

        if (exitButton != null)
            exitButton.clicked -= ExitGame;
    }

    // ---------------- NEW GAME ----------------

    public void StartGame()
    {
        if (isLoading)
            return;

        if (SceneController.Instance == null)
        {
            Debug.LogError("[UIT_MainMenu] SceneController not found in scene.", this);
            return;
        }

        isLoading = true;
        SetButtonsEnabled(false);

        // Persistent singletons keep the previous run's state (e.g. after Save and exit),
        // so it is cleared before starting a new game.
        if (SaveSystem.Instance != null)
            SaveSystem.Instance.ResetRuntimeState();

        // If CaseManager survived from a previous run, its Awake won't run again,
        // so the starting case is loaded here. If it doesn't exist yet, its own
        // Awake will load the case assigned in its inspector.
        if (CaseManager.Instance != null && startingCase != null)
            CaseManager.Instance.LoadCase(startingCase);

        SceneController.Instance.LoadScene(firstSceneName);
    }

    // ---------------- LOAD ----------------

    public void LoadGame()
    {
        if (isLoading || SaveSystem.Instance == null)
            return;

        isLoading = true;
        SetButtonsEnabled(false);

        if (!SaveSystem.Instance.Load(saveSlot))
        {
            Debug.LogWarning("[UIT_MainMenu] Could not load the saved game.", this);
            isLoading = false;
            SetButtonsEnabled(true);
        }
    }

    private void RefreshLoadButton()
    {
        if (loadButton == null)
            return;

        bool hasSave = SaveSystem.Instance != null && SaveSystem.Instance.HasSave(saveSlot);
        loadButton.SetEnabled(hasSave && !isLoading);
    }

    // ---------------- EXIT ----------------

    public void ExitGame()
    {
        if (isLoading)
            return;

        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    // ---------------- HELPERS ----------------

    private void SetButtonsEnabled(bool enabled)
    {
        startButton?.SetEnabled(enabled);
        exitButton?.SetEnabled(enabled);

        // Load also depends on a save existing.
        RefreshLoadButton();
    }
}