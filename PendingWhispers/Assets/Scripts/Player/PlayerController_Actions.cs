using System;
using Inventory.Model;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

public class PlayerController_Actions : MonoBehaviour
{
    public static Action<PlayerController_Actions> OnPlayerSpawned;

    [Header("Layers")]
    [SerializeField] private LayerMask interactableLayer;

    [Header("Movement")]
    [SerializeField] private float interactDistance = 1.5f; // Distancia necesaria para interactuar con objetos
    [SerializeField] private float moveStoppingDistance = 0.1f; // Distancia mínima para movimiento normal

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
        
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        // Configuración necesaria para NavMesh 2D
        agent.updatePosition = true;
        agent.updateRotation = false;
        agent.updateUpAxis = false;

        agent.speed = 3.5f;
        agent.acceleration = 8f;
        agent.stoppingDistance = moveStoppingDistance;

        agent.isStopped = false;

        // Asegura que el player empiece dentro del NavMesh
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
    
    private void LateUpdate()
    {
        if (!agent.isOnNavMesh)
        {
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
        }
    }

    // ---------------- CLICK + MOVIMIENTO ----------------

    private void HandleClick()
    {
        if (!canMove)
            return;

        // Ignora clicks sobre UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        Collider2D interactableHit = Physics2D.OverlapPoint(mousePos, interactableLayer);

        // Click sobre interactuable
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
        agent.stoppingDistance = moveStoppingDistance; // Distancia pequeña para que el player se acerque bien

        MoveTo(mousePos);
    }

    private void MoveTo(Vector2 destination)
    {
        if (!canMove)
            return;

        if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            agent.SetDestination(hit.position);
        }
    }

    // ---------------- INTERACCIÓN ----------------

    private void CheckInteraction()
    {
        if (!canMove || currentTarget == null)
            return;

        if (agent.pathPending)
            return;

        float distance = Vector2.Distance(transform.position,currentTarget.GetTransform().position); // Distancia real al objetivo Más fiable que remainingDistance en 2D

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + interactDistance)
        {
            agent.isStopped = true;
            currentTarget.Interact(this);
            currentTarget = null;
        }
    }

    // ---------------- ANIMACIÓN ----------------

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

        // Evita actualizar highlight innecesariamente
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
        
        // Detiene el movimiento al abrir UI
        if (journalOpen)
        {
            agent.isStopped = true;
        }
        else
        {
            agent.isStopped = false;
        }
        
        JournalController.Instance.ToggleJournal();
    }

    public void OpenMap()
    {
        GameNavigation.Instance.OpenMap();
    }
}