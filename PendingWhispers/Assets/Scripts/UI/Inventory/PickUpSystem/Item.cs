using Inventory.Model;
using UnityEngine;
using FMODUnity;

[RequireComponent(typeof(Collider2D))]
public class Item : MonoBehaviour, IInteractable, IHighlightable
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

    [Tooltip("Solo objetos de inventario: al recogerlo desaparece de la escena (y sigue oculto al volver o cargar partida).")]
    [SerializeField] private bool hideOnPickup = true;

    [Header("Spectral Lock")]
    [Tooltip("Si se asigna (o hay un SpectralTrace en el mismo objeto), la pista solo se puede recoger " +
             "después de revelarla con la habilidad de detección de Lyra.")]
    [SerializeField] private SpectralTrace requiredTrace;

    [Header("FMOD")]
    [SerializeField] private EventReference highlightEvent;
    private bool isHighlighted;

    /// <summary>False mientras la pista oculta no haya sido revelada por la onda espectral.</summary>
    public bool CanInteract => requiredTrace == null || requiredTrace.IsRevealed;

    /// <summary>
    /// Ya recogida en esta partida. Se consulta al interactuar y no en Awake: al cargar una
    /// partida los flags se restauran DESPUES del Awake de la escena (SaveSystem, sceneLoaded),
    /// asi que en Awake la pista parecia sin recoger y se podia volver a registrar.
    /// </summary>
    private bool IsAlreadyRegistered =>
        alreadyRegistered ||
        (persistenceFlag != null && GameProgress.Instance != null && GameProgress.Instance.HasFlag(persistenceFlag)) ||
        (InventoryRuntime.Instance != null &&
         InventoryRuntime.Instance.GetInventory() != null && InventoryRuntime.Instance.GetInventory().Contains(InventoryItem));

    /// <summary>Pista o testimonio (evidencia) frente a objeto de inventario usable.</summary>
    private bool IsObject => InventoryItem is ObjectItemSO;

    private void Awake()
    {
        if (requiredTrace == null)
            requiredTrace = GetComponent<SpectralTrace>();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    // En Start y no en Awake: las flags de la partida cargada ya estan restauradas aqui.
    private void Start()
    {
        if (hideOnPickup && IsObject && IsAlreadyRegistered)
            gameObject.SetActive(false);
    }

    public void Interact(PlayerController_Actions player)
    {
        // Pista oculta sin revelar: el clic no hace nada, como si fuera suelo.
        if (!CanInteract)
            return;

        if (IsAlreadyRegistered)
        {
            UIGameEvents.OnFeedback?.Invoke(IsObject
                ? "You already picked this up."
                : "You have already examined this evidence.");
            return;
        }

        // AddItem devuelve lo que NO ha cabido. Si no cabe, no se marca como recogido
        // para que el jugador pueda volver a intentarlo.
        int notAdded = player.Inventory.AddItem(InventoryItem, 1);
        if (notAdded > 0)
        {
            Debug.LogWarning($"[Item] Inventario lleno: '{InventoryItem.name}' no se ha aÃ±adido. Sube Size en el InventorySO.", this);
            UIGameEvents.OnFeedback?.Invoke("Your inventory is full.");
            return;
        }

        alreadyRegistered = true;
        string textToShow = string.IsNullOrEmpty(discoveryText) ? InventoryItem.Name : discoveryText;
        UIGameEvents.OnDialogue?.Invoke(textToShow);

        if (IsObject)
        {
            UIGameEvents.OnFeedback?.Invoke("Item added to inventory");
        }
        else
        {
            UIGameEvents.RaiseEvidenceRegistered(InventoryItem);
            CameraFlash.Instance?.PlayFlash();
            UIGameEvents.OnFeedback?.Invoke("Evidence registered");
        }

        if (persistenceFlag != null)
            GameProgress.Instance.AddFlag(persistenceFlag);
        else if (IsObject)
            Debug.LogWarning($"[Item] '{name}' es un objeto sin persistenceFlag: se podrÃ¡ volver a recoger tras usarlo o al recargar la escena.", this);

        if (hideOnPickup && IsObject)
        {
            SetHighlight(false);
            gameObject.SetActive(false);
        }
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