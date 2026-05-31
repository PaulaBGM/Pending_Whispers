using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class ZOrder : MonoBehaviour
{
    [SerializeField] private Transform anchor;
    private SpriteRenderer sprite;

    private void Awake()
    {
        sprite = GetComponent<SpriteRenderer>();
    }

    private void LateUpdate()
    {
        float y = anchor != null ? anchor.position.y : transform.position.y;
        sprite.sortingOrder = -(int)(anchor.position.y * 1000);    
    }
}