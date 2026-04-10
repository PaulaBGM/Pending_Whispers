using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private Vector2 target;
    public float speed = 5f;

    public float interactDistance = 1.5f;

    private IInteractable currentTarget;
    
    /*CON INPUT ACTION*/
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
        /*if (Input.GetMouseButtonDown(0))
        {
            HandleClick();
        }*/

        Move();
        CheckInteraction();
    }
    
    void HandleClick()
    {
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
        transform.position = Vector2.MoveTowards(
            transform.position,
            target,
            speed * Time.deltaTime
        );
    }

    void CheckInteraction()
    {
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