using UnityEngine;
using Inventory;
using Inventory.Model;

/// <summary>
/// Pinta en la mano de Lyra el objeto de HeldItemState.
///
/// Montaje: en GO_Player_Main, crea un hijo de LyraVisual llamado "HeldItem" con un
/// SpriteRenderer (Order in Layer por encima del de Lyra), colócalo a mano sobre la mano
/// y arrástralo al campo heldRenderer. Este componente puede ir en el propio hijo o en el jugador.
///
/// Si el objeto sale del inventario (consumido, partida cargada...), se suelta solo.
/// </summary>
public class PlayerHeldItemView : MonoBehaviour
{
    [SerializeField] private SpriteRenderer heldRenderer;

    private InventorySO subscribedInventory;

    private void OnEnable()
    {
        HeldItemState.OnChanged += Refresh;
        TrySubscribeInventory();
        Refresh(HeldItemState.Current);
    }

    private void OnDisable()
    {
        HeldItemState.OnChanged -= Refresh;

        if (subscribedInventory != null)
            subscribedInventory.OnInventoryChanged -= HandleInventoryChanged;
        subscribedInventory = null;
    }

    private void Start()
    {
        // InventoryRuntime puede despertar después que el jugador.
        TrySubscribeInventory();
    }

    private void TrySubscribeInventory()
    {
        if (subscribedInventory != null || InventoryRuntime.Instance == null)
            return;

        subscribedInventory = InventoryRuntime.Instance.GetInventory();
        if (subscribedInventory != null)
            subscribedInventory.OnInventoryChanged += HandleInventoryChanged;
    }

    private void HandleInventoryChanged()
    {
        if (HeldItemState.IsHolding && !subscribedInventory.Contains(HeldItemState.Current))
            HeldItemState.Clear();
    }

    private void Refresh(ObjectItemSO item)
    {
        if (heldRenderer == null)
            return;

        heldRenderer.sprite = item != null ? item.ItemImage : null;
        heldRenderer.enabled = item != null;
    }
}
