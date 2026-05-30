using UnityEngine;

public class CaseManager : MonoBehaviour
{
    public static CaseManager Instance;

    [SerializeField] private CaseData currentCaseData;

    [Header("Hypothesis")]
    [SerializeField] private HypothesisController hypothesisController;
    [SerializeField] private FlagSO perfectHypothesisFlag;
    [SerializeField] private FlagSO partialHypothesisFlag;
    [SerializeField] private FlagSO wrongHypothesisFlag;

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

    public void TryResolveCaseWithHypothesis()
    {
        if (currentCase == null)
            return;

        if (currentCase.isResolved)
            return;

        if (hypothesisController == null)
        {
            Debug.LogError("Missing HypothesisController");
            return;
        }

        if (!currentCase.CanResolve())
        {
            UIFeedbackManager.Instance.ShowMessage("Faltan pistas...");
            return;
        }

        int score = hypothesisController.GetCorrectCount();
        int totalSlots = hypothesisController.TotalSlots;

        if (totalSlots <= 0)
        {
            Debug.LogError("Hypothesis has no slots.");
            return;
        }

        if (score == totalSlots)
        {
            if (perfectHypothesisFlag != null)
                GameProgress.Instance.AddFlag(perfectHypothesisFlag);
        }
        else if (score >= Mathf.CeilToInt(totalSlots * 0.5f))
        {
            if (partialHypothesisFlag != null)
                GameProgress.Instance.AddFlag(partialHypothesisFlag);
        }
        else
        {
            if (wrongHypothesisFlag != null)
                GameProgress.Instance.AddFlag(wrongHypothesisFlag);
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

        UIFeedbackManager.Instance.ShowMessage("No has llegado a ninguna conclusión clara...");
    }

    void ApplyOutcome(CaseOutcome outcome)
    {
        currentCase.isResolved = true;

        currentCase.chosenOutcome = outcome.outcomeID;

        foreach (var flag in outcome.resultingFlags)
        {
            GameProgress.Instance.AddFlag(flag);
        }

        currentCase.Resolve();

        var page = FindFirstObjectByType<CasePageController>();

        if (page != null)
        {
            page.RefreshUI();
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