using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System;

public class CaseEntryUI : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] public TMP_Text progressText;

    [SerializeField] private Image icon;

    [SerializeField] private GameObject selectedBorder;

    private CaseRuntime runtime;

    public event Action<CaseRuntime> OnEntryClicked;

    public void SetData(CaseRuntime caseRuntime)
    {
        runtime = caseRuntime;

        if (runtime == null || runtime.data == null)
            return;

        if (titleText != null)
            titleText.text = runtime.data.caseTitle;

        if (progressText != null)
            progressText.text = runtime.GetProgressText();

        if (icon != null)
            icon.sprite = runtime.data.caseIcon;

        gameObject.SetActive(true);
    }

    public void ResetData()
    {
        runtime = null;

        if (titleText != null)
            titleText.text = "";

        if (progressText != null)
            progressText.text = "";

        if (icon != null)
            icon.sprite = null;

        gameObject.SetActive(false);
    }

    public void Select()
    {
        if (selectedBorder != null)
            selectedBorder.SetActive(true);
    }

    public void Deselect()
    {
        if (selectedBorder != null)
            selectedBorder.SetActive(false);
    }

    public void OnClick()
    {
        if (runtime != null)
            OnEntryClicked?.Invoke(runtime);
    }

    public CaseRuntime GetData()
    {
        return runtime;
    }
}