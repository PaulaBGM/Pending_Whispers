using UnityEngine;

public class NPC : MonoBehaviour, IInteractable
{
    public DialogueData dialogue;

    public void Interact()
    {
        DialogueManager.Instance.StartDialogue(dialogue);
    }

    public Transform GetTransform()
    {
        return transform;
    }
}