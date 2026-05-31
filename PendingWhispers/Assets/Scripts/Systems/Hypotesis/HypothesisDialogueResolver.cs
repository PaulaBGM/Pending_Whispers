using UnityEngine;

public class HypothesisDialogueResolver : MonoBehaviour
{
    [SerializeField] private HypothesisController hypothesisController;

    [Header("Result Flags")]
    [SerializeField] private FlagSO goodEndingFlag;
    [SerializeField] private FlagSO badEndingFlag;

    public void PresentHypothesis()
    {
        if (hypothesisController == null)
        {
            Debug.LogError("Missing HypothesisController");
            return;
        }

        if (hypothesisController.IsCorrect())
        {
            GameProgress.Instance.AddFlag(goodEndingFlag);

            Debug.Log("[Hypothesis] Correct");
        }
        else
        {
            GameProgress.Instance.AddFlag(badEndingFlag);

            Debug.Log("[Hypothesis] Wrong");
        }
    }
}