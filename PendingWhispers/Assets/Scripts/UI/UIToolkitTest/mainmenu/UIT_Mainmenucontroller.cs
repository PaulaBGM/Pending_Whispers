using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Main menu controller for MainMenu_Template.uxml (UI Toolkit).
/// Start and Exit are wired. Load is left untouched until the save system exists.
/// Keyboard/gamepad Submit is handled by UI Toolkit focus, so this class does not
/// subscribe to UIManager.OnSubmitPressed. That subscription started the game even
/// when another button had focus.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class UIT_MainMenuController : MonoBehaviour
{
    private const string StartButtonName = "BT_Start";
    private const string ExitButtonName = "BT_Exit";

    [Header("Flow")]
    [SerializeField] private string firstSceneName = "SC_HouseOfPhantom1";

    private UIDocument document;
    private Button startButton;
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
        exitButton = root.Q<Button>(ExitButtonName);

        if (startButton == null || exitButton == null)
        {
            Debug.LogError("[UIT_MainMenu] BT_Start or BT_Exit not found in UXML.", this);
            return;
        }

        startButton.clicked += StartGame;
        exitButton.clicked += ExitGame;

        isLoading = false;
        SetButtonsEnabled(true);

        // Next frame, give initial focus for keyboard/gamepad navigation.
        startButton.schedule.Execute(() => startButton.Focus());
    }

    private void OnDisable()
    {
        if (startButton != null)
            startButton.clicked -= StartGame;

        if (exitButton != null)
            exitButton.clicked -= ExitGame;
    }

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
        SceneController.Instance.LoadScene(firstSceneName);
    }

    public void ExitGame()
    {
        if (isLoading)
            return;

        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }

    private void SetButtonsEnabled(bool enabled)
    {
        startButton?.SetEnabled(enabled);
        exitButton?.SetEnabled(enabled);
    }
}