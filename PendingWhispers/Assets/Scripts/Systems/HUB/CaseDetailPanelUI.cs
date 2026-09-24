using UnityEngine;
using UnityEngine.UIElements;

public class CaseDetailPanelUI : MonoBehaviour
{
    [SerializeField] private UIDocument document;

    private VisualElement root;
    private Label caseNumber, requesterName, requesterLocation, summary, reward;
    private CaseData currentData;
    private CaseBoardPanelUI boardPanel;

    private void Awake()
    {
        root = document.rootVisualElement.Q<VisualElement>("case-detail-panel");
        caseNumber = root.Q<Label>("case-number");
        requesterName = root.Q<Label>("requester-name");
        requesterLocation = root.Q<Label>("requester-location");
        summary = root.Q<Label>("summary");
        reward = root.Q<Label>("reward");

        root.Q<Button>("accept-button").clicked += OnAcceptClicked;
        root.Q<Button>("close-button").clicked += Close;
        Close();
    }

    public void Open(CaseData data, CaseBoardPanelUI board)
    {
        currentData = data;
        boardPanel = board;

        caseNumber.text = data.caseID;
        requesterName.text = data.requesterName;
        requesterLocation.text = data.requesterLocation;
        summary.text = data.requestSummary;
        reward.text = $"Resompensa: +{data.baseReputationReward}% de reputación";

        root.style.display = DisplayStyle.Flex;
    }

    public void Close() => root.style.display = DisplayStyle.None;

    private void OnAcceptClicked()
    {
        if (currentData == null) return;

        if (currentData.startedFlag != null)
            GameProgress.Instance.AddFlag(currentData.startedFlag);

        CaseManager.Instance.LoadCase(currentData);
        UIGameEvents.RaiseFeedback($"Caso aceptado: {currentData.caseTitle}");

        Close();
        boardPanel.Close();
    }
}