using System.Collections.Generic;
using UnityEngine;
using Inventory.Model;
using Inventory.UI;

namespace Inventory
{
    /// <summary>
    /// Conecta el InventorySO con la pagina de pistas del diario (UIT_CluePageView).
    ///
    /// Persiste entre escenas, pero la vista NO: cada escena trae su propio diario
    /// (prefab -----UIToolkit-----). Por eso la vista se registra al despertar
    /// (UIT_CluePageView llama a EnsureFor) y el controlador se re-engancha a ella.
    /// Antes se quedaba apuntando a la vista de la escena de la casa: al cambiar de
    /// escena o cargar partida en otra, la pagina de pistas salia vacia aunque el
    /// inventario (y el guardado) tuviera las pistas.
    /// </summary>
    public class UIT_InventoryController : BaseSingleton<UIT_InventoryController>
    {
        [Tooltip("Opcional: la vista de pistas de esta escena. Si se deja vacio, la propia vista se registra.")]
        [SerializeField] private UIT_CluePageView inventoryUI;
        protected override bool PersistAcrossScenes => true;

        private InventorySO inventoryData;
        private UIT_CluePageView boundView;

        private ItemType currentTab = ItemType.Clue;
        private readonly List<int> filteredIndices = new();

        private Dictionary<int, InventoryItem> lastState;
        private bool hasPendingUpdate;

        // =========================
        // LIFECYCLE
        // =========================
        protected override void Awake()
        {
            base.Awake();

            if (Instance != this)
            {
                // Copia de la escena (se destruye): le pasa su vista al controlador que persiste.
                if (inventoryUI != null && Instance != null)
                    Instance.RegisterView(inventoryUI);
                return;
            }

            EnsureInventoryData();

            if (inventoryUI != null)
                RegisterView(inventoryUI);
        }

        protected override void OnDestroy()
        {
            if (Instance == this)
            {
                UnbindView();

                if (inventoryData != null)
                    inventoryData.OnInventoryUpdated -= QueueRefresh;
            }

            base.OnDestroy();
        }

        /// <summary>
        /// La llama cada UIT_CluePageView: garantiza que hay controlador (en escenas que no
        /// lo tienen, como el HUB, se crea uno) y que esta enganchado a esa vista.
        /// </summary>
        public static UIT_InventoryController EnsureFor(UIT_CluePageView view)
        {
            if (Instance == null)
                new GameObject("InventoryController (auto)").AddComponent<UIT_InventoryController>();

            if (Instance != null && view != null)
                Instance.RegisterView(view);

            return Instance;
        }

        // =========================
        // INVENTORY
        // =========================
        private bool EnsureInventoryData()
        {
            if (inventoryData != null)
                return true;

            if (InventoryRuntime.Instance == null)
                return false;

            inventoryData = InventoryRuntime.Instance.GetInventory();
            if (inventoryData == null)
                return false;

            inventoryData.OnInventoryUpdated -= QueueRefresh;
            inventoryData.OnInventoryUpdated += QueueRefresh;
            return true;
        }

        // =========================
        // VIEW
        // =========================
        public void RegisterView(UIT_CluePageView view)
        {
            if (view == null || view == boundView)
                return;

            UnbindView();

            boundView = view;
            inventoryUI = view;

            if (EnsureInventoryData())
                view.InitializeInventoryUI(inventoryData.Size);

            view.OnTabChanged += HandleTabChanged;
            view.OnDescriptionRequested += HandleDescriptionRequest;
            view.OnSwapItems += HandleSwapItems;
            view.OnStartDragging += HandleDragging;
            view.OnItemActionRequested += HandleItemActionRequest;

            // Lo que ya hay en el inventario (escena anterior o partida cargada) se pinta al abrir.
            RefreshUI();
        }

        public void UnregisterView(UIT_CluePageView view)
        {
            if (view != null && view == boundView)
                UnbindView();
        }

        private void UnbindView()
        {
            // boundView puede estar ya destruida (cambio de escena): los eventos son de C#,
            // se pueden quitar igual mientras la referencia exista.
            if (!ReferenceEquals(boundView, null))
            {
                boundView.OnTabChanged -= HandleTabChanged;
                boundView.OnDescriptionRequested -= HandleDescriptionRequest;
                boundView.OnSwapItems -= HandleSwapItems;
                boundView.OnStartDragging -= HandleDragging;
                boundView.OnItemActionRequested -= HandleItemActionRequest;
            }

            boundView = null;
            inventoryUI = null;
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
            if (!EnsureInventoryData()) return;

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
            if (inventoryUI == null) return; // tambien si la vista se destruyo con su escena
            if (!inventoryUI.IsVisible) return;

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
            if (inventoryData == null || index < 0 || index >= filteredIndices.Count)
                return InventoryItem.GetEmptyItem();

            return inventoryData.GetItemAt(filteredIndices[index]);
        }

        private void HandleItemActionRequest(int index)
        {
            var item = GetItemByFilteredIndex(index);
            if (item.IsEmpty || inventoryUI == null) return;

            inventoryUI.ShowItemAction(index);
        }

        private void HandleDragging(int index)
        {
            var item = GetItemByFilteredIndex(index);
            if (item.IsEmpty || inventoryUI == null) return;

            inventoryUI.CreateDraggedItem(item.item.ItemImage, item.quantity);
        }

        private void HandleSwapItems(int a, int b)
        {
            if (inventoryData == null) return;
            if (a < 0 || b < 0) return;
            if (a >= filteredIndices.Count || b >= filteredIndices.Count) return;

            inventoryData.SwapItems(filteredIndices[a], filteredIndices[b]);
        }

        private void HandleDescriptionRequest(int index)
        {
            if (inventoryUI == null) return;

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
