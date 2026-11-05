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

    private Color originalColor;

    [Header("Persistence")]
    [SerializeField] private FlagSO persistenceFlag;

    private bool alreadyRegistered;

    [Header("Spectral Lock")]
    [Tooltip("Si se asigna (o hay un SpectralTrace en el mismo objeto), la pista solo se puede recoger " +
             "después de revelarla con la habilidad de detección de Lyra.")]
    [SerializeField] private SpectralTrace requiredTrace;

    [Header("FMOD")]
    [SerializeField] private EventReference highlightEvent;
    private bool isHighlighted;

    /// <summary>False mientras la pista oculta no haya sido revelada por la onda espectral.</summary>
    public bool CanInteract => requiredTrace == null || requiredTrace.IsRevealed;

    private void Awake()
    {
        if (persistenceFlag != null &&
            GameProgress.Instance.HasFlag(persistenceFlag))
        {
            alreadyRegistered = true;
        }

        if (requiredTrace == null)
            requiredTrace = GetComponent<SpectralTrace>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    public void Interact(PlayerController_Actions player)
    {
        // Pista oculta sin revelar: el clic no hace nada, como si fuera suelo.
        if (!CanInteract)
            return;

        if (alreadyRegistered)
        {
            UIGameEvents.OnFeedback?.Invoke("You have already examined this evidence.");
            return;
        }

        alreadyRegistered = true;
        string textToShow = string.IsNullOrEmpty(discoveryText) ? InventoryItem.Name : discoveryText;
        UIGameEvents.OnDialogue?.Invoke(textToShow);
        player.Inventory.AddItem(InventoryItem, 1);
        UIGameEvents.RaiseEvidenceRegistered(InventoryItem);
        CameraFlash.Instance?.PlayFlash();
        UIGameEvents.OnFeedback?.Invoke("Evidence registered");

        if (persistenceFlag != null)
            GameProgress.Instance.AddFlag(persistenceFlag);
    }

    public Transform GetTransform()
    {
        return transform;
    }

    public void SetHighlight(bool value)
    {
        if (spriteRenderer == null)
            return;

        // El hover no debe delatar una pista oculta que aún no se ha revelado.
        if (value && !CanInteract)
            value = false;

        if (value == isHighlighted)
            return;

        spriteRenderer.color = value ? highlightColor : originalColor;
        isHighlighted = value;

        if (value)
        {
            RuntimeManager.PlayOneShot(highlightEvent, transform.position);
        }
    }
}