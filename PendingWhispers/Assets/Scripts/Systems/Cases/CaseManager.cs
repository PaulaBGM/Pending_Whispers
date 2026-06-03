using UnityEngine;

public class CaseManager : MonoBehaviour
{
    public static CaseManager Instance;

    [SerializeField] private CaseData currentCaseData;

    private CaseRuntime currentCase;

    void Awake()
    {
        Instance = this;

        if (currentCaseData != null)
        {
            LoadCase(currentCaseData);
        }
    }

    void Start()
    {
        if (GameProgress.Instance != null)
        {
            GameProgress.Instance.OnFlagAdded += HandleFlagAdded;
        }
    }

    void OnDestroy()
    {
        if (GameProgress.Instance != null)
        {
            GameProgress.Instance.OnFlagAdded -= HandleFlagAdded;
        }
    }

    public void LoadCase(CaseData data)
    {
        if (data == null)
            return;

        currentCaseData = data;

        currentCase = new CaseRuntime(data);

        if (CaseJournalSystem.Instance != null)
        {
            CaseJournalSystem.Instance.TryAddCase(currentCase);
        }
    }

    void HandleFlagAdded(FlagSO flag)
    {
        if (currentCase == null || flag == null)
            return;

        foreach (var clue in currentCase.data.requiredClues)
        {
            if (clue.id == flag.id)
            {
                currentCase.AddClue(clue);

                var page = FindFirstObjectByType<CasePageController>();

                if (page != null)
                {
                    page.RefreshUI();
                }

                break;
            }
        }
    }

    public void TryResolveCase()
    {
        if (currentCase == null)
            return;

        if (currentCase.isResolved)
            return;

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

        UIFeedbackManager.Instance.ShowMessage(
            "No has llegado a ninguna conclusión clara..."
        );
    }

    void ApplyOutcome(CaseOutcome outcome)
    {
        currentCase.isResolved = true;

        currentCase.chosenOutcome = outcome.outcomeID;

        if (outcome.resultingFlags != null)
        {
            foreach (var flag in outcome.resultingFlags)
            {
                if (flag == null)
                    continue;

                GameProgress.Instance.AddFlag(flag);
            }
        }

        if (ReputationManager.Instance != null &&
            outcome.reputationReward != 0)
        {
            ReputationManager.Instance.AddReputation(
                outcome.reputationReward
            );
        }

        currentCase.Resolve();

        var page = FindFirstObjectByType<CasePageController>();

        if (page != null)
        {
            page.RefreshUI();
        }

        UIGameEvents.RaiseFeedback(outcome.feedbackText);

        Debug.Log(
            $"[Case] Resolved '{currentCase.data.caseTitle}' -> Outcome: {outcome.outcomeID}"
        );
    }
    public void ResolveCurrentCase()
    {
        if (currentCase == null)
            return;

        if (currentCase.isResolved)
            return;

        TryResolveCase();
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