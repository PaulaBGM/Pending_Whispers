using Inventory;
using Inventory.UI;
using UnityEngine;

public class InventoryPageController : MonoBehaviour
{
    [SerializeField] private UIInventoryPage ui;

    private void OnEnable()
    {
        ui.Show();
        Refresh();
    }

    private void OnDisable()
    {
        ui.Hide();
    }

    public void Refresh()
    {
        if (InventoryController.Instance == null)
        {
            Debug.LogWarning("InventoryController no inicializado aún");
            return;
        }

        InventoryController.Instance.RefreshUI();
    }
}