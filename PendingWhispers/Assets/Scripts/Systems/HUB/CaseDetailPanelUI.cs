using TMPro;
using UnityEngine;

public class CaseDetailPanelUI : MonoBehaviour
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private TMP_Text caseNumberText;
    [SerializeField] private TMP_Text requesterNameText;
    [SerializeField] private TMP_Text requesterLocationText;
    [SerializeField] private TMP_Text requestSummaryText;
    [SerializeField] private TMP_Text rewardText;

    private CaseData currentData;
    private CaseBoardPanelUI boardPanel;

    public void Open(CaseData data, CaseBoardPanelUI board)
    {
        currentData = data;
        boardPanel = board;

        caseNumberText.text = data.caseID;
        requesterNameText.text = data.requesterName;
        requesterLocationText.text = data.requesterLocation;
        requestSummaryText.text = data.requestSummary;
        rewardText.text = $"Resompensa: +{data.baseReputationReward}% de reputación";

        panelRoot.SetActive(true);
    }

    public void Close() => panelRoot.SetActive(false);

    public void OnAcceptClicked()
    {
        if (currentData == null) return;

        if (currentData.startedFlag != null)
            GameProgress.Instance.AddFlag(currentData.startedFlag);   // esto ya desbloquea el nodo del mapa si comparte flag

        CaseManager.Instance.LoadCase(currentData);                   // ya actualiza CaseJournalSystem -> diario

        UIGameEvents.RaiseFeedback($"Caso aceptado: {currentData.caseTitle}");

        Close();
        boardPanel.Close();
    }
}