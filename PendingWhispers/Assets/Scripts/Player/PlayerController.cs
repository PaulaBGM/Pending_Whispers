using Inventory;
using Inventory.Model;
using Inventory.UI;
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerController : MonoBehaviour
{
    public static Action<PlayerController> OnPlayerSpawned;

    [Header("Layers")]
    [SerializeField] private LayerMask interactableLayer;

    [Header("UI")]
    [SerializeField] private UIInventoryPage inventoryUI;

    [Header("Movement")]
    [SerializeField] private float speed = 5f;
    [SerializeField] private float waypointReachedDistance = 0.05f;
    [SerializeField] private float interactDistance = 1.5f;

    private Animator animator;

    private readonly Queue<Vector2> currentPath = new();

    private Vector2 lastDirection = Vector2.down;

    private IInteractable currentTarget;
    private IInteractable hoveredInteractable;

    private PathNode[] cachedNodes;

    public bool canMove = true;

    public InventorySO Inventory => InventoryRuntime.Instance.GetInventory();

    private void Awake()
    {
        OnPlayerSpawned?.Invoke(this);
    }

    private void Start()
    {
        animator = GetComponentInChildren<Animator>();

        // Cacheamos los nodos UNA SOLA VEZ
        cachedNodes = FindObjectsByType<PathNode>(FindObjectsSortMode.None);
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

    private void Update()
    {
        Move();
        CheckInteraction();
        HandleHover();
    }

    private void HandleClick()
    {
        if (!canMove)
            return;

        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
            return;

        Vector2 mousePos =
            Camera.main.ScreenToWorldPoint(Input.mousePosition);

        Collider2D interactableHit =
            Physics2D.OverlapPoint(mousePos, interactableLayer);

        // CLICK EN INTERACTUABLE
        if (interactableHit != null)
        {
            IInteractable interactable =
                interactableHit.GetComponent<IInteractable>();

            if (interactable != null)
            {
                currentTarget = interactable;

                MoveTo(interactable.GetTransform().position);

                return;
            }
        }

        // CLICK EN SUELO
        currentTarget = null;

        MoveTo(mousePos);
    }

    private void MoveTo(Vector2 destination)
    {
        PathNode startNode = GetClosestNode(transform.position);
        PathNode endNode = GetClosestNode(destination);

        if (startNode == null || endNode == null)
            return;

        List<Vector2> path =
            Pathfinder.Instance.FindPath(startNode, endNode);

        currentPath.Clear();

        foreach (Vector2 point in path)
        {
            currentPath.Enqueue(point);
        }
    }

    private void Move()
    {
        if (!canMove)
        {
            animator.SetBool("isMoving", false);
            return;
        }

        if (currentPath.Count == 0)
        {
            animator.SetBool("isMoving", false);
            return;
        }

        Vector2 currentPosition = transform.position;
        Vector2 targetPosition = currentPath.Peek();

        Vector2 direction =
            (targetPosition - currentPosition).normalized;

        Vector2 nextPosition = Vector2.MoveTowards(
            currentPosition,
            targetPosition,
            speed * Time.deltaTime
        );

        transform.position = nextPosition;

        if (Vector2.Distance(nextPosition, targetPosition)
            <= waypointReachedDistance)
        {
            currentPath.Dequeue();
        }

        if (direction.sqrMagnitude > 0.001f)
        {
            lastDirection = direction;
        }

        animator.SetFloat("moveX", lastDirection.x);
        animator.SetFloat("moveY", lastDirection.y);
        animator.SetBool("isMoving", true);
    }

    private void CheckInteraction()
    {
        if (!canMove || currentTarget == null)
            return;

        float distance = Vector2.Distance(
            transform.position,
            currentTarget.GetTransform().position
        );

        if (distance <= interactDistance)
        {
            currentPath.Clear();

            currentTarget.Interact(this);

            currentTarget = null;
        }
    }

    private void HandleHover()
    {
        Vector2 mousePos =
            Camera.main.ScreenToWorldPoint(Input.mousePosition);

        Collider2D hit =
            Physics2D.OverlapPoint(mousePos, interactableLayer);

        IInteractable newHover = null;

        if (hit != null)
        {
            newHover = hit.GetComponent<IInteractable>();
        }

        if (hoveredInteractable == newHover)
            return;

        if (hoveredInteractable is Item oldItem)
        {
            oldItem.SetHighlight(false);
        }

        if (newHover is Item newItem)
        {
            newItem.SetHighlight(true);
        }

        hoveredInteractable = newHover;
    }

    private PathNode GetClosestNode(Vector2 position)
    {
        PathNode closest = null;

        float minDistance = float.MaxValue;

        // Sin FindObjectsOfType en runtime
        foreach (PathNode node in cachedNodes)
        {
            float distance =
                ((Vector2)node.transform.position - position).sqrMagnitude;

            if (distance < minDistance)
            {
                minDistance = distance;
                closest = node;
            }
        }

        return closest;
    }

    public void ToggleInventory()
    {
        if (inventoryUI == null)
            return;

        if (!inventoryUI.isActiveAndEnabled)
        {
            inventoryUI.Show();

            canMove = false;

            if (InventoryController.Instance != null)
            {
                InventoryController.Instance.RefreshUI();
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
        SceneController.Instance.LoadScene("Map");
    }
}