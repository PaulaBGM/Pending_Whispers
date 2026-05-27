using System;
using System.Collections.Generic;

public class CaseRuntime
{
    public CaseData data;

    public bool isResolved;
    public string chosenOutcome;

    public string currentObjective;

    public HashSet<FlagSO> localFlags = new();

    public event Action OnCaseUpdated;

    public CaseRuntime(CaseData data)
    {
        this.data = data;

        currentObjective = data.currentObjective;
    }

    public bool CanResolve()
    {
        return GameProgress.Instance.HasAllFlags(data.requiredClues);
    }

    public int GetProgress()
    {
        return localFlags.Count;
    }

    public string GetProgressText()
    {
        return $"{localFlags.Count}/{data.requiredClues.Count}";
    }

    public void AddClue(FlagSO clue)
    {
        foreach (var existing in localFlags)
        {
            if (existing.id == clue.id)
                return;
        }

        localFlags.Add(clue);

        OnCaseUpdated?.Invoke();
    }

    public void UpdateObjective(string newObjective)
    {
        currentObjective = newObjective;

        OnCaseUpdated?.Invoke();
    }

    public void Resolve()
    {
        isResolved = true;

        OnCaseUpdated?.Invoke();
    }
}