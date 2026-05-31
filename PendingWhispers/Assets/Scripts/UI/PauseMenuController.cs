using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenuUI;
    [SerializeField] private PlayerController_Actions playerController;

    private bool isPaused;

    private void Awake()
    {
        pauseMenuUI.SetActive(false);
    }

    private void OnEnable()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.OnPausePressed += TogglePause;
    }

    private void OnDisable()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.OnPausePressed -= TogglePause;
    }

    private void TogglePause()
    {
        Debug.Log("Toggle Pause");

        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    private void PauseGame()
    {
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        isPaused = true;

        if (playerController != null)
            playerController.canMove = false;
    }

    public void ResumeGame()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;

        if (playerController != null)
            playerController.canMove = true;
    }

    public void Options()
    {
        Debug.Log("Options menu not implemented yet");
    }

    public void ExitGame()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}