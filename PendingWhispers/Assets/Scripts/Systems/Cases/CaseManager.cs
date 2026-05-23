using UnityEngine;

public class CaseManager : MonoBehaviour
{
    public static CaseManager Instance;

    [SerializeField] private CaseData currentCaseData;

    private CaseRuntime currentCase;

    void Awake()
    {
        Instance = this;
        LoadCase(currentCaseData);
    }

    public void LoadCase(CaseData data)
    {
        currentCaseData = data;
        currentCase = new CaseRuntime(data);
    }

    public void TryResolveCase()
    {
        if (currentCase.isResolved) return;

        if (!currentCase.CanResolve())
        {
            UIFeedbackManager.Instance.ShowMessage("Faltan pistas...");
            return;
        }

        EvaluateOutcomes();
    }

    void EvaluateOutcomes()
    {
        foreach (var outcome in currentCase.data.outcomes)
        {
            if (GameProgress.Instance.HasAllFlags(outcome.requiredFlags))
            {
                ApplyOutcome(outcome);
                return;
            }
        }

        UIFeedbackManager.Instance.ShowMessage("No has llegado a ninguna conclusi�n clara...");
    }

    void ApplyOutcome(CaseOutcome outcome)
    {
        currentCase.isResolved = true;
        currentCase.chosenOutcome = outcome.outcomeID;

        foreach (var flag in outcome.resultingFlags)
        {
            GameProgress.Instance.AddFlag(flag);
        }

        UIGameEvents.RaiseFeedback(outcome.feedbackText);
    }
    
    public CaseRuntime GetCurrentCase()
    {
        return currentCase;
    }

    public CaseData GetCurrentCaseData()
    {
        return currentCaseData;
    }
}