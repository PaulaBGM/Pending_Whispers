using System;
using UnityEngine.UIElements;

public class CaseBoardCard
{
    public CaseBoardCard(VisualElement root, CaseData data, Action<CaseData> onSelect)
    {
        root.Q<Label>("title").text = data.caseTitle;
        root.Q<Label>("character").text = data.requesterName;
        root.Q<Label>("location").text = data.requesterLocation;
        root.Q<Label>("reputation").text = $"Reputación: +{data.baseReputationReward}%";
        root.style.backgroundColor = data.cardColor;

        root.RegisterCallback<PointerEnterEvent>(_ => root.BringToFront());
        root.RegisterCallback<ClickEvent>(_ => onSelect(data));
    }
}