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

        yield return null;


        yield return null;

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

    }
    private void OnEnable()
    {
    }

    private void OnDisable()
    {
    }

    private void OnDestroy()
    {
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