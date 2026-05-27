using System.Collections.Generic;
using UnityEngine;
using Inventory.Model;
using Inventory.UI;

namespace Inventory
{
    public class InventoryController : MonoBehaviour
    {
        public static InventoryController Instance { get; private set; }

        [SerializeField] private UIInventoryPage inventoryUI;

        private InventorySO inventoryData;

        private ItemType currentTab = ItemType.Clue;
        private List<int> filteredIndices = new();

        private Dictionary<int, InventoryItem> lastState;
        private bool hasPendingUpdate;

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

        private void OnEnable()
        {
            if (inventoryData != null)
                inventoryData.OnInventoryUpdated += QueueRefresh;
        }

        private void OnDisable()
        {
            if (inventoryData != null)
                inventoryData.OnInventoryUpdated -= QueueRefresh;
        }

        private void PrepareInventoryData()
        {
            inventoryData = InventoryRuntime.Instance.GetInventory();

            if (inventoryData != null)
                inventoryData.OnInventoryUpdated += QueueRefresh;
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

        // =========================
        // TAB CHANGE
        // =========================
        private void HandleTabChanged(ItemType type)
        {
            currentTab = type;

            RefreshUI();
        }

        // =========================
        // PUBLIC ENTRY
        // =========================
        public void RefreshUI()
        {
            if (inventoryData == null) return;

            QueueRefresh(inventoryData.GetCurrentInventoryState());
        }

        // =========================
        // QUEUE (SAFE)
        // =========================
        private void QueueRefresh(Dictionary<int, InventoryItem> state)
        {
            if (state == null) return;

            // snapshot seguro
            lastState = new Dictionary<int, InventoryItem>(state);
            hasPendingUpdate = true;
        }

        // =========================
        // SAFE UPDATE LOOP
        // =========================
        private void Update()
        {
            if (!hasPendingUpdate) return;
            if (inventoryUI == null) return;
            if (!inventoryUI.gameObject.activeInHierarchy) return;

            hasPendingUpdate = false;

            ApplyRefresh();
        }

        // =========================
        // UI RENDER
        // =========================
        private void ApplyRefresh()
        {
            if (lastState == null) return;

            inventoryUI.ResetAllItems();
            filteredIndices.Clear();

            int uiIndex = 0;

            foreach (var kvp in lastState)
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

        // =========================
        // ITEM HELPERS
        // =========================
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

        private void OnDestroy()
        {
            if (inventoryData != null)
                inventoryData.OnInventoryUpdated -= QueueRefresh;

            if (Instance == this)
                Instance = null;
        }
    }
}