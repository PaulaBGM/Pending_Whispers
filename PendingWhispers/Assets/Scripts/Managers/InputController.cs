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
        if (Instance != this) return; // nuevo
        playerInput = GetComponent<PlayerInput>();
    }

    private void OnEnable()
    {
        // Copia de otra escena que se va a destruir: no se engancha a nada.
        if (Instance != this)
            return;

        // Se usan las acciones del proyecto (Project-wide Actions = InputSystem_Actions), que
        // Unity activa al arrancar y no dependen de ningun PlayerInput. Antes se leian de
        // playerInput.actions: al venir de otra escena con PlayerInput (HUB -> mapa -> casa),
        // el PlayerInput nuevo podia hacer una COPIA del asset que nunca se activaba y se
        // quedaba sin teclado/raton, asi que el clic no llegaba y el jugador no se movia.
        InputActionAsset actions = InputSystem.actions;
        if (actions == null && playerInput != null)
            actions = playerInput.actions;

        if (actions == null)
        {
            Debug.LogError("[InputController] No hay acciones de input (Project-wide Actions ni PlayerInput).", this);
            return;
        }

        pauseAction = actions["PauseMenu"];
        submitAction = actions["Submit"];
        clickAction = actions["Click"];
        mapAction = actions["Map"];
        inventoryAction = actions["Inventory"];
        detectionAction = actions["Detection"];

        // Por si algo las desactivo (no hace nada si ya estan activas).
        pauseAction.Enable();
        submitAction.Enable();
        clickAction.Enable();
        mapAction.Enable();
        inventoryAction.Enable();
        detectionAction.Enable();

        pauseAction.performed += OnPause;
        submitAction.performed += OnSubmit;
        clickAction.performed += OnClick;
        mapAction.performed += OnMap;
        inventoryAction.performed += OnInventory;
        detectionAction.performed += OnDetection;
    }

    private void OnDisable()
    {
        if (clickAction == null)
            return;

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