using Inventory.Model;
using Inventory.UI;
using System.Collections.Generic;
using UnityEngine;

namespace Inventory
{
    public class InventoryController : MonoBehaviour
    {
        public static InventoryController Instance { get; private set; }

        [SerializeField] private UIInventoryPage inventoryUI;

        private InventorySO inventoryData;

        private ItemType currentTab = ItemType.Clue;
        private List<int> filteredIndices = new();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            PrepareInventoryData();
            PrepareUI();
        }

        private void PrepareInventoryData()
        {
            inventoryData = InventoryRuntime.Instance.GetInventory();
            inventoryData.OnInventoryUpdated += UpdateInventoryUIFiltered;
        }

        private void PrepareUI()
        {
            inventoryUI.InitializeInventoryUI(inventoryData.Size);

            inventoryUI.OnTabChanged += HandleTabChanged;
            inventoryUI.OnDescriptionRequested += HandleDescriptionRequest;
            inventoryUI.OnSwapItems += HandleSwapItems;
            inventoryUI.OnStartDragging += HandleDragging;
            inventoryUI.OnItemActionRequested += HandleItemActionRequest;
        }

        //SOLO FILTRO DE DATOS
        private void HandleTabChanged(ItemType type)
        {
            currentTab = type;
            RefreshUI();
        }

        public void ShowInventoryData()
        {
            Debug.Log("Refreshing inventory UI");
            RefreshUI();
        }

        public void RefreshUI()
        {
            UpdateInventoryUIFiltered(inventoryData.GetCurrentInventoryState());
        }

        private void UpdateInventoryUIFiltered(Dictionary<int, InventoryItem> inventoryState)
        {
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