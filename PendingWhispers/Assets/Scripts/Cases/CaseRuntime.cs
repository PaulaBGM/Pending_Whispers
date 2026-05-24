using UnityEngine;

[System.Serializable]
public class CaseRuntime
{
    public CaseData data;

    public bool isResolved;

    public bool isActive;

    public string chosenOutcome;

    public CaseRuntime(CaseData data)
    {
        this.data = data;

        isResolved = false;

        isActive = true;

        chosenOutcome = "";
    }

    public bool CanResolve()
    {
        if (data.requiredClues == null ||
            data.requiredClues.Count == 0)
            return true;

        return GameProgress.Instance.HasAllFlags(
            data.requiredClues
        );
    }
}