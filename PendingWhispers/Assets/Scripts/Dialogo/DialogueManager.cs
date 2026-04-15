using UnityEngine;
using System.Collections.Generic;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    private DialogueData currentDialogue;
    private DialogueNode currentNode;

    void Awake()
    {
        Instance = this;
    }

    public void StartDialogue(DialogueData dialogue)
    {
        currentDialogue = dialogue;
        currentDialogue.Initialize();

        GoToNode("start");
    }

    public void GoToNode(string nodeID)
    {
        DialogueNode node = currentDialogue.GetNode(nodeID);

        if (node == null)
        {
            Debug.LogError("Nodo no encontrado: " + nodeID);
            return;
        }

        if (!GameState.Instance.HasAllFlags(node.requiredFlags))
        {
            Debug.Log("Nodo bloqueado");
            return;
        }

        currentNode = node;
        ShowNode();
    }

    void ShowNode()
    {
        DialogueUI.Instance.ShowDialogue(currentNode.speaker, currentNode.text);

        if (currentNode.choices != null && currentNode.choices.Count > 0)
        {
            List<DialogueChoice> validChoices = new List<DialogueChoice>();

            foreach (var choice in currentNode.choices)
            {
                if (GameState.Instance.HasAllFlags(choice.requiredFlags))
                {
                    validChoices.Add(choice);
                }
            }

            DialogueUI.Instance.ShowChoices(validChoices);
        }
    }

    public void ChooseChoice(DialogueChoice choice)
    {
        if (choice.addFlags != null)
        {
            foreach (var flag in choice.addFlags)
            {
                GameState.Instance.AddFlag(flag);
            }
        }

        GoToNode(choice.nextNodeID);
    }

    public void Next()
    {
        if (!string.IsNullOrEmpty(currentNode.nextNodeID))
        {
            GoToNode(currentNode.nextNodeID);
        }
        else
        {
            EndDialogue();
        }
    }

    public void OnContinuePressed()
    {
        Next();
    }

    public void StartDialogueByID(string nodeID)
    {
        GoToNode(nodeID);
    }

    void EndDialogue()
    {
        DialogueUI.Instance.Hide();
    }
}