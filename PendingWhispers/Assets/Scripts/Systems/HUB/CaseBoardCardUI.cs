using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class CaseBoardCardUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text characterText;
    [SerializeField] private TMP_Text locationText;
    [SerializeField] private TMP_Text reputationText;
    [SerializeField] private Image background;

    private CaseData data;
    private Action<CaseData> onSelect;
    private const float HOVER_SCALE = 1.08f;

    public void Setup(CaseData caseData, Action<CaseData> callback)
    {
        data = caseData;
        onSelect = callback;

        titleText.text = data.caseTitle;
        characterText.text = data.requesterName;
        locationText.text = data.requesterLocation;
        reputationText.text = $"Reputación: +{data.baseReputationReward}%";
        background.color = data.cardColor;
    }

    public void OnPointerEnter(PointerEventData e)
    {
        transform.SetAsLastSibling();
        transform.localScale = Vector3.one * HOVER_SCALE;
    }

    public void OnPointerExit(PointerEventData e) => transform.localScale = Vector3.one;

    public void OnPointerClick(PointerEventData e) => onSelect(data);
}