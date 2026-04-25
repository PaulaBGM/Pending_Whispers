using Inventory.Model;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class Item : MonoBehaviour
{
    [field: SerializeField]
    public ItemSO InventoryItem { get; private set; }

    [field: SerializeField]
    public int Quantity { get; set; } = 1;

    [SerializeField] private AudioSource audioSource;
    [SerializeField] private float duration = 0.3f;

    [Header("Highlight Settings")]
    [SerializeField] private Color highlightColor = Color.cyan;
    [SerializeField] private float pulseSpeed = 3f;
    [SerializeField] private bool requiresDetectionVision = true;

    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private bool isHighlighted = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalColor = spriteRenderer.color;
    }

    private void Start()
    {
        spriteRenderer.sprite = InventoryItem.ItemImage;
    }

    private void Update()
    {
        // efecto pulso
        if (isHighlighted)
        {
            float t = Mathf.PingPong(Time.time * pulseSpeed, 1f);
            spriteRenderer.color = Color.Lerp(originalColor, highlightColor, t);
        }
    }

    public void SetHighlight(bool value)
    {
        if (requiresDetectionVision)
        {
            isHighlighted = value;
        }
        else
        {
            isHighlighted = false;
        }

        if (!isHighlighted)
        {
            spriteRenderer.color = originalColor;
        }
    }

    public void DestroyItem()
    {
        GetComponent<Collider2D>().enabled = false;
        StartCoroutine(AnimateItemPickup());
    }

    private IEnumerator AnimateItemPickup()
    {
        audioSource.Play();

        Vector3 startScale = transform.localScale;
        Vector3 endScale = Vector3.zero;
        float currentTime = 0;

        while (currentTime < duration)
        {
            currentTime += Time.deltaTime;
            transform.localScale =
                Vector3.Lerp(startScale, endScale, currentTime / duration);
            yield return null;
        }

        Destroy(gameObject);
    }
}