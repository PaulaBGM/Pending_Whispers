using UnityEngine;

public class GhostTransformationController : MonoBehaviour
{
    [SerializeField] private FlagSO correctHypothesisFlag;
    [SerializeField] private FlagSO transformedFlag;
    [SerializeField] private Animator animator;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    public void TryTransform()
    {
        if (correctHypothesisFlag == null)
            return;

        if (!GameProgress.Instance.HasFlag(correctHypothesisFlag))
            return;

        if (transformedFlag == null &&
            GameProgress.Instance.HasFlag(transformedFlag))
            return;

        animator.SetTrigger("Transformation");

        if (transformedFlag != null)
            GameProgress.Instance.AddFlag(transformedFlag);
    }
}