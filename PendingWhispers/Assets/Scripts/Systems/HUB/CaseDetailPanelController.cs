using System;
using UnityEngine;
using UnityEngine.UIElements;

public class CaseDetailPanelController : MonoBehaviour
{
    [SerializeField] private UIDocument document;

    private VisualElement root;
    private Label caseNameLabel;
    private Label applicantName;
    private Label applicantLocation;
    private Label requestDescription;
    private Label rewardDescription;
    private Button acceptButton;
    private Button cancelButton;

    private CaseData currentData;
    private Action<CaseData> onAccept;

    private void Awake()
    {
        if (document == null)
        {
            Debug.LogError("[CaseDetailPanelController] Document no asignado en el Inspector.");
            return;
        }

        root = document.rootVisualElement;

        caseNameLabel = root.Q<Label>("T_ScaledPostit_CaseName");
        applicantName = root.Q<Label>("T_Applicant_Name");
        applicantLocation = root.Q<Label>("T_Applicant_Location");
        requestDescription = root.Q<Label>("T_RequestDescription");
        rewardDescription = root.Q<Label>("T_Reward_Description");
        acceptButton = root.Q<Button>("BT_AcceptCase");
        cancelButton = root.Q<Button>("BT_Cancel_Button");

        if (acceptButton != null)
            acceptButton.clicked += () => onAccept?.Invoke(currentData);
        else
            Debug.LogWarning("[CaseDetailPanelController] No se encontró BT_AcceptCase en HUB_ScaledPostit_Template.");

        if (cancelButton != null)
            cancelButton.clicked += Close;
        else
            Debug.LogWarning("[CaseDetailPanelController] No se encontró BT_Cancel_Button en HUB_ScaledPostit_Template.");

        Close();
    }

    public void Open(CaseData data, Action<CaseData> acceptCallback)
    {
        if (root == null) return;

        currentData = data;
        onAccept = acceptCallback;

        if (caseNameLabel != null) caseNameLabel.text = data.caseTitle;
        if (applicantName != null) applicantName.text = data.requesterName;
        if (applicantLocation != null) applicantLocation.text = data.requesterLocation;
        if (requestDescription != null) requestDescription.text = data.requestSummary;
        if (rewardDescription != null) rewardDescription.text = $"+{data.baseReputationReward}% de reputación";

        root.style.display = DisplayStyle.Flex;
    }

    public void Close()
    {
        currentData = null;

        if (root != null)
            root.style.display = DisplayStyle.None;
    }
}