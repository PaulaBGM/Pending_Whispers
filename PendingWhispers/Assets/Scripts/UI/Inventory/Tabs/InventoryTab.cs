using UnityEngine;
using UnityEngine.UI;
using System;

namespace Inventory.UI
{
    public class InventoryTab : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private Inventory.Model.ItemType tabType;

        public event Action<Inventory.Model.ItemType> OnTabSelected;

        private void Awake()
        {
            button.onClick.RemoveAllListeners();

            button.onClick.AddListener(OnClicked);
        }

        private void OnClicked()
        {
            
            Debug.Log("TAB CLICKED: " + tabType);
            OnTabSelected?.Invoke(tabType);
        }

        public Inventory.Model.ItemType GetTabType()
        {
            return tabType;
        }
    }
}