using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Pestaña de casos (Tab_CaseState).
/// Hoja izquierda: lista de casos abiertos → al pulsar uno, su hipótesis.
/// Hoja derecha: ficha del caso (icono, nombre, descripción, pistas encontradas).
/// Lista ↔ hipótesis es estado interno de esta página, no otra pestaña.
/// </summary>
public class UIT_CasePageView : UIT_JournalPageView
{
    private const string EntryClass = "case-entry";
    private const string EntryTitleClass = "case-entry__title";
    private const string EntrySubtitleClass = "case-entry__subtitle";
    private const string SelectedClass = "selected";

    [Header("Hipótesis")]
    [SerializeField] private UIT_HypothesisController hypothesisController;
    [SerializeField] private UIT_HypothesisPanelView hypothesisPanel;

    [Header("Hoja izquierda")]
    [SerializeField] private string listPageName = "VE_CaseListPage";
    [SerializeField] private string listContainerName = "SV_CaseList";
    [SerializeField] private string hypothesisPageName = "VE_HyphotesisPage";
    [SerializeField] private string backButtonName = "BT_BackToCases";

    [Header("Hoja derecha")]
    [SerializeField] private string caseImageName = "VE_CharacterImage";
    [SerializeField] private string caseNameName = "T_CaseName";
    [SerializeField] private string descriptionScrollName = "SV_CaseDescription";
    [SerializeField] private string descriptionName = "T_LittleCaseDescription";
    [SerializeField] private string progressName = "T_NumClues";

    [Header("Textos")]
    [SerializeField] private string resolvedText = "Case closed";
    [SerializeField] private string notActiveMessage = "You can only form a hypothesis for the active case.";

    private readonly List<VisualElement> entries = new();
    private readonly List<CaseRuntime> shownCases = new();

    private VisualElement listPage;
    private VisualElement listContainer;
    private VisualElement hypothesisPage;
    private VisualElement caseImage;
    private Label caseName;
    private ScrollView descriptionScroll;
    private Label description;
    private Label progress;

    private int previewIndex = -1;

    // =========================
    // BIND
    // =========================
    protected override void OnBind(VisualElement journalRoot)
    {
        listPage = FindInPage<VisualElement>(listPageName);
        listContainer = FindInPage<VisualElement>(listContainerName);
        hypothesisPage = FindInPage<VisualElement>(hypothesisPageName);

        caseImage = FindInPage<VisualElement>(caseImageName);
        caseName = FindInPage<Label>(caseNameName);
        descriptionScroll = FindInPage<ScrollView>(descriptionScrollName);
        description = FindInPage<Label>(descriptionName);
        progress = FindInPage<Label>(progressName);

        if (hypothesisPanel != null)
            hypothesisPanel.Bind(hypothesisPage);

        if (hypothesisPage != null)
        {
            var backButton = hypothesisPage.Q<Button>(backButtonName);
            if (backButton != null)
                backButton.clicked += ShowList;
        }
    }

    // =========================
    // REFRESH
    // =========================
    public override void Refresh()
    {
        shownCases.Clear();
        var journal = CaseJournalSystem.Instance;
        if (journal != null)
            shownCases.AddRange(journal.GetAllCases());

        if (listContainer != null)
        {
            EnsureEntryCount(shownCases.Count);

            for (int i = 0; i < entries.Count; i++)
            {
                bool used = i < shownCases.Count;
                entries[i].style.display = used ? DisplayStyle.Flex : DisplayStyle.None;
                entries[i].RemoveFromClassList(SelectedClass);

                if (used)
                    FillEntry(entries[i], shownCases[i]);
            }
        }

        ShowList();
        previewIndex = -1;
        Preview(IndexOfActiveCase());
    }

    private void EnsureEntryCount(int count)
    {
        while (entries.Count < count)
        {
            int index = entries.Count;

            var entry = new VisualElement();
            entry.AddToClassList(EntryClass);

            var title = new Label { name = "Title" };
            title.AddToClassList(EntryTitleClass);
            var subtitle = new Label { name = "Subtitle" };
            subtitle.AddToClassList(EntrySubtitleClass);

            entry.Add(title);
            entry.Add(subtitle);

            entry.RegisterCallback<PointerEnterEvent>(_ => Preview(index));
            entry.RegisterCallback<ClickEvent>(evt =>
            {
                if (evt.button == 0)
                    OpenCase(index);
            });

            entries.Add(entry);
            listContainer.Add(entry);
        }
    }

    private void FillEntry(VisualElement entry, CaseRuntime runtime)
    {
        entry.Q<Label>("Title").text = runtime.Title;
        entry.Q<Label>("Subtitle").text = runtime.isResolved ? resolvedText : GetObjectiveText(runtime);
    }

    // =========================
    // LIST ↔ HYPOTHESIS
    // =========================
    private void ShowList()
    {
        SetDisplay(listPage, true);
        SetDisplay(hypothesisPage, false);
    }

    private void ShowHypothesis()
    {
        SetDisplay(listPage, false);
        SetDisplay(hypothesisPage, true);
    }

    private void OpenCase(int index)
    {
        if (index < 0 || index >= shownCases.Count)
            return;

        Preview(index);

        // CaseManager solo admite un caso activo, y los flags de la hipótesis son globales:
        // de momento solo se puede formular la hipótesis del caso activo.
        var data = shownCases[index].data;
        if (data != CaseManager.Instance?.GetCurrentCaseData())
        {
            UIFeedbackManager.Instance?.ShowMessage(notActiveMessage);
            return;
        }

        if (hypothesisController != null && hypothesisController.TryOpenHypothesis(data))
            ShowHypothesis();
    }

    // =========================
    // DETAIL
    // =========================
    private void Preview(int index)
    {
        if (previewIndex >= 0 && previewIndex < entries.Count)
            entries[previewIndex].RemoveFromClassList(SelectedClass);

        if (index < 0 || index >= shownCases.Count)
        {
            previewIndex = -1;
            ShowDetail(null);
            return;
        }

        previewIndex = index;
        if (index < entries.Count)
            entries[index].AddToClassList(SelectedClass);

        ShowDetail(shownCases[index]);
    }

    private void ShowDetail(CaseRuntime runtime)
    {
        if (caseImage != null)
            caseImage.style.backgroundImage = runtime?.Icon != null
                ? new StyleBackground(runtime.Icon)
                : new StyleBackground(StyleKeyword.None);

        if (caseName != null)
            caseName.text = runtime?.Title ?? "";

        if (description != null)
            description.text = runtime?.Description ?? "";

        if (descriptionScroll != null)
            descriptionScroll.scrollOffset = Vector2.zero;

        if (progress != null)
            progress.text = GetProgressText(runtime);
    }

    // =========================
    // HELPERS
    // =========================
    private int IndexOfActiveCase()
    {
        var active = CaseManager.Instance?.GetCurrentCaseData();
        for (int i = 0; i < shownCases.Count; i++)
            if (shownCases[i].data == active)
                return i;

        return shownCases.Count > 0 ? 0 : -1;
    }

    // CaseRuntime no comprueba listas nulas: se protege aquí.
    private static string GetProgressText(CaseRuntime runtime)
    {
        if (runtime?.data?.requiredClues == null)
            return "";
        return runtime.GetProgressText();
    }

    private static string GetObjectiveText(CaseRuntime runtime)
    {
        if (runtime?.data?.objectives == null || runtime.data.objectives.Count == 0)
            return "";
        return runtime.GetCurrentObjective();
    }

    private static void SetDisplay(VisualElement element, bool visible)
    {
        if (element != null)
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
