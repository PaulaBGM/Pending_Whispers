using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputController : BaseSingleton<InputController>
{
    public event Action OnPausePressed;
    public event Action OnSubmitPressed;
    public event Action OnClickPressed;
    public event Action OnMapPressed;
    public event Action OnInventoryPressed;
    public event Action OnDetectionPressed;

    private PlayerInput playerInput;

    private InputAction pauseAction;
    private InputAction submitAction;
    private InputAction clickAction;
    private InputAction mapAction;
    private InputAction inventoryAction;
    private InputAction detectionAction;

    protected override void Awake()
    {
        base.Awake();
        playerInput = GetComponent<PlayerInput>();
    }

    private void OnEnable()
    {
        pauseAction = playerInput.actions["PauseMenu"];
        submitAction = playerInput.actions["Submit"];
        clickAction = playerInput.actions["Click"];
        mapAction = playerInput.actions["Map"];
        inventoryAction = playerInput.actions["Inventory"];
        detectionAction = playerInput.actions["Detection"];

        pauseAction.performed += OnPause;
        submitAction.performed += OnSubmit;
        clickAction.performed += OnClick;
        mapAction.performed += OnMap;
        inventoryAction.performed += OnInventory;
        detectionAction.performed += OnDetection;
    }

    private void OnDisable()
    {
        pauseAction.performed -= OnPause;
        submitAction.performed -= OnSubmit;
        clickAction.performed -= OnClick;
        mapAction.performed -= OnMap;
        inventoryAction.performed -= OnInventory;
        detectionAction.performed -= OnDetection;
    }

    private void OnPause(InputAction.CallbackContext ctx) => OnPausePressed?.Invoke();
   
    private void OnSubmit(InputAction.CallbackContext ctx) => OnSubmitPressed?.Invoke();
    private void OnClick(InputAction.CallbackContext ctx) => OnClickPressed?.Invoke();
    private void OnMap(InputAction.CallbackContext ctx) => OnMapPressed?.Invoke();
    private void OnInventory(InputAction.CallbackContext ctx) => OnInventoryPressed?.Invoke();
    private void OnDetection(InputAction.CallbackContext ctx) => OnDetectionPressed?.Invoke();
}