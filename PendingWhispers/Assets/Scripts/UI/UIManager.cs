using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    // Evento que los menús escucharán
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

    // Método que se llama cuando el input de pausa se detecta
    private void TogglePause()
    {
        // Reenvía el evento a cualquier UI que se suscriba
        OnPausePressed?.Invoke();
    }
    
    private void SubmitPressed()
    {
        OnSubmitPressed?.Invoke();
    }
}