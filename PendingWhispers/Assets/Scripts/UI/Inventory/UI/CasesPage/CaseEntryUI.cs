using System;
using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class CaseEntryUI : MonoBehaviour, IPointerClickHandler
{
    [Header("UI")]
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private UnityEngine.UI.Image borderImage;

    public event Action<CaseData> OnEntryClicked;

    private CaseData data;
    private bool empty = true;

    private const float SelectedAlpha = 1f;
    private const float DeselectedAlpha = 0.4f;

    private Color originalColor;

    private void Awake()
    {
        if (borderImage != null)
            originalColor = borderImage.color;
        
        ResetData();
        Deselect();
    }

    // ---------------- DATA ----------------

    public void SetData(CaseData caseData)
    {
        if (caseData == null) return;

        data = caseData;
        empty = false;

        if (titleText != null) titleText.text = caseData.caseID;
    }

    public void ResetData()
    {
        data = null;
        empty = true;

        if (titleText != null) titleText.text = "";
    }

    public CaseData GetData()
    {
        return data;
    }

    // ---------------- SELECTION ----------------

    public void Select()
    {
        SetAlpha(SelectedAlpha);
    }

    public void Deselect()
    {
        SetAlpha(DeselectedAlpha);
    }

    private void SetAlpha(float alpha)
    {
        if (borderImage == null) return;

        Color c = originalColor;
        c.a = alpha;
        borderImage.color = c;
    }

    // ---------------- CLICK ----------------

    public void OnPointerClick(PointerEventData eventData)
    {
        if (empty || data == null)
            return;

        OnEntryClicked?.Invoke(data);
    }
}