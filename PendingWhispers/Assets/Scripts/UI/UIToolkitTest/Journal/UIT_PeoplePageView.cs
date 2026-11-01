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
    [SerializeField] private string slotIconName = "ItemIcon";

    [Header("Detalle (hoja derecha)")]
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
        listContainer?.AddToClassList(GridClass);

        portrait = FindInPage<VisualElement>(portraitName);
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
        var icon = slot.Q<VisualElement>(slotIconName);
        if (icon != null)
            icon.style.backgroundImage = sprite != null
                ? new StyleBackground(sprite)
                : new StyleBackground(StyleKeyword.None);
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
            portrait.style.backgroundImage = entry?.portrait != null
                ? new StyleBackground(entry.portrait)
                : new StyleBackground(StyleKeyword.None);

        if (nameLabel != null)
            nameLabel.text = entry?.personName ?? "";

        if (dialogueLabel != null)
            dialogueLabel.text = FormatDialogues(entry);

        if (dialogueScroll != null)
            dialogueScroll.scrollOffset = Vector2.zero;
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
