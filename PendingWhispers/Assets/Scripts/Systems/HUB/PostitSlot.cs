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

        // Escopamos por contenedor porque "T_ReputationLabel" está duplicado
        // dentro del mismo post-it (reputación vs. estado del caso).
        reputationValue = root.Q<VisualElement>("HUB_Board_Postit_ReputationLabel").Q<Label>("T_ReputationLabel");
        stateLabel = root.Q<VisualElement>("HUB_Board_Postit_CaseState").Q<Label>("T_ReputationLabel");

        (root as Button).clicked += () => onClick(data);
    }

    public void Bind(CaseData caseData)
    {
        data = caseData;
        caseName.text = data.caseTitle;
        applicantName.text = data.requesterName;
        applicantLocation.text = data.requesterLocation;
        reputationValue.text = $"{data.baseReputationReward}%";
        stateLabel.text = "Available";
        root.style.display = DisplayStyle.Flex;
    }

    public void Hide()
    {
        data = null;
        root.style.display = DisplayStyle.None;
    }
}