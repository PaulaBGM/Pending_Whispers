using Inventory;
using Inventory.Model;
using Inventory.UI;
using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class PlayerController : MonoBehaviour
{
    public static Action<PlayerController> OnPlayerSpawned;

    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private LayerMask collisionLayer;

    [SerializeField] private UIInventoryPage inventoryUI;

    private Vector2 target;
    private Animator animator;
    private Vector2 lastDirection;

    public float speed = 5f;
    public float interactDistance = 1.5f;

    private IInteractable currentTarget;
    private IInteractable hoveredInteractable;

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
            InputController.Instance.OnClickPressed += HandleClick;
            InputController.Instance.OnInventoryPressed += ToggleInventory;
            InputController.Instance.OnMapPressed += OpenMap;
        }
    }

    private void OnDisable()
    {
        if (InputController.Instance != null)
        {
            InputController.Instance.OnClickPressed -= HandleClick;
            InputController.Instance.OnInventoryPressed -= ToggleInventory;
            InputController.Instance.OnMapPressed -= OpenMap;
        }
    }

    void Update()
    {
        Move();
        CheckInteraction();
        HandleHover();
    }

    void HandleClick()
    {
        if (!canMove) return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        Collider2D hit = Physics2D.OverlapPoint(mousePos, interactableLayer);

        if (hit != null)
        {
            IInteractable interactable = hit.GetComponent<IInteractable>();

            if (interactable != null)
            {
                currentTarget = interactable;
                target = interactable.GetTransform().position;
                return;
            }
        }

        Collider2D groundHit = Physics2D.OverlapPoint(mousePos, groundLayer);

        if (groundHit != null)
        {
            currentTarget = null;
            target = groundHit.ClosestPoint(mousePos);
        }
    }

    void Move()
    {
        if (!canMove) return;

        Vector2 currentPosition = transform.position;
        Vector2 direction = (target - currentPosition).normalized;

        Vector2 nextPosition = Vector2.MoveTowards(currentPosition, target, speed * Time.deltaTime);
        Vector2 moveDir = nextPosition - currentPosition;

        RaycastHit2D hit = Physics2D.BoxCast(
            currentPosition,
            new Vector2(0.8f, 0.8f),
            0f,
            moveDir.normalized,
            moveDir.magnitude,
            collisionLayer
        );

        bool isMoving = moveDir.magnitude > 0.0001f && hit.collider == null;

        if (hit.collider == null)
        {
            transform.position = nextPosition;
        }

        if (isMoving)
        {
            lastDirection = direction;
        }

        animator.SetFloat("moveX", lastDirection.x);
        animator.SetFloat("moveY", lastDirection.y);
        animator.SetBool("isMoving", isMoving);
    }

    void CheckInteraction()
    {
        if (!canMove) return;
        if (currentTarget == null) return;

        float distance = Vector2.Distance(
            transform.position,
            currentTarget.GetTransform().position
        );

        if (distance <= interactDistance)
        {
            currentTarget.Interact(this);
            currentTarget = null;
        }
    }

    void HandleHover()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        Collider2D hit = Physics2D.OverlapPoint(mousePos, interactableLayer);

        IInteractable newHover = null;

        if (hit != null)
            newHover = hit.GetComponent<IInteractable>();

        if (hoveredInteractable != newHover)
        {
            if (hoveredInteractable is Item oldItem)
                oldItem.SetHighlight(false);

            if (newHover is Item newItem)
                newItem.SetHighlight(true);

            hoveredInteractable = newHover;
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
            {
                Debug.Log("[Player] Refresh UI");
                InventoryController.Instance.RefreshUI();
            }
            else
            {
                Debug.LogWarning("[Player] InventoryController NULL");
            }
        }
        else
        {
            inventoryUI.Hide();
            canMove = true;
        }
    }

    public void OpenMap()
    {
        SceneManager.LoadScene("Map");
    }
}