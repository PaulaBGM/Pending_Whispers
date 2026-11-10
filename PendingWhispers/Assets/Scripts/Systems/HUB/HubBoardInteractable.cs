using FMODUnity;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class HubBoardInteractable : MonoBehaviour, IInteractable, IHighlightable
{
    [SerializeField] private HubBoardController board;

    [Header("Highlight")]
    [Tooltip("Sprite del tablero que se tiñe al pasar el ratón. Si se deja vacío se usa el primero de los hijos (Visual).")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color highlightColor = Color.cyan;

    [Header("FMOD")]
    [Tooltip("Opcional. Sonido al resaltar el tablero.")]
    [SerializeField] private EventReference highlightEvent;

    private Color originalColor;
    private bool isHighlighted;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    public void Interact(PlayerController_Actions player) => board.Open();
    public Transform GetTransform() => transform;

    public void SetHighlight(bool value)
    {
        if (spriteRenderer == null || value == isHighlighted)
            return;

        spriteRenderer.color = value ? highlightColor : originalColor;
        isHighlighted = value;

        if (value && !highlightEvent.IsNull)
            RuntimeManager.PlayOneShot(highlightEvent, transform.position);
    }

    private void OnDisable()
    {
        // Si el tablero se desactiva resaltado, que no se quede teñido al volver.
        SetHighlight(false);
    }
}
