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

    [Tooltip("Estado del Animator con el fantasma ya transformado (p. ej. GhostCase1_Transformation). " +
             "Al cargar la escena con el flag de transformado, se salta directamente a su ultimo frame.")]
    [SerializeField] private string transformedStateName;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void Start()
    {
        RestoreTransformedState();
    }

    /// <summary>
    /// Si la transformacion ya ocurrio (flag guardado), el NPC aparece transformado,
    /// sin repetir la animacion ni el sonido.
    /// </summary>
    private void RestoreTransformedState()
    {
        if (transformedFlag == null || GameProgress.Instance == null)
            return;

        if (!GameProgress.Instance.HasFlag(transformedFlag))
            return;

        if (animator == null || string.IsNullOrEmpty(transformedStateName))
            return;

        animator.Play(transformedStateName, 0, 1f);
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

        // El flag de transformado lo pone SOLO este metodo. Si un dialogo lo anade antes de
        // terminar, aqui se cree que ya estaba transformado y no se transforma nunca.
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
