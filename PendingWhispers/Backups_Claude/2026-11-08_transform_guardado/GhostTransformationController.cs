using System.Collections.Generic;
using UnityEngine;
using FMODUnity;

public class GhostTransformationController : MonoBehaviour
{
    [SerializeField] private FlagSO correctHypothesisFlag;
    [SerializeField] private FlagSO transformedFlag;
    [SerializeField] private Animator animator;
    [SerializeField] private EventReference transformationEvent;

    [Tooltip("Dialogos que, al terminar, transforman al fantasma (p. ej. Revelation). " +
             "Vacio = comportamiento antiguo: se intenta al acabar cualquier dialogo.")]
    [SerializeField] private List<DialogueData> transformationDialogues = new();
    
    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    public void TryTransform(DialogueData endedDialogue)
    {
        if (transformationDialogues != null && transformationDialogues.Count > 0 &&
            (endedDialogue == null || !transformationDialogues.Contains(endedDialogue)))
            return;

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