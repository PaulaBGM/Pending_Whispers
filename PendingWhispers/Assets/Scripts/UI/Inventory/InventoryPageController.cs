using Inventory;
using Inventory.UI;
using UnityEngine;

public class InventoryPageController : JournalPageController
{
    [SerializeField] private CluePageController ui;

    public override void Refresh()
    {
        if (InventoryController.Instance == null)
        {
            Debug.LogWarning("InventoryController no inicializado aún");
            return;
        }

        InventoryController.Instance.RefreshUI();
    }

    public override void Show()
    {
        base.Show();
        ui.Show();
    }

    public override void Hide()
    {
        ui.Hide();
        base.Hide();
    }
}