using UnityEngine;

public class CaseManager : BaseSingleton<CaseManager>
{

    [SerializeField] private CaseData currentCaseData;
    [SerializeField] private CaseOutcomeEventChannelSO onCaseResolved;

    private CaseRuntime currentCase;

    protected override void Awake()
    {
        base.Awake();

        if (Instance != this)
            return;

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

    protected override void OnDestroy()
    {
        if (GameProgress.Instance != null)
        {
            GameProgress.Instance.OnFlagAdded -= HandleFlagAdded;
        }

        base.OnDestroy();
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
        onCaseResolved?.Raise(outcome);

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