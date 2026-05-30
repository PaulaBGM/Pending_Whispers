using UnityEngine;
using System.Collections.Generic;

public class NPC : MonoBehaviour, IInteractable
{
    public List<DialogueCondition> dialogues;

    private NPCExpressions expressions;

    private void Awake()
    {
        expressions = GetComponentInChildren<NPCExpressions>();
    }

    public Sprite GetExpression(DialogueExpression expression)
    {
        if (expressions == null)
            return null;

        return expressions.GetSprite(expression);
    }

    public void Interact(PlayerController_Actions player)
    {
        foreach (var d in dialogues)
        {
            if (GameProgress.Instance.HasAllFlags(d.requiredFlags))
            {
                DialogueManager.Instance.StartDialogue(d.dialogue, this);
                return;
            }
        }

        Debug.Log("No hay dialogo valido");
    }

    public Transform GetTransform()
    {
        return transform;
    }
}