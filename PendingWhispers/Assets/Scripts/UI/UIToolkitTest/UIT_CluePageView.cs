using System;
using System.Collections.Generic;
using Inventory.Model;
using UnityEngine;
using UnityEngine.UIElements;

namespace Inventory.UI
{
    /// <summary>
    /// Página de inventario/pistas en UI Toolkit. Sustituye a CluePageController + ClueEntryUI
    /// + UIInventoryDescription + MouseFollower, manteniendo la misma API pública para que
    /// UIT_InventoryController solo tenga que cambiar el tipo de su referencia.
    ///
    /// La hoja izquierda (lista de slots) es común a todas las pestañas que la usan;
    /// la hoja derecha cambia según el filtro (VE_Inventory, VE_ObjectClueInformation...).
    /// </summary>
    public class UIT_CluePageView : UIT_JournalPageView
    {
        [Serializable]
        private class DetailPanel
        {
            [Tooltip("Filtro de items para el que se usa este panel.")]
            public ItemType filter;
            [Tooltip("Texto del título de la hoja izquierda con este filtro.")]
            public string sectionTitle;
            [Tooltip("Raíz del panel en la hoja derecha (p. ej. VE_Inventory).")]
            public string panelName;
            public string imageName;
            public string titleName;
            public string descriptionName;
            [Tooltip("Opcional (p. ej. BT_UseObject).")]
            public string useButtonName;

            [NonSerialized] public VisualElement root;
            [NonSerialized] public VisualElement image;
            [NonSerialized] public Label title;
            [NonSerialized] public Label description;
        }

        private const string GridClass = "inventory-grid";
        private const string SlotClass = "inventory-slot";
        private const string SelectedClass = "selected";
        private const string GhostClass = "inventory-drag-ghost";
        private const string ActionButtonClass = "inventory-action-button";
        private const float DragThreshold = 8f;

        [Header("Slots (hoja izquierda)")]
        [SerializeField] private VisualTreeAsset slotTemplate;
        [SerializeField] private string slotContainerName = "SV_SelectorContainer";
        [SerializeField] private string slotIconName = "ItemIcon";
        [SerializeField] private string slotQuantityName = "T_Quantity";
        [SerializeField] private string sectionTitleLabelName = "T_Title_Inventory";

        [Header("Paneles de detalle (hoja derecha)")]
        [SerializeField] private List<DetailPanel> detailPanels = new();

        [Header("Acciones (clic derecho, opcional)")]
        [SerializeField] private string actionPanelName;

        public event Action<int> OnDescriptionRequested;
        public event Action<int> OnItemActionRequested;
        public event Action<int> OnStartDragging;
        public event Action<int, int> OnSwapItems;
        public event Action<ItemType> OnTabChanged;
        /// <summary>Botón "Use" del panel activo, con el índice filtrado seleccionado.</summary>
        public event Action<int> OnUseRequested;

        private sealed class Slot
        {
            public VisualElement Element;
            public VisualElement Icon;
            public Label Quantity;
            public bool IsEmpty = true;
        }

        private readonly List<Slot> slots = new();
        private VisualElement journalRoot;
        private VisualElement slotContainer;
        private Label sectionTitleLabel;
        private DetailPanel activePanel;
        private VisualElement actionPanel;
        private VisualElement dragGhost;

        private int requestedSlotCount;
        private int selectedIndex = -1;

        private int pressedIndex = -1;
        private Vector2 pressPosition;
        private int draggingIndex = -1;
        private int lastDragEndFrame = -1;

        // =========================
        // BIND
        // =========================
        protected override void OnBind(VisualElement root)
        {
            journalRoot = root;

            slotContainer = FindInPage<VisualElement>(slotContainerName);
            if (slotContainer == null)
                return;
            slotContainer.AddToClassList(GridClass);

            sectionTitleLabel = FindInPage<Label>(sectionTitleLabelName);

            foreach (var panel in detailPanels)
                BindDetailPanel(panel);

            if (!string.IsNullOrEmpty(actionPanelName))
            {
                actionPanel = root.Q<VisualElement>(actionPanelName);
                HideActions();
            }

            CreateGhost();

            // UIT_InventoryController puede haber llamado a InitializeInventoryUI antes del Bind.
            BuildSlots();
        }

        private void BindDetailPanel(DetailPanel panel)
        {
            panel.root = journalRoot.Q<VisualElement>(panel.panelName);
            if (panel.root == null)
            {
                Debug.LogError($"{name}: no se encontró el panel '{panel.panelName}'.", this);
                return;
            }

            panel.image = QueryOptional<VisualElement>(panel.root, panel.imageName);
            panel.title = QueryOptional<Label>(panel.root, panel.titleName);
            panel.description = QueryOptional<Label>(panel.root, panel.descriptionName);

            var useButton = QueryOptional<Button>(panel.root, panel.useButtonName);
            if (useButton != null)
                useButton.clicked += HandleUseClicked;

            panel.root.style.display = DisplayStyle.None;
        }

        private T QueryOptional<T>(VisualElement parent, string elementName) where T : VisualElement
        {
            if (string.IsNullOrEmpty(elementName))
                return null;

            var result = parent.Q<T>(elementName);
            if (result == null)
                Debug.LogWarning($"{name}: no se encontró '{elementName}' dentro de '{parent.name}'.", this);
            return result;
        }

        // =========================
        // API USADA POR UIT_InventoryController
        // =========================
        public void InitializeInventoryUI(int size)
        {
            requestedSlotCount = size;
            if (slotContainer != null)
                BuildSlots();
        }

        public void ResetAllItems()
        {
            foreach (var slot in slots)
                SetSlotEmpty(slot);
            ResetSelection();
        }

        public void UpdateData(int index, Sprite sprite, int quantity)
        {
            if (index < 0 || index >= slots.Count)
                return;

            var slot = slots[index];
            if (sprite == null)
            {
                SetSlotEmpty(slot);
                return;
            }

            slot.IsEmpty = false;
            slot.Element.style.display = DisplayStyle.Flex;

            if (slot.Icon != null)
                slot.Icon.style.backgroundImage = new StyleBackground(sprite);
            if (slot.Quantity != null)
                slot.Quantity.text = quantity.ToString();
        }

        public void UpdateDescription(int index, Sprite img, string itemName, string description)
        {
            Select(index);

            if (activePanel == null)
                return;

            if (activePanel.image != null)
                activePanel.image.style.backgroundImage = img != null
                    ? new StyleBackground(img)
                    : new StyleBackground(StyleKeyword.None);
            if (activePanel.title != null)
                activePanel.title.text = itemName;
            if (activePanel.description != null)
                activePanel.description.text = description;
        }

        public void ResetSelection()
        {
            Select(-1);
            ClearPanel(activePanel);
            HideActions();
        }

        public void CreateDraggedItem(Sprite sprite, int quantity)
        {
            if (dragGhost == null)
                return;

            dragGhost.style.backgroundImage = new StyleBackground(sprite);
            dragGhost.style.display = DisplayStyle.Flex;
            dragGhost.BringToFront();
        }

        public void ShowItemAction(int index)
        {
            if (actionPanel == null || index < 0 || index >= slots.Count)
                return;

            Vector2 local = actionPanel.parent.WorldToLocal(slots[index].Element.worldBound.center);
            actionPanel.style.position = Position.Absolute;
            actionPanel.style.left = local.x;
            actionPanel.style.top = local.y;
            actionPanel.style.display = DisplayStyle.Flex;
            actionPanel.BringToFront();
        }

        public void AddAction(string label, Action action)
        {
            if (actionPanel == null)
                return;

            var button = new Button(() =>
            {
                action?.Invoke();
                HideActions();
            }) { text = label };
            button.AddToClassList(ActionButtonClass);
            actionPanel.Add(button);
        }

        // =========================
        // UIT_JournalPageView
        // =========================
        protected override void OnShow(ItemType filter)
        {
            activePanel = null;
            foreach (var panel in detailPanels)
            {
                bool isActive = panel.filter == filter && panel.root != null;
                if (panel.root != null)
                    panel.root.style.display = isActive ? DisplayStyle.Flex : DisplayStyle.None;
                if (isActive)
                    activePanel = panel;
            }

            if (activePanel == null)
                Debug.LogWarning($"{name}: no hay panel de detalle para el filtro '{filter}'.", this);
            else if (sectionTitleLabel != null && !string.IsNullOrEmpty(activePanel.sectionTitle))
                sectionTitleLabel.text = activePanel.sectionTitle;

            OnTabChanged?.Invoke(filter);
        }

        protected override void OnHide()
        {
            EndDrag();
            HideActions();

            foreach (var panel in detailPanels)
                if (panel.root != null)
                    panel.root.style.display = DisplayStyle.None;

            activePanel = null;
        }

        public override void Refresh()
        {
            UIT_InventoryController.Instance?.RefreshUI();
            ResetSelection();
        }

        // =========================
        // SLOTS
        // =========================
        private void BuildSlots()
        {
            if (slotTemplate == null)
            {
                Debug.LogError($"{name}: falta el slotTemplate.", this);
                return;
            }

            slotContainer.Clear();
            slots.Clear();

            for (int i = 0; i < requestedSlotCount; i++)
                CreateSlot(i);
        }

        private void CreateSlot(int index)
        {
            TemplateContainer element = slotTemplate.CloneTree();
            element.AddToClassList(SlotClass);
            element.userData = index;

            var slot = new Slot
            {
                Element = element,
                Icon = element.Q<VisualElement>(slotIconName),
                Quantity = element.Q<Label>(slotQuantityName)
            };

            element.RegisterCallback<ClickEvent>(evt => HandleClick(index, evt));
            element.RegisterCallback<PointerDownEvent>(evt => HandlePointerDown(index, evt));
            element.RegisterCallback<PointerMoveEvent>(evt => HandlePointerMove(index, evt));
            element.RegisterCallback<PointerUpEvent>(evt => HandlePointerUp(index, evt));
            element.RegisterCallback<PointerCaptureOutEvent>(_ => HandleCaptureLost(index));

            slots.Add(slot);
            slotContainer.Add(element);
            SetSlotEmpty(slot);
        }

        private void SetSlotEmpty(Slot slot)
        {
            slot.IsEmpty = true;
            slot.Element.style.display = DisplayStyle.None;
            slot.Element.RemoveFromClassList(SelectedClass);

            if (slot.Icon != null)
                slot.Icon.style.backgroundImage = new StyleBackground(StyleKeyword.None);
            if (slot.Quantity != null)
                slot.Quantity.text = "";
        }

        private void Select(int index)
        {
            if (selectedIndex >= 0 && selectedIndex < slots.Count)
                slots[selectedIndex].Element.RemoveFromClassList(SelectedClass);

            selectedIndex = index;

            if (selectedIndex >= 0 && selectedIndex < slots.Count)
                slots[selectedIndex].Element.AddToClassList(SelectedClass);
        }

        private static void ClearPanel(DetailPanel panel)
        {
            if (panel == null)
                return;

            if (panel.image != null)
                panel.image.style.backgroundImage = new StyleBackground(StyleKeyword.None);
            if (panel.title != null)
                panel.title.text = "";
            if (panel.description != null)
                panel.description.text = "";
        }

        // =========================
        // INPUT
        // =========================
        private void HandleUseClicked()
        {
            if (selectedIndex >= 0)
                OnUseRequested?.Invoke(selectedIndex);
        }

        private void HandleClick(int index, ClickEvent evt)
        {
            if (evt.button != 0)
                return;
            if (Time.frameCount == lastDragEndFrame)
                return; // El clic que llega al soltar un arrastre no cuenta como selección.

            HideActions();
            OnDescriptionRequested?.Invoke(index);
        }

        private void HandlePointerDown(int index, PointerDownEvent evt)
        {
            if (evt.button == 1)
            {
                OnItemActionRequested?.Invoke(index);
                evt.StopPropagation();
                return;
            }

            if (evt.button != 0)
                return;

            pressedIndex = index;
            pressPosition = evt.position;
            slots[index].Element.CapturePointer(evt.pointerId);
        }

        private void HandlePointerMove(int index, PointerMoveEvent evt)
        {
            if (pressedIndex != index)
                return;

            Vector2 position = evt.position;

            if (draggingIndex == -1)
            {
                if ((position - pressPosition).sqrMagnitude < DragThreshold * DragThreshold)
                    return;
                if (slots[index].IsEmpty)
                    return;

                draggingIndex = index;
                OnStartDragging?.Invoke(index); // UIT_InventoryController responde con CreateDraggedItem
            }

            MoveGhost(position);
        }

        private void HandlePointerUp(int index, PointerUpEvent evt)
        {
            if (pressedIndex != index)
                return;

            if (draggingIndex != -1)
            {
                int target = SlotIndexAt(evt.position);
                if (target != -1 && target != draggingIndex)
                    OnSwapItems?.Invoke(draggingIndex, target);

                lastDragEndFrame = Time.frameCount;
                EndDrag();
            }

            pressedIndex = -1;

            var element = slots[index].Element;
            if (element.HasPointerCapture(evt.pointerId))
                element.ReleasePointer(evt.pointerId);
        }

        private void HandleCaptureLost(int index)
        {
            if (pressedIndex != index)
                return;

            pressedIndex = -1;
            EndDrag();
        }

        private int SlotIndexAt(Vector2 panelPosition)
        {
            var picked = journalRoot.panel?.Pick(panelPosition);
            while (picked != null)
            {
                if (picked.userData is int index && picked.ClassListContains(SlotClass))
                    return index;
                picked = picked.parent;
            }
            return -1;
        }

        // =========================
        // DRAG GHOST (antes MouseFollower)
        // =========================
        private void CreateGhost()
        {
            dragGhost = new VisualElement { name = "InventoryDragGhost", pickingMode = PickingMode.Ignore };
            dragGhost.AddToClassList(GhostClass);
            dragGhost.style.position = Position.Absolute;
            dragGhost.style.display = DisplayStyle.None;
            journalRoot.Add(dragGhost);
        }

        private void MoveGhost(Vector2 panelPosition)
        {
            if (dragGhost == null || dragGhost.resolvedStyle.display == DisplayStyle.None)
                return;

            Vector2 local = journalRoot.WorldToLocal(panelPosition);
            dragGhost.style.left = local.x - dragGhost.resolvedStyle.width * 0.5f;
            dragGhost.style.top = local.y - dragGhost.resolvedStyle.height * 0.5f;
        }

        private void EndDrag()
        {
            draggingIndex = -1;
            if (dragGhost != null)
                dragGhost.style.display = DisplayStyle.None;
        }

        private void HideActions()
        {
            if (actionPanel == null)
                return;

            actionPanel.Clear();
            actionPanel.style.display = DisplayStyle.None;
        }
    }
}
