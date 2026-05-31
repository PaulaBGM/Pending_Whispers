using System;
using UnityEngine;
using UnityEngine.SceneManagement; 

public partial class UIManager_ : MonoBehaviour
{
    public static UIManager_ Instance { get; private set; }

    public event Action OnPausePressed;
    public event Action OnSubmitPressed;

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

        }
    }

    private void OnDisable()
    {
        if (InputController.Instance != null)
        {
            InputController.Instance.OnPausePressed -= TogglePause;
            InputController.Instance.OnSubmitPressed -= SubmitPressed;

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

    private void OpenMap()
    {
        Debug.Log("[UIManager] Abriendo mapa");

        SceneController.Instance.LoadScene("Map");
    }

}