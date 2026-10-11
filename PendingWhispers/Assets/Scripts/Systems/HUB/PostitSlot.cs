using System;
using UnityEngine;
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

        // Diagnóstico: confirma si este elemento es realmente un Button
        // (y por tanto puede recibir "clicked") y si le llegan eventos
        // de puntero en absoluto.
        Debug.Log($"[PostitSlot] '{root.name}' creado. ¿Es Button? {root is Button}. " +
            $"pickingMode={root.pickingMode}, visible={root.resolvedStyle.display}.");

        if (root is Button button)
        {
            button.clicked += () =>
            {
                Debug.Log($"[PostitSlot] CLICK recibido en '{root.name}' (caso: {data?.caseTitle}).");
                onClick(data);
            };
        }
        else
        {
            Debug.LogWarning($"[PostitSlot] '{root.name}' NO es un Button — el UXML puede haber cambiado su tipo.");
        }

        // Fallback de diagnóstico: esto se dispara con cualquier puntero
        // que baje sobre el elemento, exista o no un Button real debajo.
        root.RegisterCallback<PointerDownEvent>(evt =>
            Debug.Log($"[PostitSlot] PointerDownEvent en '{root.name}' (target real: {evt.target})."));
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