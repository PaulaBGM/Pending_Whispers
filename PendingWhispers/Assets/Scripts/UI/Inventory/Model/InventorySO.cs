using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Inventory.Model
{
    [CreateAssetMenu]
    public class InventorySO : ScriptableObject
    {
        [SerializeField] private List<InventoryItem> inventoryItems;

        [field: SerializeField] public int Size { get; private set; } = 10;
        public event Action<Dictionary<int, InventoryItem>> OnInventoryUpdated;
        public event Action OnInventoryChanged;

        [Header("Progression")]
        [SerializeField] private List<string> requiredClueIDs;
        [SerializeField] private FlagSO completedFlag;
        private bool progressionCompleted = false;

        public void Initialize()
        {
            inventoryItems = new List<InventoryItem>();

            for (int i = 0; i < Size; i++)
                inventoryItems.Add(InventoryItem.GetEmptyItem());
        }

        public int AddItem(ItemSO item, int quantity)
        {
            if (item is ClueItemSO)
            {
                if (Contains(item))
                    return 0;
                quantity = 1;
            }

            while (quantity > 0 && !IsInventoryFull())
            {
                quantity -= AddItemToFirstFreeSlot(item, 1);
            }

            InformAboutChange();
            CheckClueProgression();
            return quantity;
        }

        private int AddItemToFirstFreeSlot(ItemSO item, int quantity)
        {
            InventoryItem newItem = new InventoryItem
            {
                item = item,
                quantity = quantity,
                itemState = new List<ItemParameter>()
            };

            for (int i = 0; i < inventoryItems.Count; i++)
            {
                if (inventoryItems[i].IsEmpty)
                {
                    inventoryItems[i] = newItem;
                    return quantity;
                }
            }

            return 0;
        }

        private bool IsInventoryFull()
        {
            return !inventoryItems.Any(item => item.IsEmpty);
        }

        public void RemoveItem(int index)
        {
            inventoryItems[index] = InventoryItem.GetEmptyItem();
            InformAboutChange();
        }

        public InventoryItem GetItemAt(int index) => inventoryItems[index];

        public Dictionary<int, InventoryItem> GetCurrentInventoryState()
        {
            Dictionary<int, InventoryItem> result = new();

            for (int i = 0; i < inventoryItems.Count; i++)
            {
                if (!inventoryItems[i].IsEmpty)
                    result[i] = inventoryItems[i];
            }

            return result;
        }

        public List<InventoryItem> GetItemsByType(ItemType type)
        {
            return inventoryItems
                .Where(i => !i.IsEmpty && i.item.ItemType == type)
                .ToList();
        }

        public bool Contains(ItemSO item)
        {
            return inventoryItems.Any(i => !i.IsEmpty && i.item == item);
        }
        
        public void SwapItems(int indexA, int indexB)
        {
            if (indexA < 0 || indexA >= inventoryItems.Count) return;
            if (indexB < 0 || indexB >= inventoryItems.Count) return;

            InventoryItem temp = inventoryItems[indexA];
            inventoryItems[indexA] = inventoryItems[indexB];
            inventoryItems[indexB] = temp;

            InformAboutChange();
        }

        private void InformAboutChange()
        {
            OnInventoryUpdated?.Invoke(GetCurrentInventoryState());

            Debug.Log("[Inventory] Actualizado");

            OnInventoryChanged?.Invoke(); 

            CheckClueProgression();
        }

        private void CheckClueProgression()
        {
            if (progressionCompleted) return;

            foreach (var requiredID in requiredClueIDs)
            {
                bool found = false;

                foreach (var item in inventoryItems)
                {
                    if (!item.IsEmpty && item.item is ClueItemSO clue)
                    {
                        if (clue.DiscoveryFlag != null &&
                            GameProgress.Instance.HasFlag(clue.DiscoveryFlag))
                        {
                            found = true;
                            break;
                        }
                    }
                }

                if (!found)
                    return;
            }

            progressionCompleted = true;

            Debug.Log("[Inventory] Todas las pistas recogidas");

            GameProgress.Instance.AddFlag(completedFlag);
        }
    }

    [Serializable]
    public struct InventoryItem
    {
        public int quantity;
        public ItemSO item;
        public List<ItemParameter> itemState;

        public bool IsEmpty => item == null;

        public static InventoryItem GetEmptyItem()
        {
            return new InventoryItem
            {
                item = null,
                quantity = 0,
                itemState = new List<ItemParameter>()
            };
        }
    }
}