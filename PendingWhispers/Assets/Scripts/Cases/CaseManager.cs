using UnityEngine;

public class CaseManager : MonoBehaviour
{
    public static CaseManager Instance;

    [Header("Caso inicial")]
    [SerializeField] private CaseData startingCase;

    private CaseData currentCaseData;
    private CaseRuntime currentCase;

    // Acceso global
    public CaseRuntime CurrentCase => currentCase;

    public CaseData CurrentCaseData => currentCaseData;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            DontDestroyOnLoad(gameObject);

            if (startingCase != null)
            {
                LoadCase(startingCase);
            }
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadCase(CaseData data)
    {
        if (data == null)
        {
            Debug.LogError("[CaseManager] Caso NULL");
            return;
        }

        currentCaseData = data;
<<<<<<< Updated upstream:PendingWhispers/Assets/Scripts/Cases/CaseManager.cs
        currentCase = new CaseRuntime(data);
=======

        currentCase = new CaseRuntime(data);

        Debug.Log(
            "[CaseManager] Caso cargado: " +
            data.caseID
        );

        // Flag de inicio
        if (data.startedFlag != null)
        {
            GameProgress.Instance.AddFlag(
                data.startedFlag
            );
        }
>>>>>>> Stashed changes:PendingWhispers/Assets/Scripts/Systems/Cases/CaseManager.cs
    }

    public void TryResolveCase()
    {
        if (currentCase == null)
            return;

        if (currentCase.isResolved)
            return;

        if (!currentCase.CanResolve())
        {
            UIFeedbackManager.Instance.ShowMessage(
                "Faltan pistas..."
            );

            return;
        }

        EvaluateOutcomes();
    }

    private void EvaluateOutcomes()
    {
        foreach (var outcome in currentCase.data.outcomes)
        {
            if (GameProgress.Instance.HasAllFlags(
                outcome.requiredFlags
            ))
            {
                ApplyOutcome(outcome);
                return;
            }
        }

<<<<<<< Updated upstream:PendingWhispers/Assets/Scripts/Cases/CaseManager.cs
        UIFeedbackManager.Instance.ShowMessage("No has llegado a ninguna conclusi�n clara...");
=======
        UIFeedbackManager.Instance.ShowMessage(
            "No has llegado a ninguna conclusión clara..."
        );
>>>>>>> Stashed changes:PendingWhispers/Assets/Scripts/Systems/Cases/CaseManager.cs
    }

    private void ApplyOutcome(CaseOutcome outcome)
    {
        currentCase.isResolved = true;

        currentCase.chosenOutcome = outcome.outcomeID;

        // Flags del final
        foreach (var flag in outcome.resultingFlags)
        {
            GameProgress.Instance.AddFlag(flag);
        }

<<<<<<< Updated upstream:PendingWhispers/Assets/Scripts/Cases/CaseManager.cs
        UIGameEvents.RaiseFeedback(outcome.feedbackText);
=======
        // Caso completado
        if (currentCase.data.completedFlag != null)
        {
            GameProgress.Instance.AddFlag(
                currentCase.data.completedFlag
            );
        }

        // Desbloquear siguiente caso
        if (currentCase.data.nextCase != null &&
            currentCase.data.nextCase.unlockFlag != null)
        {
            GameProgress.Instance.AddFlag(
                currentCase.data.nextCase.unlockFlag
            );
        }

        // Desbloquear localización
        if (!string.IsNullOrEmpty(
                currentCase.data.unlockedNodeID))
        {
            MapState.Instance.UnlockNode(
                currentCase.data.unlockedNodeID
            );

            UIGameEvents.RaiseLocationUnlocked(
                currentCase.data.unlockedNodeID
            );
        }

        UIGameEvents.RaiseFeedback(
            outcome.feedbackText
        );

        Debug.Log(
            "[CaseManager] Caso resuelto: " +
            currentCase.data.caseID
        );
>>>>>>> Stashed changes:PendingWhispers/Assets/Scripts/Systems/Cases/CaseManager.cs
    }
}