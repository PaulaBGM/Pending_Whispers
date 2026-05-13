using UnityEngine;
using Inventory;

public class InventoryAnimationEvents : MonoBehaviour
{
    [SerializeField] private GameObject tabs;
    
    private void Awake()
    {
        tabs.SetActive(false);
    }
    public void OnOpenAnimationFinished()
    {
        if (InventoryController.Instance != null)
        {
            InventoryController.Instance.ShowInventoryData();
            tabs.SetActive(true);
        }
    }
}