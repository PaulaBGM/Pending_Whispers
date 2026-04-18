using Inventory.Model;
using Inventory.UI;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace Inventory
{
    public class InventoryController : MonoBehaviour
    {
        [SerializeField] private UIInventoryPage inventoryUI;
        [SerializeField] private InventorySO inventoryData;
        private Inventory.Model.ItemType currentTab = Inventory.Model.ItemType.Misc;

        public List<InventoryItem> initialItems = new List<InventoryItem>();

        //[SerializeField] private AudioClip dropClip;
        [SerializeField] private AudioSource audioSource;

        private void Start()
        {
            PrepareUI();
            PrepareInventoryData();
        }

        private void PrepareInventoryData()
        {
            inventoryData.Initialize();
            inventoryData.OnInventoryUpdated += UpdateInventoryUI;

            foreach (InventoryItem item in initialItems)
            {
                if (!item.IsEmpty)
                    inventoryData.AddItem(item);
            }
        }

        private void UpdateInventoryUI(Dictionary<int, InventoryItem> inventoryState)
        {
            inventoryUI.ResetAllItems();

            foreach (var item in inventoryState)
            {
                inventoryUI.UpdateData(item.Key,
                    item.Value.item.ItemImage,
                    item.Value.quantity);
            }
        }

        private void PrepareUI()
        {
            inventoryUI.InitializeInventoryUI(inventoryData.Size);

            inventoryUI.OnDescriptionRequested += HandleDescriptionRequest;
            inventoryUI.OnSwapItems += HandleSwapItems;
            inventoryUI.OnStartDragging += HandleDragging;
            inventoryUI.OnItemActionRequested += HandleItemActionRequest;
            inventoryUI.OnTabChanged += HandleTabChanged;
        }
        private void HandleTabChanged(Inventory.Model.ItemType type)
        {
            currentTab = type;
            UpdateInventoryUIFiltered();
        }

        private void UpdateInventoryUIFiltered()
        {
            inventoryUI.ResetAllItems();

            var items = inventoryData.GetItemsByType(currentTab);

            for (int i = 0; i < items.Count; i++)
            {
                inventoryUI.UpdateData(i,
                    items[i].item.ItemImage,
                    items[i].quantity);
            }
        }
        private void HandleItemActionRequest(int itemIndex)
        {
            InventoryItem inventoryItem = inventoryData.GetItemAt(itemIndex);
            if (inventoryItem.IsEmpty)
                return;

            inventoryUI.ShowItemAction(itemIndex);

           /* if (inventoryItem.item is IItemAction action)
            {
                inventoryUI.AddAction(action.ActionName,
                    () => PerformAction(itemIndex));
            }

            if (inventoryItem.item is IDestroyableItem)
            {
                inventoryUI.AddAction("Drop",
                    () => DropItem(itemIndex, inventoryItem.quantity));
            }*/
        }

        private void DropItem(int itemIndex, int quantity)
        {
            inventoryData.RemoveItem(itemIndex, quantity);
            inventoryUI.ResetSelection();
            //audioSource.PlayOneShot(dropClip);
        }

        public void PerformAction(int itemIndex)
        {
            InventoryItem item = inventoryData.GetItemAt(itemIndex);
            if (item.IsEmpty)
                return;

           /* if (item.item is IItemAction action)
            {
                bool success = action.PerformAction(gameObject, item.itemState);

                if (success && item.item is IDestroyableItem)
                {
                    inventoryData.RemoveItem(itemIndex, 1);
                }

                audioSource.PlayOneShot(action.actionSFX);

                if (inventoryData.GetItemAt(itemIndex).IsEmpty)
                    inventoryUI.ResetSelection();
            }*/
        }

        private void HandleDragging(int itemIndex)
        {
            var item = inventoryData.GetItemAt(itemIndex);
            if (item.IsEmpty)
                return;

            inventoryUI.CreateDraggedItem(item.item.ItemImage, item.quantity);
        }

        private void HandleSwapItems(int a, int b)
        {
            inventoryData.SwapItems(a, b);
        }

        private void HandleDescriptionRequest(int index)
        {
            var item = inventoryData.GetItemAt(index);

            if (item.IsEmpty)
            {
                inventoryUI.ResetSelection();
                return;
            }

            inventoryUI.UpdateDescription(index,
                item.item.ItemImage,
                item.item.Name,
                PrepareDescription(item));
        }

        private string PrepareDescription(InventoryItem item)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine(item.item.Description);

            for (int i = 0; i < item.itemState.Count; i++)
            {
                sb.AppendLine(
                    $"{item.itemState[i].itemParameter.ParameterName}: " +
                    $"{item.itemState[i].value} / {item.item.DefaultParametersList[i].value}");
            }

            return sb.ToString();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (!inventoryUI.isActiveAndEnabled)
                {
                    inventoryUI.Show();
                    UpdateInventoryUI(inventoryData.GetCurrentInventoryState());
                }
                else
                {
                    inventoryUI.Hide();
                }
            }
        }
    }
}