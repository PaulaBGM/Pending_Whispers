using UnityEngine;
using FMODUnity;

public class GhostTransformationController : MonoBehaviour
{
    [SerializeField] private FlagSO correctHypothesisFlag;
    [SerializeField] private FlagSO transformedFlag;
    [SerializeField] private Animator animator;
    [SerializeField] private EventReference transformationEvent;
    
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
        RuntimeManager.PlayOneShot(transformationEvent, transform.position);
        
        if (transformedFlag != null)
            GameProgress.Instance.AddFlag(transformedFlag);
    }
}