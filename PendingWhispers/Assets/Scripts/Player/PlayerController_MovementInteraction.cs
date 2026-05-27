using System;
using Inventory.Model;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

public class PlayerController_MovementInteraction : MonoBehaviour
{
    public static Action<PlayerController_MovementInteraction> OnPlayerSpawned;

    [Header("Layers")]
    [SerializeField] private LayerMask interactableLayer;

    [Header("Movement")]
    [SerializeField] private float interactDistance = 1.5f;

    private NavMeshAgent agent;
    private Animator animator;

    private IInteractable currentTarget;
    private IInteractable hoveredInteractable;

    public bool canMove = true;

    private bool journalOpen;

    public InventorySO Inventory => InventoryRuntime.Instance.GetInventory();

    private void Awake()
    {
        OnPlayerSpawned?.Invoke(this);
    }

    private void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();

        agent.updatePosition = true;
        agent.updateRotation = false;
        agent.updateUpAxis = false;

        agent.speed = 3.5f;
        agent.acceleration = 8f;
        agent.stoppingDistance = interactDistance;

        agent.isStopped = false;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }
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
        UpdateAnimator();
        CheckInteraction();
        HandleHover();
    }

    // ---------------- CLICK + MOVEMENT ----------------

    private void HandleClick()
    {
        if (!canMove)
            return;

        if (EventSystem.current != null &&
            EventSystem.current.IsPointerOverGameObject())
            return;

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        Collider2D interactableHit =
            Physics2D.OverlapPoint(mousePos, interactableLayer);

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

        currentTarget = null;
        MoveTo(mousePos);
    }

    private void MoveTo(Vector2 destination)
    {
        if (!canMove)
            return;

        agent.isStopped = false;
        agent.SetDestination(destination);
    }

    // ---------------- INTERACTION ----------------

    private void CheckInteraction()
    {
        if (!canMove || currentTarget == null)
            return;

        if (agent.pathPending)
            return;

        if (agent.remainingDistance > agent.stoppingDistance)
            return;

        currentTarget.Interact(this);
        currentTarget = null;
    }

    // ---------------- ANIMATION ----------------

    private void UpdateAnimator()
    {
        if (agent.velocity.sqrMagnitude > 0.01f)
        {
            Vector2 direction = (agent.steeringTarget - transform.position).normalized;

            animator.SetFloat("moveX", direction.x, 0.1f, Time.deltaTime);
            animator.SetFloat("moveY", direction.y, 0.1f, Time.deltaTime);
            animator.SetBool("isMoving", true);
        }
        else
        {
            animator.SetBool("isMoving", false);
        }
    }

    // ---------------- HOVER ----------------

    private void HandleHover()
    {
        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        Collider2D hit = Physics2D.OverlapPoint(mousePos, interactableLayer);

        IInteractable newHover = null;

        if (hit != null)
            newHover = hit.GetComponent<IInteractable>();

        if (hoveredInteractable == newHover)
            return;

        if (hoveredInteractable is Item oldItem)
            oldItem.SetHighlight(false);

        if (newHover is Item newItem)
            newItem.SetHighlight(true);

        hoveredInteractable = newHover;
    }

    // ---------------- UI / JOURNAL ----------------

    public void ToggleInventory()
    {
        if (JournalController.Instance == null)
            return;

        journalOpen = !journalOpen;

        canMove = !journalOpen;
        agent.isStopped = journalOpen;

        JournalController.Instance.ToggleJournal();
    }

    public void OpenMap()
    {
        Debug.Log("OPEN MAP PRESSED");

        GameNavigation.Instance.OpenMap();
    }
}