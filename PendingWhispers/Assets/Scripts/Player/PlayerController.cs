using UnityEngine;
using UnityEngine.EventSystems;

public class PlayerController : MonoBehaviour
{
    private Vector2 target;
    public float speed = 5f;

    public float interactDistance = 1.5f;

    private IInteractable currentTarget;

    public bool canMove = true;

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

    void HandleClick()
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
    }

    void Move()
    {
        if (!canMove) return;

        transform.position = Vector2.MoveTowards(
            transform.position,
            target,
            speed * Time.deltaTime
        );
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