using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Pestaña de testimonios (Tab_DialogueClue). Lee de PeopleJournalSystem, no del InventorySO.
/// Hoja izquierda: rejilla de retratos. Hoja derecha: retrato, nombre y frases registradas.
/// Los slots se reutilizan entre refrescos (pool), solo se crean los que faltan.
/// </summary>
public class UIT_PeoplePageView : UIT_JournalPageView
{
    private const string GridClass = "inventory-grid";
    private const string SlotClass = "inventory-slot";
    private const string SelectedClass = "selected";

    [Header("Lista (hoja izquierda)")]
    [SerializeField] private VisualTreeAsset slotTemplate;
    [SerializeField] private string listContainerName = "SV_PeopleList";
    [Tooltip("Elemento del slotTemplate que muestra la miniatura (el mismo que 'Slot Icon Name' de Page_Items).")]
    [SerializeField] private string slotIconName = "VE_Item_Image";

    [Header("Detalle (hoja derecha)")]
    [Tooltip("Elemento donde va el retrato grande. Si duplicaste VE_ObjectClueInformation, " +
             "es el equivalente al 'Image Name' del panel de pistas de Page_Items.")]
    [SerializeField] private string portraitName = "VE_PersonImage";
    [SerializeField] private string nameLabelName = "T_PersonName";
    [SerializeField] private string dialogueScrollName = "SV_PersonDialogue";
    [SerializeField] private string dialogueLabelName = "T_PersonDialogue";

    private readonly List<VisualElement> slots = new();
    private readonly List<PersonJournalEntry> shownEntries = new();

    private VisualElement listContainer;
    private VisualElement portrait;
    private Label nameLabel;
    private ScrollView dialogueScroll;
    private Label dialogueLabel;

    private int selectedIndex = -1;

    // =========================
    // BIND
    // =========================
    protected override void OnBind(VisualElement journalRoot)
    {
        listContainer = FindInPage<VisualElement>(listContainerName);
        if (listContainer != null)
        {
            listContainer.AddToClassList(GridClass);
            // Al duplicar la página en UI Builder se copian los huecos de ejemplo (p. ej. el cuchillo):
            // se quitan para que solo queden los que crea el código.
            (listContainer is ScrollView scroll ? scroll.contentContainer : listContainer).Clear();
        }

        portrait = FindInPage<VisualElement>(portraitName);
        if (portrait == null)
            Debug.LogWarning($"{name}: sin '{portraitName}' el retrato grande no se verá. " +
                             "Pon en 'Portrait Name' el nombre real del elemento de imagen de VE_TestimonyInformation.", this);
        nameLabel = FindInPage<Label>(nameLabelName);
        dialogueScroll = FindInPage<ScrollView>(dialogueScrollName);
        dialogueLabel = FindInPage<Label>(dialogueLabelName);
    }

    // =========================
    // REFRESH
    // =========================
    public override void Refresh()
    {
        if (listContainer == null)
            return;

        shownEntries.Clear();
        var system = PeopleJournalSystem.Instance;
        if (system != null)
            shownEntries.AddRange(system.GetEntries());

        EnsureSlotCount(shownEntries.Count);

        for (int i = 0; i < slots.Count; i++)
        {
            bool used = i < shownEntries.Count;
            slots[i].style.display = used ? DisplayStyle.Flex : DisplayStyle.None;
            slots[i].RemoveFromClassList(SelectedClass);

            if (used)
                SetIcon(slots[i], shownEntries[i].portrait);
        }

        selectedIndex = -1;
        Select(shownEntries.Count > 0 ? 0 : -1);
    }

    private void EnsureSlotCount(int count)
    {
        if (slotTemplate == null)
        {
            Debug.LogError($"{name}: falta el slotTemplate.", this);
            return;
        }

        while (slots.Count < count)
        {
            int index = slots.Count;
            TemplateContainer slot = slotTemplate.CloneTree();
            slot.AddToClassList(SlotClass);

            // Igual que UIT_CluePageView: aviso una vez si el nombre no coincide con el slotTemplate.
            if (index == 0 && slot.Q<VisualElement>(slotIconName) == null)
                Debug.LogWarning($"{name}: el slotTemplate no tiene ningún elemento '{slotIconName}'. " +
                                 "Revisa 'Slot Icon Name' (debe ser el mismo que en Page_Items).", this);
            slot.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.button == 0)
                    Select(index);
            });

            slots.Add(slot);
            listContainer.Add(slot);
        }
    }

    private void SetIcon(VisualElement slot, Sprite sprite)
    {
        // Antes, si no encontraba el icono, pintaba en el propio hueco y la imagen de ejemplo
        // de la plantilla (el cuchillo) quedaba encima. Ahora se usa el mismo elemento que Page_Items.
        var icon = slot.Q<VisualElement>(slotIconName);
        if (icon == null)
            return;

        icon.style.backgroundImage = sprite != null
            ? new StyleBackground(sprite)
            : new StyleBackground(StyleKeyword.None);
        FitBackground(icon);

        if (sprite == null)
            Debug.LogWarning($"{name}: un testimonio no tiene retrato. Asigna Portrait (o Neutral) " +
                             "a ese personaje en su DialogueData.", this);
    }

    // =========================
    // SELECTION / DETAIL
    // =========================
    private void Select(int index)
    {
        if (selectedIndex >= 0 && selectedIndex < slots.Count)
            slots[selectedIndex].RemoveFromClassList(SelectedClass);

        if (index < 0 || index >= shownEntries.Count)
        {
            selectedIndex = -1;
            ShowDetail(null);
            return;
        }

        selectedIndex = index;
        slots[index].AddToClassList(SelectedClass);
        ShowDetail(shownEntries[index]);
    }

    private void ShowDetail(PersonJournalEntry entry)
    {
        if (portrait != null)
        {
            portrait.style.backgroundImage = entry?.portrait != null
                ? new StyleBackground(entry.portrait)
                : new StyleBackground(StyleKeyword.None);
            FitBackground(portrait);
        }

        if (nameLabel != null)
            nameLabel.text = entry?.personName ?? "";

        if (dialogueLabel != null)
            dialogueLabel.text = FormatDialogues(entry);

        if (dialogueScroll != null)
            dialogueScroll.scrollOffset = Vector2.zero;
    }

    /// <summary>Equivale al antiguo scale-to-fit: la imagen entera, centrada y sin deformar.</summary>
    private static void FitBackground(VisualElement element)
    {
        element.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
        element.style.backgroundRepeat = new BackgroundRepeat(Repeat.NoRepeat, Repeat.NoRepeat);
        element.style.backgroundPositionX = new BackgroundPosition(BackgroundPositionKeyword.Center);
        element.style.backgroundPositionY = new BackgroundPosition(BackgroundPositionKeyword.Center);
    }

    private static string FormatDialogues(PersonJournalEntry entry)
    {
        if (entry == null)
            return "";

        if (entry.dialogues == null || entry.dialogues.Count == 0)
            return entry.fullDialogue ?? "";

        return "• " + string.Join("\n\n• ", entry.dialogues);
    }
}