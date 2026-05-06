using Inventory;
using Inventory.Model;
using Inventory.UI;
using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerController_Tank : MonoBehaviour
{
    public static Action<PlayerController_Tank> OnPlayerSpawned;

    [Header("Layers")]
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private LayerMask collisionLayer;

    [Header("UI")]
    [SerializeField] private UIInventoryPage inventoryUI;

    [Header("Movement")]
    public float moveSpeed = 3f;
    public float rotationSpeed = 200f;
    public float interactDistance = 1.5f;

    private Animator animator;

    private float moveInput;
    private float rotationInput;

    private Vector2 lastDirection = Vector2.down;

    private IInteractable currentInteractable;

    public bool canMove = true;

    public InventorySO Inventory => InventoryRuntime.Instance.GetInventory();

    private void Awake()
    {
        OnPlayerSpawned?.Invoke(this);
    }

    private void Start()
    {
        animator = GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        if (InputController.Instance != null)
        {
            InputController.Instance.OnInventoryPressed += ToggleInventory;
            InputController.Instance.OnMapPressed += OpenMap;
            InputController.Instance.OnClickPressed += TryInteract; // reutilizamos click como "interact"
        }
    }

    private void OnDisable()
    {
        if (InputController.Instance != null)
        {
            InputController.Instance.OnInventoryPressed -= ToggleInventory;
            InputController.Instance.OnMapPressed -= OpenMap;
            InputController.Instance.OnClickPressed -= TryInteract;
        }
    }

    void Update()
    {
        ReadInput();
        Move();
    }

    void ReadInput()
    {
        if (!canMove)
        {
            moveInput = 0;
            rotationInput = 0;
            return;
        }

        moveInput = Input.GetAxis("Vertical");   // W/S o stick
        rotationInput = -Input.GetAxis("Horizontal"); // A/D o stick
    }

    void Move()
    {
        if (!canMove) return;

        // ROTACIÓN
        transform.Rotate(Vector3.forward, rotationInput * rotationSpeed * Time.deltaTime);

        // DIRECCIÓN (forward del personaje en 2D = up)
        Vector2 forward = transform.up;

        Vector2 movement = forward * moveInput * moveSpeed * Time.deltaTime;

        // COLISIÓN
        RaycastHit2D hit = Physics2D.BoxCast(
            transform.position,
            new Vector2(0.8f, 0.8f),
            0f,
            forward,
            Mathf.Abs(moveInput * moveSpeed * Time.deltaTime),
            collisionLayer
        );

        bool isMoving = Mathf.Abs(moveInput) > 0.01f && hit.collider == null;

        if (hit.collider == null)
        {
            transform.position += (Vector3)movement;
        }

        if (isMoving)
        {
            lastDirection = forward;
        }

        // ANIMACIÓN
        animator.SetFloat("moveX", lastDirection.x);
        animator.SetFloat("moveY", lastDirection.y);
        animator.SetBool("isMoving", isMoving);
    }

    void TryInteract()
    {
        if (!canMove) return;

        // Detectar delante del jugador
        Vector2 origin = transform.position;
        Vector2 direction = transform.up;

        RaycastHit2D hit = Physics2D.Raycast(origin, direction, interactDistance, interactableLayer);

        if (hit.collider != null)
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                interactable.Interact(null);
            }
        }
    }

    public void ToggleInventory()
    {
        if (inventoryUI == null) return;

        if (!inventoryUI.isActiveAndEnabled)
        {
            inventoryUI.Show();
            canMove = false;

            if (InventoryController.Instance != null)
                InventoryController.Instance.RefreshUI();
        }
        else
        {
            inventoryUI.Hide();
            canMove = true;
        }
    }

    public void OpenMap()
    {
        SceneController.Instance.LoadScene("Map");
    }
}