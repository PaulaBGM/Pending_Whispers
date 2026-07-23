using System;
using System.Collections.Generic;
using UnityEngine;

namespace Inventory.UI
{
    public class CluePageController : JournalPageController
    {
        [Header("Inventory")]
        [SerializeField] private ClueEntryUI itemPrefab;
        [SerializeField] private RectTransform contentPanel;
        [SerializeField] private UIInventoryDescription itemDescription;
        [SerializeField] private MouseFollower mouseFollower;
        [SerializeField] private ItemActionPanel actionPanel;

        [Header("Tabs")]
        [SerializeField] private List<InventoryTab> tabs;

        private readonly List<ClueEntryUI> listOfUIItems = new();
        private int currentlyDraggedItemIndex = -1;

        public event Action<int> OnDescriptionRequested;
        public event Action<int> OnItemActionRequested;
        public event Action<int> OnStartDragging;
        public event Action<int, int> OnSwapItems;
        public event Action<Inventory.Model.ItemType> OnTabChanged;

        protected override void Awake()
        {
            base.Awake();

            mouseFollower.Toggle(false);

            if (itemDescription != null)
                itemDescription.ResetDescription();

            foreach (var tab in tabs)
                tab.OnTabSelected += HandleTabChanged;
        }

        private void HandleTabChanged(Inventory.Model.ItemType type)
        {
            OnTabChanged?.Invoke(type);
        }

        public override void Refresh()
        {
            ResetSelection();
        }

        public void InitializeInventoryUI(int size)
        {
            for (int i = 0; i < size; i++)
            {
                ClueEntryUI uiItem = Instantiate(itemPrefab, contentPanel);
                uiItem.transform.localScale = Vector3.one;

                listOfUIItems.Add(uiItem);

                uiItem.OnItemClicked += HandleItemSelection;
                uiItem.OnItemBeginDrag += HandleBeginDrag;
                uiItem.OnItemDroppedOn += HandleSwap;
                uiItem.OnItemEndDrag += HandleEndDrag;
                uiItem.OnRightMouseBtnClick += HandleShowItemActions;
            }
        }

        public void ResetAllItems()
        {
            listOfUIItems.RemoveAll(item => item == null);

            foreach (var item in listOfUIItems)
            {
                item.ResetData();
                item.Deselect();
            }

            ResetSelection();
        }

        public void UpdateData(int index, Sprite sprite, int quantity)
        {
            if (index < listOfUIItems.Count)
                listOfUIItems[index].SetData(sprite, quantity);
        }

        private void HandleItemSelection(ClueEntryUI item)
        {
            int index = listOfUIItems.IndexOf(item);

            if (index != -1)
                OnDescriptionRequested?.Invoke(index);
        }

        private void HandleShowItemActions(ClueEntryUI item)
        {
            int index = listOfUIItems.IndexOf(item);

            if (index != -1)
                OnItemActionRequested?.Invoke(index);
        }

        private void HandleBeginDrag(ClueEntryUI item)
        {
            int index = listOfUIItems.IndexOf(item);

            if (index == -1)
                return;

            currentlyDraggedItemIndex = index;
            OnStartDragging?.Invoke(index);
        }

        private void HandleEndDrag(ClueEntryUI item)
        {
            mouseFollower.Toggle(false);
            currentlyDraggedItemIndex = -1;
        }

        private void HandleSwap(ClueEntryUI item)
        {
            int index = listOfUIItems.IndexOf(item);

            if (index == -1)
                return;

            OnSwapItems?.Invoke(currentlyDraggedItemIndex, index);
        }

        public void CreateDraggedItem(Sprite sprite, int quantity)
        {
            mouseFollower.Toggle(true);
            mouseFollower.SetData(sprite, quantity);
        }

        public void AddAction(string name, Action action)
        {
            actionPanel.AddButton(name, action);
        }

        public void ShowItemAction(int index)
        {
            actionPanel.Toggle(true);
            actionPanel.transform.position = listOfUIItems[index].transform.position;
        }

        public void UpdateDescription(int index, Sprite img, string name, string desc)
        {
            itemDescription.SetDescription(img, name, desc);
        }

        public override void Show()
        {
            base.Show();
            ResetSelection();
        }

        public override void Hide()
        {
            actionPanel.Toggle(false);
            base.Hide();
        }

        public void ResetSelection()
        {
            itemDescription.ResetDescription();
        }
    }
}