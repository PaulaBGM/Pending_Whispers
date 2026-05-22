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
        InventoryController.Instance.RefreshUI();
    }
}