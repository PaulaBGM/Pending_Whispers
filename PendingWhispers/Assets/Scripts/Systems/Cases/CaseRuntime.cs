using System;
using System.Collections.Generic;
using System.Linq;

public class CaseRuntime
{
    public CaseData data;

    public bool isResolved;
    public string chosenOutcome;

    public HashSet<FlagSO> localFlags = new();

    public event Action OnCaseUpdated;

    public CaseRuntime(CaseData data)
    {
        this.data = data;
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
        return $"{GetCompletedEvidenceCount()}/{data.requiredClues.Count}";
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

    public int GetCompletedObjectivesCount()
    {
        int completed = 0;

        foreach (var objective in data.objectives)
        {
            if (IsObjectiveCompleted(objective))
                completed++;
        }

        return completed;
    }

    public int GetCompletedEvidenceCount()
    {
        if (data.requiredClues == null || data.requiredClues.Count == 0)
            return 0;

        return data.requiredClues.Count(clue =>
            clue != null &&
            GameProgress.Instance.HasFlag(clue));
    }

    public bool IsObjectiveCompleted(CaseObjective objective)
    {
        if (objective == null)
            return false;

        if (objective.completedFlag == null)
            return false;

        return GameProgress.Instance.HasFlag(objective.completedFlag);
    }

    public string GetCurrentObjective()
    {
        foreach (var objective in data.objectives)
        {
            if (!IsObjectiveCompleted(objective))
                return objective.objectiveText;
        }

        return "Case Completed";
    }

    public void Resolve()
    {
        isResolved = true;

        OnCaseUpdated?.Invoke();
    }
}