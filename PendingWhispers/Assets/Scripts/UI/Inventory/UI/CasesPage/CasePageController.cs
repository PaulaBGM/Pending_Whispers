using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CasePageController : MonoBehaviour
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
    [SerializeField] private TMP_Text objectiveText;
    [SerializeField] private TMP_Text progressText;

    [SerializeField] private Image caseImage;

    private readonly List<CaseEntryUI> spawnedEntries = new();

    private CaseRuntime selectedCase;

    private void OnEnable()
    {
        if (CaseJournalSystem.Instance != null)
            CaseJournalSystem.Instance.OnCasesChanged += RefreshUI;
    }

    private void OnDisable()
    {
        if (CaseJournalSystem.Instance != null)
            CaseJournalSystem.Instance.OnCasesChanged -= RefreshUI;

        if (selectedCase != null)
            selectedCase.OnCaseUpdated -= RefreshSelectedCase;
    }

    private void Start()
    {
        summaryPanel.SetActive(true);

        hypothesisPanel.SetActive(false);

        RefreshUI();
    }

    public void RefreshUI()
    {
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
        {
            selectedCase.OnCaseUpdated -= RefreshSelectedCase;
        }

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
        if (selectedCase == null) return;

        UpdateSummary(selectedCase);

        foreach (var entry in spawnedEntries)
        {
            if (entry.GetData() == selectedCase)
            {
                entry.SetData(selectedCase);
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

        objectiveText.text = runtime.currentObjective;

        progressText.text = runtime.GetProgressText();

        if (caseImage != null)
            caseImage.sprite = data.caseIcon;
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
}