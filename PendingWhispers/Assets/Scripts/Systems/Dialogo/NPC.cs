using UnityEngine;
using System.Collections.Generic;

public class NPC : MonoBehaviour, IInteractable
{
    public List<DialogueCondition> dialogues;

    public void Interact(PlayerController_MovementInteraction player)
    {
        foreach (var d in dialogues)
        {
            if (GameProgress.Instance.HasAllFlags(d.requiredFlags))
            {
                DialogueManager.Instance.StartDialogue(d.dialogue);
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