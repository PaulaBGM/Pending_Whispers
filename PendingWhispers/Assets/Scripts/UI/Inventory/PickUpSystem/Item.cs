using Inventory.Model;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class Item : MonoBehaviour, IInteractable
{
    [field: SerializeField]
    public ItemSO InventoryItem { get; private set; }

    [field: SerializeField]
    public int Quantity { get; set; } = 1;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float duration = 0.3f;

    [Header("Highlight")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color highlightColor = Color.cyan;

    private Color originalColor;

    [Header("Persistence")]
    [SerializeField] private FlagSO persistenceFlag;

    private void Awake()
    {
        if (persistenceFlag != null &&
            GameProgress.Instance.HasFlag(persistenceFlag))
        {
            Debug.Log("[Item] Ya recogido, destruyendo: " + persistenceFlag.id);
            Destroy(gameObject);
            return;
        }

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        originalColor = spriteRenderer.color;
    }

    public void Interact(PlayerController player)
    {

        InventorySO inventory = player.Inventory;
        int remainder = inventory.AddItem(InventoryItem, Quantity);

        if (remainder == 0)
        {
            if (persistenceFlag != null)
            {
                UIGameEvents.OnItemCollected?.Invoke(InventoryItem);
                GameProgress.Instance.AddFlag(persistenceFlag);
            }

            DestroyItem();
        }
        else
        {
            UIGameEvents.OnFeedback?.Invoke("Inventario lleno");
            Quantity = remainder;
        }
    }

    public Transform GetTransform()
    {
        return transform;
    }

    public void SetHighlight(bool value)
    {
        spriteRenderer.color = value ? highlightColor : originalColor;
    }

    public void DestroyItem()
    {
        GetComponent<Collider2D>().enabled = false;
        StartCoroutine(AnimateItemPickup());
    }

    private IEnumerator AnimateItemPickup()
    {
        if (audioSource != null)
            audioSource.Play();

        Vector3 startScale = transform.localScale;
        Vector3 endScale = Vector3.zero;
        float t = 0;

        while (t < duration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, endScale, t / duration);
            yield return null;
        }

        Destroy(gameObject);
    }
}