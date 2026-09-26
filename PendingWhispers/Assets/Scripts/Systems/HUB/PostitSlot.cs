using System;
using UnityEngine.UIElements;

public class PostitSlot
{
    private readonly VisualElement root;
    private readonly Label caseName;
    private readonly Label applicantName;
    private readonly Label applicantLocation;
    private readonly Label reputationValue;
    private readonly Label stateLabel;

    private CaseData data;

    public PostitSlot(VisualElement postitButton, Action<CaseData> onClick)
    {
        root = postitButton;

        caseName = root.Q<Label>("T_Postit_CaseName");
        applicantName = root.Q<Label>("T_LittlePostit_Name");
        applicantLocation = root.Q<Label>("T_LittlePostit_Localization");

        // "T_ReputationLabel" está duplicado dentro del mismo post-it
        // (reputación vs. estado del caso), por eso se acota por contenedor.
        var reputationContainer = root.Q<VisualElement>("HUB_Board_Postit_ReputationLabel");
        var stateContainer = root.Q<VisualElement>("HUB_Board_Postit_CaseState");

        reputationValue = reputationContainer?.Q<Label>("T_ReputationLabel");
        stateLabel = stateContainer?.Q<Label>("T_ReputationLabel");

        if (root is Button button)
            button.clicked += () => onClick(data);
    }

    public void Bind(CaseData caseData)
    {
        data = caseData;

        if (caseName != null) caseName.text = data.caseTitle;
        if (applicantName != null) applicantName.text = data.requesterName;
        if (applicantLocation != null) applicantLocation.text = data.requesterLocation;
        if (reputationValue != null) reputationValue.text = $"{data.baseReputationReward}%";
        if (stateLabel != null) stateLabel.text = "Available";

        root.style.display = DisplayStyle.Flex;
    }

    public void Hide()
    {
        data = null;
        root.style.display = DisplayStyle.None;
    }
}