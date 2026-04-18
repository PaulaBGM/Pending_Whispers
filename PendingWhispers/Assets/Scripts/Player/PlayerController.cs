using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private LayerMask groundLayer;
    [SerializeField] private LayerMask interactableLayer;
    [SerializeField] private LayerMask collisionLayer;
    [SerializeField] private float collisionCheckDistance = 0.1f;
    
    private Vector2 target;
    private Animator animator;
    private Vector2 lastDirection;
    
    public float speed = 5f;

    public float interactDistance = 1.5f;

    private IInteractable currentTarget;

    public bool canMove = true;
    
    public static Action<PlayerController> OnPlayerSpawned;

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
        InputController.Instance.OnClickPressed += HandleClick;
    }

    private void OnDisable()
    {
        InputController.Instance.OnClickPressed -= HandleClick;
    }

    void Update()
    {
        Move();
        CheckInteraction();
    }

    /*void HandleClick()
    {
        if (!canMove) return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero);

        if (hit.collider != null)
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                currentTarget = interactable;
                target = interactable.GetTransform().position;
                return;
            }
        }

        currentTarget = null;
        target = mousePos;
    }*/
    
    //PARA QUE NO SE SALGA DEL SUELO
    void HandleClick()
    {
        if (!canMove) return;

        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            return;

        Vector2 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);

        //Intentar interactuar
        RaycastHit2D hit = Physics2D.Raycast(mousePos, Vector2.zero, Mathf.Infinity, interactableLayer);
        if (hit.collider != null)
        {
            IInteractable interactable = hit.collider.GetComponent<IInteractable>();

            if (interactable != null)
            {
                currentTarget = interactable;
                target = interactable.GetTransform().position;
                return; // 🔥 IMPORTANTE
            }
        }

        //Movimiento solo si es suelo
        RaycastHit2D groundHit = Physics2D.Raycast(mousePos, Vector2.zero, Mathf.Infinity, groundLayer);

        if (groundHit.collider != null)
        {
            currentTarget = null;
            target = groundHit.point;
            return; // 🔥 IMPORTANTE
        }

        //Si no es NPC ni suelo → no hacer nada
    }

    /*void Move()
    {
        if (!canMove) return;

        Vector2 currentPosition = transform.position;

        // Movimiento
        Vector2 newPosition = Vector2.MoveTowards(currentPosition, target, speed * Time.deltaTime);

        transform.position = newPosition;

        // Dirección hacia el objetivo
        Vector2 direction = (target - currentPosition).normalized;

        // ¿Se está moviendo realmente?
        bool isMoving = Vector2.Distance(currentPosition, target) > 0.01f;

        if (isMoving)
        {
            lastDirection = direction;
        }

        // Uso de lastDirection para no perder idle
        animator.SetFloat("moveX", lastDirection.x);
        animator.SetFloat("moveY", lastDirection.y);
        animator.SetBool("isMoving", isMoving);
    }*/
    
    //PARA EVOTAR QUE TRASPASE OBJETOS
    void Move()
    {
        if (!canMove) return;

        Vector2 currentPosition = transform.position;

        Vector2 direction = (target - currentPosition).normalized;

        Vector2 nextPosition = Vector2.MoveTowards(currentPosition, target, speed * Time.deltaTime);

        Vector2 moveDir = nextPosition - currentPosition;

        //Comprobar colisión
        RaycastHit2D hit = Physics2D.BoxCast(currentPosition, new Vector2(0.8f, 0.8f), 0f, moveDir.normalized, moveDir.magnitude, collisionLayer);

        bool isMoving = moveDir.magnitude > 0.0001f && hit.collider == null;

        if (hit.collider == null)
        {
            transform.position = nextPosition;
        }

        // Dirección animación
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
            currentTarget.Interact();
            currentTarget = null;
        }
    }
}