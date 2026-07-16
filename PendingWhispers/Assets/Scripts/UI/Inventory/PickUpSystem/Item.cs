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
    
    [Header("FMOD")]
    [SerializeField] private EventReference highlightEvent;
    private bool isHighlighted;

    private void Awake()
    {
        if (persistenceFlag != null &&
            GameProgress.Instance.HasFlag(persistenceFlag))
        {
            alreadyRegistered = true;
        }

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    public void Interact(PlayerController_Actions player)
    {
        if (alreadyRegistered)
        {
            UIGameEvents.OnFeedback?.Invoke("You have already examined this evidence." );
            return;
        }
        alreadyRegistered = true;
        string textToShow = string.IsNullOrEmpty(discoveryText)? InventoryItem.name: discoveryText;
        UIGameEvents.OnDialogue?.Invoke(textToShow);
        player.Inventory.AddItem(InventoryItem, 1);
        HUDController hud = FindFirstObjectByType<HUDController>();
        hud?.AddClueNotification();
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