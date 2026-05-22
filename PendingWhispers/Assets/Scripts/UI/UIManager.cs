using System;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    public event Action OnPausePressed;
    public event Action OnSubmitPressed;
    public event Action OnMapPressed;
    public event Action OnJournalPressed;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void HandlePause() => OnPausePressed?.Invoke();
    private void HandleSubmit() => OnSubmitPressed?.Invoke();
    private void HandleMap() => OnMapPressed?.Invoke();
    private void HandleJournal() => OnJournalPressed?.Invoke();

    private void OnEnable()
    {
        if (InputController.Instance == null) return;

        InputController.Instance.OnPausePressed += HandlePause;
        InputController.Instance.OnSubmitPressed += HandleSubmit;
        InputController.Instance.OnMapPressed += HandleMap;
        InputController.Instance.OnInventoryPressed += HandleJournal;
    }

    private void OnDisable()
    {
        if (InputController.Instance == null) return;

        InputController.Instance.OnPausePressed -= HandlePause;
        InputController.Instance.OnSubmitPressed -= HandleSubmit;
        InputController.Instance.OnMapPressed -= HandleMap;
        InputController.Instance.OnInventoryPressed -= HandleJournal;
    }
}