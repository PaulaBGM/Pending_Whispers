using Inventory.Model;
using UnityEngine;
using FMODUnity;

[RequireComponent(typeof(Collider2D))]
public class Item : MonoBehaviour, IInteractable
{
    [field: SerializeField]
    public ItemSO InventoryItem { get; private set; }

    [Header("Inspection")]
    [TextArea]
    [SerializeField] private string discoveryText;

    [SerializeField] private AudioSource audioSource;

    [Header("Highlight")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color highlightColor = Color.cyan;

    [Header("Persistence")]
    [Tooltip("Flag que marca este objeto como registrado. Sin él, el objeto vuelve a ser interactuable al recargar la escena o la partida.")]
    [SerializeField] private FlagSO persistenceFlag;

    [Header("FMOD")]
    [SerializeField] private EventReference highlightEvent;

    private Collider2D interactionCollider;
    private Color originalColor;
    private bool alreadyRegistered;
    private bool isHighlighted;

    private void Awake()
    {
        interactionCollider = GetComponent<Collider2D>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    private void OnEnable()
    {
        InteractionBlocker.OnBlockedChanged += HandleBlockedChanged;
    }

    private void OnDisable()
    {
        InteractionBlocker.OnBlockedChanged -= HandleBlockedChanged;
    }

    // En Start y no en Awake: al cargar partida, los flags se restauran
    // después de los Awake de la escena y antes de los Start.
    private void Start()
    {
        if (persistenceFlag != null &&
            GameProgress.Instance != null &&
            GameProgress.Instance.HasFlag(persistenceFlag))
        {
            MarkAsRegistered();
        }
    }

    public void Interact(PlayerController_Actions player)
    {
        if (alreadyRegistered || InteractionBlocker.IsBlocked)
            return;

        string textToShow = string.IsNullOrEmpty(discoveryText) ? InventoryItem.Name : discoveryText;
        UIGameEvents.OnDialogue?.Invoke(textToShow);

        player.Inventory.AddItem(InventoryItem, 1);
        UIGameEvents.RaiseEvidenceRegistered(InventoryItem);
        CameraFlash.Instance?.PlayFlash();
        UIGameEvents.OnFeedback?.Invoke("Evidence registered");

        if (persistenceFlag != null && GameProgress.Instance != null)
            GameProgress.Instance.AddFlag(persistenceFlag);

        MarkAsRegistered();
    }

    public Transform GetTransform()
    {
        return transform;
    }

    public void SetHighlight(bool value)
    {
        // Ni registrado ni con UI encima: nunca se resalta ni suena.
        if (value && (alreadyRegistered || InteractionBlocker.IsBlocked))
            value = false;

        if (spriteRenderer == null || value == isHighlighted)
            return;

        spriteRenderer.color = value ? highlightColor : originalColor;
        isHighlighted = value;

        if (value)
            RuntimeManager.PlayOneShot(highlightEvent, transform.position);
    }

    // Una vez registrado deja de ser interactuable: sin collider no recibe
    // hover ni clic. Para consultarlo, el jugador tiene que ir al cuaderno.
    private void MarkAsRegistered()
    {
        alreadyRegistered = true;
        SetHighlight(false);

        if (interactionCollider != null)
            interactionCollider.enabled = false;
    }

    // Si estaba resaltado al abrirse el cuaderno, se quita el resaltado.
    private void HandleBlockedChanged(bool blocked)
    {
        if (blocked)
            SetHighlight(false);
    }
}