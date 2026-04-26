using Inventory.Model;
using Inventory.UI;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Inventory
{
    public class InventoryController : MonoBehaviour
    {
        [SerializeField] private UIInventoryPage inventoryUI;
        [SerializeField] private InventorySO inventoryData;

        private ItemType currentTab = ItemType.Clue;
        private List<int> filteredIndices = new();

        private void Start()
        {
            PrepareUI();
            PrepareInventoryData();
        }

        private void PrepareInventoryData()
        {
            inventoryData.Initialize();
            inventoryData.OnInventoryUpdated += UpdateInventoryUIFiltered;
        }

        private void OnDestroy()
        {
            if (inventoryData != null)
                inventoryData.OnInventoryUpdated -= UpdateInventoryUIFiltered;
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

        private void HandleTabChanged(ItemType type)
        {
            currentTab = type;
            UpdateInventoryUIFiltered(inventoryData.GetCurrentInventoryState());
        }

        private void UpdateInventoryUIFiltered(Dictionary<int, InventoryItem> inventoryState)
        {
            if (inventoryUI == null || !inventoryUI.gameObject.activeInHierarchy)
                return;

            inventoryUI.ResetAllItems();
            filteredIndices.Clear();

            int uiIndex = 0;

            foreach (var kvp in inventoryState)
            {
                if (kvp.Value.item.ItemType == currentTab)
                {
                    filteredIndices.Add(kvp.Key);

                    inventoryUI.UpdateData(
                        uiIndex,
                        kvp.Value.item.ItemImage,
                        kvp.Value.quantity
                    );

                    uiIndex++;
                }
            }
        }

        private InventoryItem GetItemByFilteredIndex(int index)
        {
            if (index < 0 || index >= filteredIndices.Count)
                return InventoryItem.GetEmptyItem();

            return inventoryData.GetItemAt(filteredIndices[index]);
        }

        private void HandleItemActionRequest(int index)
        {
            var item = GetItemByFilteredIndex(index);
            if (item.IsEmpty) return;

            inventoryUI.ShowItemAction(index);
        }

        private void HandleDragging(int index)
        {
            var item = GetItemByFilteredIndex(index);
            if (item.IsEmpty) return;

            inventoryUI.CreateDraggedItem(item.item.ItemImage, item.quantity);
        }

        private void HandleSwapItems(int a, int b)
        {
            if (a < 0 || b < 0) return;
            if (a >= filteredIndices.Count || b >= filteredIndices.Count) return;

            inventoryData.SwapItems(filteredIndices[a], filteredIndices[b]);
        }

        private void HandleDescriptionRequest(int index)
        {
            var item = GetItemByFilteredIndex(index);

            if (item.IsEmpty)
            {
                inventoryUI.ResetSelection();
                return;
            }

            inventoryUI.UpdateDescription(
                index,
                item.item.ItemImage,
                item.item.Name,
                item.item.Description
            );
        }
    }
}