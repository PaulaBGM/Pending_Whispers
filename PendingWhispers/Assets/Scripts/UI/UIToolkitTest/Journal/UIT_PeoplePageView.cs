using System.Collections.Generic;
using Inventory.Model;
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

    [Header("Presentar testimonios")]
    [Tooltip("Botón de la hoja derecha. Solo aparece cuando el diario se abre para presentar algo.")]
    [SerializeField] private string presentButtonName = "BT_PresentTestimony";
    [Tooltip("Fondo de la frase seleccionada. Se puede sustituir por la clase USS 'testimony-line--selected'.")]
    [SerializeField] private Color selectedLineColor = new Color(0.55f, 0.12f, 0.24f, 0.25f);

    private const string LineClass = "testimony-line";
    private const string LineSelectedClass = "testimony-line--selected";

    private Button presentButton;
    private readonly List<Label> lines = new();
    private PersonJournalEntry shownEntry;
    private int selectedLine = -1;

    private readonly List<VisualElement> slots = new();
    private readonly List<PersonJournalEntry> shownEntries = new();

    private VisualElement listContainer;
    private VisualElement portrait;
    private Label nameLabel;
    private ScrollView dialogueScroll;
    private Label dialogueLabel;

    private int selectedIndex = -1;

    [Header("Respaldo si falta la hoja izquierda en el UXML")]
    [Tooltip("Si el UXML no tiene la hoja izquierda de testimonios, se crea copiando esta (la de pistas).")]
    [SerializeField] private string fallbackSourcePageName = "VE_ClueDialogueInventory";
    [SerializeField] private string testimonyPageName = "VE_TestimonyPage";
    [SerializeField] private string testimonyTitleName = "T_Title_Testimony";
    [SerializeField] private string testimonyTitleText = "Testimonies";

    // =========================
    // BIND
    // =========================

    /// <summary>
    /// Si alguien borra VE_TestimonyPage del UXML (ya ha pasado), la página se reconstruye
    /// copiando la hoja de pistas del propio UXML. Así People sigue funcionando aunque se toque el diseño.
    /// </summary>
    protected override void PrepareElements(VisualElement journalRoot)
    {
        if (journalRoot.Q<VisualElement>(testimonyPageName) != null)
            return;

        VisualElement source = journalRoot.Q<VisualElement>(fallbackSourcePageName);
        VisualTreeAsset template = FindJournalTemplate(journalRoot);
        if (source == null || template == null)
        {
            Debug.LogError($"{name}: falta '{testimonyPageName}' en el UXML y no se pudo reconstruir.", this);
            return;
        }

        VisualElement copy = template.CloneTree().Q<VisualElement>(fallbackSourcePageName);
        if (copy == null)
            return;

        copy.name = testimonyPageName;
        copy.style.display = DisplayStyle.None;

        var title = copy.Q<Label>("T_Title_Inventory");
        if (title != null)
        {
            title.name = testimonyTitleName;
            title.text = testimonyTitleText;
        }

        var list = copy.Q<ScrollView>() ?? copy.Q<VisualElement>("SV_SelectorContainer");
        if (list != null)
            list.name = listContainerName;

        VisualElement parent = source.parent;
        parent.Insert(parent.IndexOf(source) + 1, copy);

        Debug.LogWarning($"{name}: el UXML no tenía '{testimonyPageName}'. Se ha creado por código copiando " +
                         $"'{fallbackSourcePageName}'. Conviene volver a añadirlo en UI Builder.", this);
    }

    private static VisualTreeAsset FindJournalTemplate(VisualElement journalRoot)
    {
        foreach (var document in FindObjectsByType<UIDocument>(FindObjectsSortMode.None))
        {
            if (document.rootVisualElement != null &&
                document.rootVisualElement.Contains(journalRoot) &&
                document.visualTreeAsset != null)
                return document.visualTreeAsset;
        }
        return null;
    }

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

        presentButton = FindInPage<Button>(presentButtonName);
        if (presentButton != null)
            presentButton.clicked += HandlePresentClicked;
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

        BuildLines(entry);

        if (dialogueScroll != null)
            dialogueScroll.scrollOffset = Vector2.zero;
    }

    // =========================
    // FRASES (una por testimonio, seleccionables)
    // =========================
    private void BuildLines(PersonJournalEntry entry)
    {
        shownEntry = entry;

        // Sin ScrollView no se pueden poner frases sueltas: se usa el texto de siempre.
        if (dialogueScroll == null)
        {
            if (dialogueLabel != null)
                dialogueLabel.text = FormatDialogues(entry);
            UpdatePresentButton();
            return;
        }

        // El Label único del UXML sirve de plantilla de estilo; no se muestra.
        if (dialogueLabel != null)
            dialogueLabel.style.display = DisplayStyle.None;

        int count = entry?.dialogues?.Count ?? 0;

        while (lines.Count < count)
            lines.Add(CreateLine(lines.Count));

        for (int i = 0; i < lines.Count; i++)
        {
            bool used = i < count;
            lines[i].style.display = used ? DisplayStyle.Flex : DisplayStyle.None;
            if (used)
                lines[i].text = "• " + entry.dialogues[i];
        }

        SelectLine(-1);
    }

    private Label CreateLine(int index)
    {
        var line = new Label();
        line.AddToClassList(LineClass);

        // Mismo aspecto que T_PersonDialogue.
        if (dialogueLabel != null)
        {
            line.style.unityFontDefinition = dialogueLabel.style.unityFontDefinition;
            line.style.fontSize = dialogueLabel.style.fontSize;
            line.style.color = dialogueLabel.style.color;
            line.style.unityTextAlign = dialogueLabel.style.unityTextAlign;
            line.style.wordSpacing = dialogueLabel.style.wordSpacing;
        }
        line.style.whiteSpace = WhiteSpace.Normal;
        line.style.width = Length.Percent(100);
        line.style.flexShrink = 0;
        line.style.marginBottom = 10;
        line.style.paddingTop = 4;
        line.style.paddingBottom = 4;
        line.style.paddingLeft = 4;
        line.style.paddingRight = 4;

        line.RegisterCallback<ClickEvent>(evt =>
        {
            if (evt.button == 0)
                SelectLine(index);
        });

        dialogueScroll.Add(line);
        return line;
    }

    private void SelectLine(int index)
    {
        selectedLine = index;
        if (index >= 0)
            Debug.Log($"[Present] Frase {index} seleccionada. Testimonio: {(GetSelectedTestimony() != null ? GetSelectedTestimony().name : "NINGUNO (null)")}. " +
                      $"Testimonios de esta persona: {shownEntry?.testimonies?.Count ?? 0}, frases: {shownEntry?.dialogues?.Count ?? 0}");

        for (int i = 0; i < lines.Count; i++)
        {
            bool selected = i == index;
            lines[i].EnableInClassList(LineSelectedClass, selected);
            lines[i].style.backgroundColor = selected ? selectedLineColor : new StyleColor(Color.clear);
        }

        UpdatePresentButton();
    }

    private TestimonyItemSO GetSelectedTestimony()
    {
        var testimonies = shownEntry?.testimonies;
        if (testimonies == null || selectedLine < 0 || selectedLine >= testimonies.Count)
            return null;
        return testimonies[selectedLine];
    }

    private void UpdatePresentButton()
    {
        if (presentButton == null)
        {
            Debug.LogWarning($"[Present] No se encontró '{presentButtonName}' en la página de testimonios.");
            return;
        }

        var journal = UIT_JournalController.Instance;
        var request = journal != null ? journal.ActiveRequest : null;

        Debug.Log($"[Present] Página testimonios: petición activa = {request != null}, botón encontrado = {presentButton != null}");
        presentButton.style.display = request != null ? DisplayStyle.Flex : DisplayStyle.None;
        if (request == null)
            return;

        if (!string.IsNullOrEmpty(request.ActionLabel))
            presentButton.text = request.ActionLabel;
        presentButton.SetEnabled(GetSelectedTestimony() != null);
    }

    private void HandlePresentClicked()
    {
        var testimony = GetSelectedTestimony();
        Debug.Log($"[Present] Botón pulsado. Testimonio: {(testimony != null ? testimony.name : "null")}");
        if (testimony == null)
            return;

        var journal = UIT_JournalController.Instance;
        if (journal != null)
            journal.SubmitSelection(testimony);
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