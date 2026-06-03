using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    public static PauseMenuController Instance { get; private set; }

    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private PlayerController_Actions playerController;

    private bool isPaused;
    private bool subscribed;

    public bool IsPaused => isPaused;

    private void Awake()
    {
        Instance = this;

        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false);
    }

    private void OnEnable()
    {
        TrySubscribe();
    }

    private void TrySubscribe()
    {
        if (subscribed)
            return;

        if (UIManager.Instance == null)
        {
            Invoke(nameof(TrySubscribe), 0.1f);
            return;
        }

        UIManager.Instance.OnPausePressed += TogglePause;
        subscribed = true;
    }

    private void OnDisable()
    {
        if (!subscribed || UIManager.Instance == null)
            return;

        UIManager.Instance.OnPausePressed -= TogglePause;
        subscribed = false;
    }

    private void TogglePause()
    {
        if (JournalController.Instance != null && JournalController.Instance.IsOpen)
            return;

        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    private void PauseGame()
    {
        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(true);

        Time.timeScale = 0f;
        isPaused = true;

        UIManager.Instance?.SetPauseOpen(true);

        if (playerController != null)
            playerController.canMove = false;
    }

    public void ResumeGame()
    {
        if (pauseMenuUI != null)
            pauseMenuUI.SetActive(false);

        Time.timeScale = 1f;
        isPaused = false;

        UIManager.Instance?.SetPauseOpen(false);

        if (playerController != null)
            playerController.canMove = true;
    }

    public void Options()
    {
    }

    public void ExitGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}