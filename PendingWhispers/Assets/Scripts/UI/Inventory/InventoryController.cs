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

        private int selectedItemIndex = -1;

        public List<InventoryItem> initialItems = new();

        private void Start()
        {
            inventoryData.Initialize();
            inventoryData.OnInventoryUpdated += UpdateInventoryUI;

            inventoryUI.InitializeInventoryUI(inventoryData.Size);

            inventoryUI.OnDescriptionRequested += HandleDescriptionRequest;
            inventoryUI.OnItemActionRequested += HandleItemActionRequest;
            inventoryUI.OnSwapItems += (a, b) => { };

            foreach (var item in initialItems)
            {
                if (!item.IsEmpty)
                    inventoryData.AddItem(item.item, item.quantity);
            }
        }

        private void UpdateInventoryUI(Dictionary<int, InventoryItem> state)
        {
            inventoryUI.ResetAllItems();

            foreach (var item in state)
            {
                inventoryUI.UpdateData(item.Key,
                    item.Value.item.ItemImage,
                    item.Value.quantity);
            }
        }

        private void HandleDescriptionRequest(int index)
        {
            selectedItemIndex = index;

            var item = inventoryData.GetItemAt(index);

            if (item.IsEmpty)
            {
                inventoryUI.ResetSelection();
                return;
            }

            inventoryUI.UpdateDescription(index,
                item.item.ItemImage,
                item.item.Name,
                item.item.Description);
        }

        private void HandleItemActionRequest(int index)
        {
            var item = inventoryData.GetItemAt(index);
            if (item.IsEmpty) return;

            inventoryUI.ShowItemAction(index);

            inventoryUI.AddAction("Seleccionar", () =>
            {
                selectedItemIndex = index;
            });

            inventoryUI.AddAction("Vincular", () =>
            {
                if (selectedItemIndex != -1 && selectedItemIndex != index)
                {
                    var a = inventoryData.GetItemAt(selectedItemIndex);
                    var b = inventoryData.GetItemAt(index);

                    inventoryData.TryLinkItems(a.item, b.item);
                }
            });

            inventoryUI.AddAction("Presentar", () =>
            {
                Debug.Log("Presentando: " + item.item.Name);
            });

            if (item.item is SampleItemSO sample)
            {
                inventoryUI.AddAction("Analizar", () =>
                {
                    if (!sample.IsAnalyzed)
                    {
                        sample.IsAnalyzed = true;
                        Debug.Log(sample.AnalysisResult);
                    }
                });
            }
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