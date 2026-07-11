using System;
using UnityEngine;

public class UIManager : BaseSingleton<UIManager>
{

    public event Action<bool> OnJournalStateChanged;
    public event Action<bool> OnPauseStateChanged;

    public event Action OnPausePressed;
    public event Action OnSubmitPressed;
    public event Action OnMapPressed;
    public event Action OnJournalPressed;

    private bool subscribed;

    private void Start()
    {
        SubscribeToInput();
    }

    private void SubscribeToInput()
    {
        if (subscribed || InputController.Instance == null)
            return;

        InputController.Instance.OnPausePressed += HandlePause;
        InputController.Instance.OnSubmitPressed += HandleSubmit;
        InputController.Instance.OnMapPressed += HandleMap;
        InputController.Instance.OnInventoryPressed += HandleJournal;

        subscribed = true;
    }

    protected override void OnDestroy()
    {
        if (subscribed && InputController.Instance != null)
        {
            InputController.Instance.OnPausePressed -= HandlePause;
            InputController.Instance.OnSubmitPressed -= HandleSubmit;
            InputController.Instance.OnMapPressed -= HandleMap;
            InputController.Instance.OnInventoryPressed -= HandleJournal;
        }

        base.OnDestroy();
    }
    private void HandlePause()
    {
        OnPausePressed?.Invoke();
    }

    private void HandleSubmit()
    {
        OnSubmitPressed?.Invoke();
    }

    private void HandleMap()
    {
        OnMapPressed?.Invoke();
    }

    private void HandleJournal()
    {
        OnJournalPressed?.Invoke();
    }

    public void SetJournalOpen(bool isOpen)
    {
        OnJournalStateChanged?.Invoke(isOpen);
    }

    public void SetPauseOpen(bool isOpen)
    {
        OnPauseStateChanged?.Invoke(isOpen);
    }
}