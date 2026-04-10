using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenuUI;

    private bool isPaused;

    private void Awake()
    {
        pauseMenuUI.SetActive(false);
    }

    private void OnEnable()
    {
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
    }

    public void ResumeGame()
    {
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        isPaused = false;
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