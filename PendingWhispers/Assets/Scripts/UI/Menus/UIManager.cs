using System;
using System.Collections;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    public event Action<bool> OnJournalStateChanged;
    public event Action<bool> OnPauseStateChanged;

    public event Action OnPausePressed;
    public event Action OnSubmitPressed;
    public event Action OnMapPressed;
    public event Action OnJournalPressed;

    private bool subscribed;

    private void Awake()
    {
        Debug.Log("[UIManager] Awake");

        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        StartCoroutine(TestCoroutine());
    }

    private IEnumerator TestCoroutine()
    {
        Debug.Log("[UIManager] Coroutine Start");

        yield return null;

        Debug.Log("[UIManager] Coroutine Frame 1");

        yield return null;

        Debug.Log("[UIManager] Coroutine Frame 2");
    }

    private void Start()
    {
        StartCoroutine(SubscribeToInput());
    }

    private IEnumerator SubscribeToInput()
    {
        while (InputController.Instance == null)
            yield return null;

        InputController.Instance.OnPausePressed += HandlePause;
        InputController.Instance.OnSubmitPressed += HandleSubmit;
        InputController.Instance.OnMapPressed += HandleMap;
        InputController.Instance.OnInventoryPressed += HandleJournal;

        subscribed = true;

        Debug.Log("[UIManager] Suscrito a InputController");
    }
    private void OnEnable()
    {
        Debug.Log("[UIManager] OnEnable");
    }

    private void OnDisable()
    {
        Debug.Log("[UIManager] OnDisable");
    }

    private void OnDestroy()
    {
        Debug.Log("[UIManager] OnDestroy");
    }
    private void HandlePause()
    {
        Debug.Log("[UIManager] Evento Pause recibido");
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