using System;
using Inventory.Model;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.EventSystems;

public class PlayerController_Actions : MonoBehaviour
{
    public static Action<PlayerController_Actions> OnPlayerSpawned;
    [SerializeField] private FlagSO unlockCatacombsFlag;
    [Header("Layers")]
    [SerializeField] private LayerMask interactableLayer;
    
    [Header("Movement")]
    [SerializeField] private float speed = 3.5f; // Distancia necesaria para interactuar con objetos
    [SerializeField] private float interactDistance = 1.5f; // Distancia necesaria para interactuar con objetos
    [SerializeField] private float moveStoppingDistance = 0.1f; // Distancia mínima para movimiento normal
    [SerializeField] private float aceleration = 50f; // Distancia mínima para movimiento normal

    private NavMeshAgent agent;
    private Animator animator;

    private IInteractable currentTarget;
    private IInteractable hoveredInteractable;

    public bool canMove = true;
    
    public InventorySO Inventory => InventoryRuntime.Instance.GetInventory();
    [SerializeField] private BoolEventChannelSO journalStateChannel;
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

        agent.speed = speed;
        agent.acceleration = aceleration;
        agent.stoppingDistance = moveStoppingDistance;

        agent.isStopped = false;

        // Asegura que el player empiece dentro del NavMesh
        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }
    }

    // InputController al que estamos suscritos. Al cambiar de escena se destruye el anterior
    // (no es raiz y no persiste) y aparece otro: si el jugador se suscribia antes de que
    // existiera el nuevo, se quedaba sin recibir clics y no se podia mover.
    private InputController subscribedInput;

    private void OnEnable()
    {
        SyncInputSubscription();
        journalStateChannel.OnRaised += HandleJournalStateChanged;
        InteractionBlocker.OnBlockedChanged += HandleBlockedChanged;
    }

    private void OnDisable()
    {
        UnsubscribeInput();
        journalStateChannel.OnRaised -= HandleJournalStateChanged;
        InteractionBlocker.OnBlockedChanged -= HandleBlockedChanged;
    }

    // Al abrirse una pantalla que bloquea el mundo (pizarra del HUB, diario, dialogo, pausa)
    // Lyra se para y se olvida del objetivo que llevaba.
    private void HandleBlockedChanged(bool blocked)
    {
        if (!blocked)
            return;

        currentTarget = null;
        ClearHover();

        if (agent != null && agent.isOnNavMesh)
            agent.isStopped = true;
    }

    private void SyncInputSubscription()
    {
        InputController current = InputController.Instance;
        if (current == subscribedInput && !ReferenceEquals(subscribedInput, null))
            return;

        UnsubscribeInput();

        if (current == null)
            return;

        current.OnClickPressed += HandleClick;
        current.OnInventoryPressed += ToggleInventory;
        current.OnMapPressed += OpenMap;
        subscribedInput = current;
    }

    private void UnsubscribeInput()
    {
        if (!ReferenceEquals(subscribedInput, null))
        {
            subscribedInput.OnClickPressed -= HandleClick;
            subscribedInput.OnInventoryPressed -= ToggleInventory;
            subscribedInput.OnMapPressed -= OpenMap;
        }

        subscribedInput = null;
    }

    private void Update()
    {
        SyncInputSubscription();
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
        {
            Debug.Log($"[Player] Clic ignorado: canMove = false (escena {gameObject.scene.name}).", this);
            return;
        }

        // Pizarra del HUB, diario, dialogo o pausa abiertos.
        if (InteractionBlocker.IsBlocked)
            return;

        // Ignora clicks sobre UI
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            LogUIBlockingClick();
            return;
        }

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        Collider2D interactableHit = Physics2D.OverlapPoint(mousePos, interactableLayer);

        // Click sobre interactuable
        if (interactableHit != null)
        {
            IInteractable interactable = interactableHit.GetComponent<IInteractable>();

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

    // Diagnostico: dice que UI se esta comiendo el clic (UIDocument, Canvas...).
    private static readonly System.Collections.Generic.List<RaycastResult> uiHits = new();

    private void LogUIBlockingClick()
    {
        var data = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        uiHits.Clear();
        EventSystem.current.RaycastAll(data, uiHits);

        string names = uiHits.Count == 0
            ? "(ningun raycast; probablemente un panel de UI Toolkit)"
            : string.Join(", ", uiHits.ConvertAll(h => h.gameObject != null ? h.gameObject.name : "?"));

        Debug.Log($"[Player] Clic ignorado: el raton esta sobre UI -> {names}", this);
    }

    private void MoveTo(Vector2 destination)
    {
        if (!canMove)
            return;

        agent.isStopped = false;

        if (NavMesh.SamplePosition(destination, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            if (!agent.isOnNavMesh || !agent.SetDestination(hit.position))
                Debug.Log($"[Player] No se pudo mover: agente en NavMesh = {agent.isOnNavMesh}, " +
                          $"posicion = {transform.position}.", this);
        }
        else
        {
            Debug.Log($"[Player] Clic fuera del NavMesh en {destination}.", this);
        }
    }

    // ---------------- INTERACCIÓN ----------------

    private void CheckInteraction()
    {
        if (!canMove || currentTarget == null || InteractionBlocker.IsBlocked)
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
            Vector2 direction = new Vector2(agent.velocity.x, agent.velocity.y).normalized;

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
        // Con el diario abierto, en un diálogo o con el ratón encima de la UI (diario, HUD, pausa…)
        // no se resaltan objetos del mundo: si no, sonaba el hover de las pistas que hay debajo.
        if (!canMove || InteractionBlocker.IsBlocked || IsPointerOverUI())
        {
            ClearHover();
            return;
        }

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        Collider2D hit = Physics2D.OverlapPoint(mousePos, interactableLayer);

        IInteractable newHover = null;

        if (hit != null)
            newHover = hit.GetComponent<IInteractable>();

        // Evita actualizar highlight innecesariamente
        if (hoveredInteractable == newHover)
            return;

        if (hoveredInteractable is IHighlightable oldHighlight)
            oldHighlight.SetHighlight(false);

        if (newHover is IHighlightable newHighlight)
            newHighlight.SetHighlight(true);

        hoveredInteractable = newHover;
    }

    private void ClearHover()
    {
        if (hoveredInteractable is IHighlightable highlight)
            highlight.SetHighlight(false);

        hoveredInteractable = null;
    }

    // Funciona también con UI Toolkit: los paneles se registran en el EventSystem.
    private static bool IsPointerOverUI()
    {
        return EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }

    // ---------------- UI / JOURNAL ----------------

    private void HandleJournalStateChanged(bool opened)
    {
        canMove = !opened;

        if (opened)
            ClearHover();

        if (agent != null)
        {
            agent.isStopped = opened;
            agent.ResetPath();
        }
    }
    public void ToggleInventory()
    {
        if (PauseMenuController.Instance != null &&
            PauseMenuController.Instance.IsPaused)
            return;

        if (JournalController.Instance == null)
            return;

        JournalController.Instance.ToggleJournal();
    }
    public void OpenMap()
    {
        if (!GameProgress.Instance.HasFlag(unlockCatacombsFlag))
            return;

        SceneController.Instance.LoadScene("SC_ParisMap");
    }
}