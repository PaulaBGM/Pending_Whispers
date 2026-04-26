using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputController : MonoBehaviour
{
    public static InputController Instance { get; private set; }

    public event Action OnPausePressed;
    public event Action OnSubmitPressed;
    public event Action OnClickPressed;
    public event Action OnMapPressed;
    public event Action OnInventoryPressed;

    private PlayerInput playerInput;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            playerInput = GetComponent<PlayerInput>();
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        playerInput.actions["PauseMenu"].performed += PausePerformed;
        playerInput.actions["Submit"].performed += SubmitPerformed;
        playerInput.actions["Click"].performed += ClickPerformed;
        playerInput.actions["Map"].performed += MapPerformed;
        playerInput.actions["Inventory"].performed += InventoryPerformed;
    }

    private void OnDisable()
    {
        playerInput.actions["PauseMenu"].performed -= PausePerformed;
        playerInput.actions["Submit"].performed -= SubmitPerformed;
        playerInput.actions["Click"].performed -= ClickPerformed;
        playerInput.actions["Map"].performed -= MapPerformed;
        playerInput.actions["Inventory"].performed -= InventoryPerformed;
    }

    private void ClickPerformed(InputAction.CallbackContext context)
    {
        OnClickPressed?.Invoke();
    }

    private void SubmitPerformed(InputAction.CallbackContext context)
    {
        OnSubmitPressed?.Invoke();
    }

    private void PausePerformed(InputAction.CallbackContext context)
    {
        OnPausePressed?.Invoke();
    }

    private void MapPerformed(InputAction.CallbackContext context)
    {
        OnMapPressed?.Invoke();
    }

    private void InventoryPerformed(InputAction.CallbackContext context)
    {
        OnInventoryPressed?.Invoke();
    }
}