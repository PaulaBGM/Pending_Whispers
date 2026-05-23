using System;
using UnityEngine;

public class CasesPageController : MonoBehaviour
{
    [SerializeField] private GameObject listPanel;
    [SerializeField] private GameObject summaryPanel;
    [SerializeField] private GameObject hypothesisPanel;
    
    [SerializeField] private HypothesisController hypothesis;

    private CaseData selectedCase;

    private void Start()
    {
        summaryPanel.SetActive(true);
        hypothesisPanel.SetActive(false);
    }

    public void SelectCase(CaseData caseData)
    {
        selectedCase = caseData;

        listPanel.SetActive(false);

        summaryPanel.SetActive(true);
        hypothesisPanel.SetActive(false);

        UpdateSummary(caseData);
    }

    public void OnCreateHypothesis()
    {
        hypothesisPanel.SetActive(true);

        hypothesis.OpenHypothesis();

        summaryPanel.SetActive(false);
    }

    public void CloseHypothesis()
    {
        
    }

    private void UpdateSummary(CaseData caseData)
    {
        // actualizar texto, progreso, etc.
    }

    public void BackToList()
    {
        listPanel.SetActive(true);

        summaryPanel.SetActive(true);
        hypothesisPanel.SetActive(false);
    }
}