using System.Collections.Generic;

public class CaseRuntime
{
    public CaseData data;

    public bool isResolved;
    public string chosenOutcome;

    public HashSet<FlagSO> localFlags = new();

    public CaseRuntime(CaseData data)
    {
        this.data = data;
    }

    public bool CanResolve()
    {
        return GameProgress.Instance.HasAllFlags(data.requiredClues);
    }
}