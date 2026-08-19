using UnityEngine;
using System.Collections.Generic;

public class NPC : MonoBehaviour, IInteractable
{
    [Tooltip("Stable id used to scope dialogue topic seen-state across cases. Uses the GameObject name when empty.")]
    [SerializeField] private string npcId;

    public List<DialogueCondition> dialogues;

    private readonly List<DialogueCondition> validDialoguesCache = new();
    private NPCExpressions expressions;
    private GhostTransformationController transformationController;

    private void Awake()
    {
        expressions = GetComponentInChildren<NPCExpressions>();
        transformationController = GetComponent<GhostTransformationController>();
    }

    public string DialogueId => !string.IsNullOrWhiteSpace(npcId) ? npcId.Trim() : name;

    public Sprite GetExpression(DialogueExpression expression)
    {
        if (expressions == null)
            return null;

        return expressions.GetSprite(expression);
    }

    public void TryTransform()
    {
        Debug.Log($"{name} TryTransform");
        transformationController?.TryTransform();
    }

    public void Interact(PlayerController_Actions player)
    {
        List<DialogueCondition> validDialogues = GetValidDialogues();

        if (validDialogues.Count == 0)
        {
            Debug.Log("No hay dialogo valido");
            return;
        }

        if (validDialogues.Count == 1)
        {
            DialogueManager.Instance.StartDialogue(validDialogues[0].dialogue, this, validDialogues[0]);
            return;
        }

        DialogueManager.Instance.ShowDialogueSelector(validDialogues, this);
    }

    private List<DialogueCondition> GetValidDialogues()
    {
        validDialoguesCache.Clear();

        if (dialogues == null)
            return validDialoguesCache;

        foreach (DialogueCondition dialogueCondition in dialogues)
        {
            if (dialogueCondition?.dialogue == null)
                continue;

            bool hasFlags = GameProgress.Instance == null ||
                            GameProgress.Instance.HasAllFlags(dialogueCondition.requiredFlags);

            if (hasFlags)
                validDialoguesCache.Add(dialogueCondition);
        }

        return validDialoguesCache;
    }

    public Transform GetTransform()
    {
        return transform;
    }
}