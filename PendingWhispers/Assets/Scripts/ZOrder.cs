using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZOrder : MonoBehaviour
{
    [SerializeField] private Transform anchor;  // Referencia al objeto de anclaje para determinar el orden de renderizado
    private SpriteRenderer sprite; 

    void Start()
    {
        sprite = GetComponent<SpriteRenderer>();  
    }

    void Update()
    {
        // Cambia el orden de renderizado dependiendo de la posición en el eje Y (más bajo en Y = más cerca al fondo)
        if (anchor == null)
        {
            sprite.sortingOrder = (int)(transform.position.y * -10);  
        }
        else
        {
            sprite.sortingOrder = (int)(anchor.position.y * -10);
        }
    }
}
