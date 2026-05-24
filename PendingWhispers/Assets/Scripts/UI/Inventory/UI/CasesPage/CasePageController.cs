using System;
using System.Collections.Generic;
using UnityEngine;

public class CasesPageController : MonoBehaviour
{
    [Header("Panels")]
    [SerializeField] private GameObject listPanel;
    [SerializeField] private GameObject summaryPanel;
    [SerializeField] private GameObject hypothesisPanel;
    
    [Header("UI References")]
    [SerializeField] private HypothesisController hypothesis;
    [SerializeField] private CaseEntryUI caseEntryPrefab;
    [SerializeField] private Transform content;

    private readonly List<CaseEntryUI> spawnedEntries = new();

    private CaseData selectedCase;

    private void OnEnable()
    {
        if (CaseJournalSystem.Instance != null)
            CaseJournalSystem.Instance.OnCasesChanged += RefreshUI;
    }

    private void OnDisable()
    {
        if (CaseJournalSystem.Instance != null)
            CaseJournalSystem.Instance.OnCasesChanged -= RefreshUI;
    }

    private void Start()
    {
        summaryPanel.SetActive(true);
        hypothesisPanel.SetActive(false);
        
        RefreshUI();
    }

    // ---------------- UI REFRESH ----------------

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

    // ---------------- CLICK ----------------

    private void HandleCaseClicked(CaseData caseData)
    {
        selectedCase = caseData;

        foreach (var entry in spawnedEntries)
        {
            if (entry.GetData() == caseData)
                entry.Select();
            else
                entry.Deselect();
        }

        ShowSummary(caseData);
    }

    // ---------------- UI STATES ----------------

    private void ShowSummary(CaseData caseData)
    {
        summaryPanel.SetActive(true);
        hypothesisPanel.SetActive(false);

        UpdateSummary(caseData);
    }

    private void UpdateSummary(CaseData caseData)
    {
        // Aquí luego metes:
        // - progreso del caso
        // - pistas desbloqueadas
        // - estado del caso

        Debug.Log("Showing case: " + caseData.caseID);
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