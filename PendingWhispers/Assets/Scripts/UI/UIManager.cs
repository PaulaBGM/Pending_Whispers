using System;
using UnityEngine;
using UnityEngine.SceneManagement; 

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    public event Action OnPausePressed;
    public event Action OnSubmitPressed;
    public event Action OnMapPressed;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void OnEnable()
    {
        if (InputController.Instance != null)
        {
            InputController.Instance.OnPausePressed += TogglePause;
            InputController.Instance.OnSubmitPressed += SubmitPressed;

            InputController.Instance.OnMapPressed += OpenMap; 
        }
    }

    private void OnDisable()
    {
        if (InputController.Instance != null)
        {
            InputController.Instance.OnPausePressed -= TogglePause;
            InputController.Instance.OnSubmitPressed -= SubmitPressed;

            InputController.Instance.OnMapPressed -= OpenMap; // NEW
        }
    }

    private void TogglePause()
    {
        OnPausePressed?.Invoke();
    }

    private void SubmitPressed()
    {
        OnSubmitPressed?.Invoke();
    }

    // NEW
    private void OpenMap()
    {
        Debug.Log("[UIManager] Abriendo mapa");

        SceneManager.LoadScene("Map"); 
    }
}