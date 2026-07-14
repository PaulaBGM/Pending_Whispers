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
            LoadCase(currentCaseData);
    }

    private void Start()
    {
        if (GameProgress.Instance != null)
            GameProgress.Instance.OnFlagAdded += HandleFlagAdded;
    }

    protected override void OnDestroy()
    {
        if (GameProgress.Instance != null)
            GameProgress.Instance.OnFlagAdded -= HandleFlagAdded;

        base.OnDestroy();
    }

    public void LoadCase(CaseData data)
    {
        if (data == null)
            return;

        currentCaseData = data;
        currentCase = new CaseRuntime(data);

        CaseJournalSystem.Instance?.TryAddCase(currentCase);
    }

    private void HandleFlagAdded(FlagSO flag)
    {
        if (currentCase == null || flag == null)
            return;

        foreach (var clue in currentCase.data.requiredClues)
        {
            if (clue == flag)
            {
                currentCase.AddClue(clue);
                break;
            }
        }
    }

    public void ResolveCurrentCase()
    {
        TryResolveCase();
    }

    public void TryResolveCase()
    {
        if (currentCase == null || currentCase.isResolved)
            return;

        if (!currentCase.CanResolve())
        {
            UIGameEvents.RaiseFeedback("Faltan pistas...");
            return;
        }

        EvaluateOutcomes();
    }

    private void EvaluateOutcomes()
    {
        foreach (var outcome in currentCase.data.outcomes)
        {
            if (GameProgress.Instance.HasAllFlags(outcome.requiredFlags))
            {
                ApplyOutcome(outcome);
                return;
            }
        }

        UIGameEvents.RaiseFeedback("No has llegado a ninguna conclusión clara...");
    }

    private void ApplyOutcome(CaseOutcome outcome)
    {
        currentCase.isResolved = true;
        currentCase.chosenOutcome = outcome.outcomeID;

        if (outcome.resultingFlags != null)
        {
            foreach (var flag in outcome.resultingFlags)
            {
                if (flag != null)
                    GameProgress.Instance.AddFlag(flag);
            }
        }

        if (outcome.reputationReward != 0)
            ReputationManager.Instance?.AddReputation(outcome.reputationReward);

        currentCase.Resolve();

        onCaseResolved?.Raise(outcome);

        UIGameEvents.RaiseFeedback(outcome.feedbackText);

        Debug.Log($"[Case] Resolved '{currentCase.data.caseTitle}' -> Outcome: {outcome.outcomeID}");
    }

    public CaseRuntime GetCurrentCase() => currentCase;

    public CaseData GetCurrentCaseData() => currentCaseData;
}