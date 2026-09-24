using System;
using UnityEngine;
using UnityEngine.UIElements;

public class CaseDetailPanelController : MonoBehaviour
{
    [SerializeField] private UIDocument document;

    private VisualElement root;
    private Label caseNameLabel, applicantName, applicantLocation, requestDescription, rewardDescription;
    private Button acceptButton, cancelButton;

    private CaseData currentData;
    private Action<CaseData> onAccept;

    private void Awake()
    {
        root = document.rootVisualElement;

        caseNameLabel = root.Q<Label>("T_ScaledPostit_CaseName");
        applicantName = root.Q<Label>("T_Applicant_Name");
        applicantLocation = root.Q<Label>("T_Applicant_Location");
        requestDescription = root.Q<Label>("T_RequestDescription");
        rewardDescription = root.Q<Label>("T_Reward_Description");

        acceptButton = root.Q<Button>("BT_AcceptCase");
        cancelButton = root.Q<Button>("BT_Cancel_Button");

        acceptButton.clicked += () => onAccept?.Invoke(currentData);
        cancelButton.clicked += Close;

        Close();
    }

    public void Open(CaseData data, Action<CaseData> acceptCallback)
    {
        currentData = data;
        onAccept = acceptCallback;

        caseNameLabel.text = data.caseTitle;
        applicantName.text = data.requesterName;
        applicantLocation.text = data.requesterLocation;
        requestDescription.text = data.requestSummary;
        rewardDescription.text = $"+{data.baseReputationReward}% de reputación";

        root.style.display = DisplayStyle.Flex;
    }

    public void Close()
    {
        currentData = null;
        root.style.display = DisplayStyle.None;
    }
}