using Inventory.Model;
using UnityEngine;

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

    [Header("Spectral Detection")]
    [SerializeField] private bool spectralOnly;

    private Collider2D itemCollider;

    private bool alreadyRegistered;

    private void Awake()
    {
        itemCollider = GetComponent<Collider2D>();

        // Persistencia
        if (persistenceFlag != null && GameProgress.Instance.HasFlag(persistenceFlag))
        {
            alreadyRegistered = true;
        }

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;

        // Detectar evidencia espectral
        ClueItemSO clue = InventoryItem as ClueItemSO;

        if (clue != null)
        {
            spectralOnly = clue.HasSpectralTrace;
        }

        // Ocultar si es espectral
        if (spectralOnly)
        {
            HideSpectralItem();
        }
    }

    private void Update()
    {
        HandleSpectralVisibility();
    }

    private void HandleSpectralVisibility()
    {
        if (!spectralOnly)
            return;

        if (SpectralDetectionSystem.Instance == null)
            return;

        if (SpectralDetectionSystem.Instance.DetectionActive)
        {
            ShowSpectralItem();
        }
        else
        {
            HideSpectralItem();
        }
    }

    private void ShowSpectralItem()
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = true;

        if (itemCollider != null)
            itemCollider.enabled = true;
    }

    private void HideSpectralItem()
    {
        if (spriteRenderer != null)
            spriteRenderer.enabled = false;

        if (itemCollider != null)
            itemCollider.enabled = false;
    }

    public void Interact(PlayerController_MovementInteraction player)
    {
        if (alreadyRegistered)
        {
            UIGameEvents.OnFeedback?.Invoke("Ya has examinado esta evidencia.");
            return;
        }

        alreadyRegistered = true;

        // 1. Mostrar diálogo
        string textToShow = string.IsNullOrEmpty(discoveryText)
            ? InventoryItem.name
            : discoveryText;

        UIGameEvents.OnDialogue?.Invoke(textToShow);

        // 2. Abrir journal automáticamente
        JournalController.Instance.OpenToCluesTab();

        // 3. Registrar en journal
        player.Inventory.AddItem(InventoryItem, 1);

        // 4. feedback opcional
        UIGameEvents.OnFeedback?.Invoke("Evidencia registrada");

        // 5. persistencia
        if (persistenceFlag != null)
            GameProgress.Instance.AddFlag(persistenceFlag);
    }

    private void RegisterEvidence(PlayerController_MovementInteraction player)
    {
        InventorySO inventory = player.Inventory;

        // Evita duplicados
        if (inventory.Contains(InventoryItem))
            return;

        inventory.AddItem(InventoryItem, 1);
    }

    public Transform GetTransform()
    {
        return transform;
    }

    public void SetHighlight(bool value)
    {
        if (spriteRenderer == null)
            return;

        spriteRenderer.color = value ? highlightColor : originalColor;
    }
}