using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CaseEntryUI : JournalEntryUI<CaseRuntime>
{
    [Header("UI")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text progressText;

    public void SetData(CaseRuntime runtime)
    {
        if (runtime == null || runtime.data == null)
        {
            ResetData();
            return;
        }

        SetEntry(runtime);

        if (icon != null)
        {
            icon.gameObject.SetActive(true);
            icon.sprite = runtime.data.caseIcon;
            icon.color = Color.white;
        }

        if (titleText != null)
            titleText.text = runtime.data.caseTitle;

        if (progressText != null)
            progressText.text = runtime.GetProgressText();
    }

    public override void ResetData()
    {
        base.ResetData();

        if (icon != null)
        {
            icon.sprite = null;
            icon.gameObject.SetActive(false);
        }

        if (titleText != null)
            titleText.text = "";

        if (progressText != null)
            progressText.text = "";
    }
}