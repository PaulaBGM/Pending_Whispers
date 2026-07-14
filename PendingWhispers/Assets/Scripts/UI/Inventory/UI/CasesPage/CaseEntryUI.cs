using TMPro;
using UnityEngine;

public class CaseEntryUI : JournalEntryUI<CaseRuntime>
{
    [SerializeField] private TMP_Text progressText;

    protected override void RefreshExtraUI(CaseRuntime runtime)
    {
        if (progressText != null)
            progressText.text = runtime.GetProgressText();
    }

    public override void ResetData()
    {
        base.ResetData();

        if (progressText != null)
            progressText.text = "";
    }
}