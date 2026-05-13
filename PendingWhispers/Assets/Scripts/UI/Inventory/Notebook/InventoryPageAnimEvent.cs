using UnityEngine;

public class InventoryPageAnimEvent : MonoBehaviour
{
    // Se llama desde Animation Event al final del clip
    public void OnAnimationFinished()
    {
        gameObject.SetActive(false);
    }
}
