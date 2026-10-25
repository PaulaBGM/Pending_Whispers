
using UnityEngine;
using UnityEngine.UIElements;

public class InventorySlotTest : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private UIDocument uiDocument;
    [SerializeField] private VisualTreeAsset slotTemplate;

    [Header("Iconos de prueba")]
    [SerializeField] private Sprite[] testIcons;

    private ScrollView inventoryScroll;
    private VisualElement selectedSlot;

    private void Start()
    {
        Debug.Log("InventorySlotTest: iniciado.");

        if (uiDocument == null)
        {
            Debug.LogError("Asigna el UIDocument en el Inspector.");
            return;
        }

        if (slotTemplate == null)
        {
            Debug.LogError("Asigna InventorySlot_Template en el Inspector.");
            return;
        }

        // Buscar tu ScrollView por su nombre real.
        inventoryScroll = uiDocument.rootVisualElement.Q<ScrollView>("SV_SelectorContainer");

        if (inventoryScroll == null)
        {
            Debug.LogError("No se encontró SV_SelectorContainer. " + "Comprueba el nombre en UI Builder.");
            return;
        }

        // Limpiar slots anteriores de prueba.
        inventoryScroll.Clear();

        // Si no hay iconos asignados, crear tres slots vacíos.
        int slotCount = testIcons != null && testIcons.Length > 0 ? testIcons.Length : 3;

        for (int i = 0; i < slotCount; i++)
        {
            Sprite icon = testIcons != null && i < testIcons.Length ? testIcons[i] : null;

            CreateSlot(icon, i);
        }

        Debug.Log($"Slots creados: {inventoryScroll.childCount}");
    }

    private void CreateSlot(Sprite icon, int index)
    {
        // Instanciar la plantilla de UI Builder.
        TemplateContainer slot = slotTemplate.CloneTree();
        slot.style.width = 64;
        slot.style.height = 64;
        slot.style.minWidth = 64;
        slot.style.minHeight = 64;
        slot.style.flexShrink = 0;

        slot.style.backgroundColor = Color.magenta;

        slot.AddToClassList("inventory-slot");

        // Buscar el elemento que muestra el icono.
        VisualElement iconElement =
            slot.Q<VisualElement>("ItemIcon");

        if (iconElement != null && icon != null)
        {
            iconElement.style.backgroundImage =
                new StyleBackground(icon);
        }

        // Detectar el clic y seleccionar el slot.
        slot.RegisterCallback<ClickEvent>(evt => { SelectSlot(slot, index); });

        // Añadirlo al ScrollView.
        inventoryScroll.Add(slot);

        Debug.Log($"Slot {index + 1} añadido.");
    }

    private void SelectSlot(VisualElement slot, int index)
    {
        if (selectedSlot != null)
        {
            selectedSlot.RemoveFromClassList("selected");
        }

        selectedSlot = slot;
        selectedSlot.AddToClassList("selected");

        Debug.Log($"Slot {index + 1} seleccionado.");
    }

    public void ClearInventory()
    {
        inventoryScroll?.Clear();
        selectedSlot = null;
    }
}
