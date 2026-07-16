using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CasePageController : JournalPageController
{
    [Header("Panels")]
    [SerializeField] private GameObject listPanel;
    [SerializeField] private GameObject summaryPanel;
    [SerializeField] private GameObject hypothesisPanel;

    [Header("UI References")]
    [SerializeField] private HypothesisController hypothesis;
    [SerializeField] private CaseEntryUI caseEntryPrefab;
    [SerializeField] private Transform content;

    [Header("Summary UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private TMP_Text reputationText;
    [SerializeField] private Image caseImage;

    [Header("Objectives")]
    [SerializeField] private TMP_Text objectivePrefab;
    [SerializeField] private Transform objectivesContainer;

    private readonly List<CaseEntryUI> spawnedEntries = new();
    private readonly List<TMP_Text> objectiveEntries = new();

    private CaseRuntime selectedCase;

    protected override void OnEnable()
    {
        base.OnEnable();

        if (CaseJournalSystem.Instance != null)
            CaseJournalSystem.Instance.OnCasesChanged += Refresh;

        if (ReputationManager.Instance != null)
            ReputationManager.Instance.OnReputationChanged += HandleReputationChanged;
    }

    protected override void OnDisable()
    {
        if (CaseJournalSystem.Instance != null)
            CaseJournalSystem.Instance.OnCasesChanged -= Refresh;

        if (ReputationManager.Instance != null)
            ReputationManager.Instance.OnReputationChanged -= HandleReputationChanged;

        if (selectedCase != null)
            selectedCase.OnCaseUpdated -= RefreshSelectedCase;

        base.OnDisable();
    }

    public override void Refresh()
    {
        if (CaseJournalSystem.Instance == null)
            return;

        var cases = CaseJournalSystem.Instance.GetAllCases();

        EnsureSlots(cases.Count);

        for (int i = 0; i < spawnedEntries.Count; i++)
        {
            spawnedEntries[i].ResetData();
            spawnedEntries[i].Deselect();
        }

        for (int i = 0; i < cases.Count; i++)
        {
            spawnedEntries[i].SetData(cases[i]);

            if (cases[i] == selectedCase)
                spawnedEntries[i].Select();
        }

        if (selectedCase == null && cases.Count > 0)
        {
            HandleCaseClicked(cases[0]);
        }
        else if (selectedCase != null)
        {
            RefreshSelectedCase();
        }
    }

    private void EnsureSlots(int count)
    {
        while (spawnedEntries.Count < count)
        {
            var obj = Instantiate(caseEntryPrefab, content);
            var ui = obj.GetComponent<CaseEntryUI>();

            ui.OnEntryClicked += HandleCaseClicked;
            spawnedEntries.Add(ui);
        }
    }

    private void HandleCaseClicked(CaseRuntime runtime)
    {
        if (selectedCase != null)
            selectedCase.OnCaseUpdated -= RefreshSelectedCase;

        selectedCase = runtime;
        selectedCase.OnCaseUpdated += RefreshSelectedCase;

        foreach (var entry in spawnedEntries)
        {
            if (entry.GetData() == runtime)
                entry.Select();
            else
                entry.Deselect();
        }

        ShowSummary(runtime);
    }

    private void RefreshSelectedCase()
    {
        if (selectedCase == null)
            return;

        UpdateSummary(selectedCase);

        foreach (var entry in spawnedEntries)
        {
            if (entry.GetData() == selectedCase)
            {
                entry.SetData(selectedCase);
                break;
            }
        }
    }

    private void ShowSummary(CaseRuntime runtime)
    {
        summaryPanel.SetActive(true);
        hypothesisPanel.SetActive(false);
        UpdateSummary(runtime);
    }

    private void UpdateSummary(CaseRuntime runtime)
    {
        var data = runtime.data;

        titleText.text = data.caseTitle;
        descriptionText.text = data.caseDescription;

        if (reputationText != null)
        {
            if (ReputationManager.Instance != null)
                reputationText.text = $"{ReputationManager.Instance.Reputation}%";
            else
                reputationText.text = "0%";
        }

        UpdateObjectives(runtime);

        if (caseImage != null)
            caseImage.sprite = data.caseIcon;
    }

    private void UpdateObjectives(CaseRuntime runtime)
    {
        foreach (var entry in objectiveEntries)
        {
            if (entry != null)
                Destroy(entry.gameObject);
        }

        objectiveEntries.Clear();

        if (runtime.data.objectives == null)
            return;

        foreach (var objective in runtime.data.objectives)
        {
            TMP_Text text = Instantiate(objectivePrefab, objectivesContainer);

            bool completed = runtime.IsObjectiveCompleted(objective);

            text.text = completed
                ? "[COMPLETE] " + objective.objectiveText
                : "[PENDING] " + objective.objectiveText;

            objectiveEntries.Add(text);
        }
    }

    private void HandleReputationChanged(int value)
    {
        if (reputationText != null)
            reputationText.text = $"{value}%";
    }

    public void OnCreateHypothesis()
    {
        hypothesisPanel.SetActive(true);
        hypothesis.OpenHypothesis();
        summaryPanel.SetActive(false);
    }

    public void CloseHypothesis()
    {
        hypothesis.CloseHypothesis();
        summaryPanel.SetActive(true);
        hypothesisPanel.SetActive(false);
    }

    public void BackToList()
    {
        summaryPanel.SetActive(true);
        hypothesisPanel.SetActive(false);
    }

    public override void Show()
    {
        base.Show();
        summaryPanel.SetActive(true);
        hypothesisPanel.SetActive(false);
    }
}