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
        if (correctHypothesisFlag == null || GameProgress.Instance == null)
            return;

        if (!GameProgress.Instance.HasFlag(correctHypothesisFlag))
            return;

        // Antes ponia '== null': la condicion nunca se cumplia y el fantasma se volvia
        // a transformar al acabar CADA conversacion despues de la hipotesis correcta.
        if (transformedFlag != null &&
            GameProgress.Instance.HasFlag(transformedFlag))
            return;

        if (animator != null)
            animator.SetTrigger("Transformation");
        RuntimeManager.PlayOneShot(transformationEvent, transform.position);
        
        if (transformedFlag != null)
            GameProgress.Instance.AddFlag(transformedFlag);
    }
}