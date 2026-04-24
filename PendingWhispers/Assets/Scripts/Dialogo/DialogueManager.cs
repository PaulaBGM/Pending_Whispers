using UnityEngine;
using System.Collections.Generic;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    private DialogueData currentDialogue;
    private DialogueNode currentNode;

    private PlayerController player;

    void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        PlayerController.OnPlayerSpawned += SetPlayer;
    }

    void OnDisable()
    {
        PlayerController.OnPlayerSpawned -= SetPlayer;
    }

    void Start()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerController>();
    }

    void SetPlayer(PlayerController p)
    {
        player = p;
    }

    public void StartDialogue(DialogueData dialogue)
    {
        currentDialogue = dialogue;
        currentDialogue.Initialize();

        if (player != null)
            player.canMove = false;

        GoToNode("start");
    }

    public void GoToNode(string nodeID)
    {
        DialogueNode node = currentDialogue.GetNode(nodeID);

        if (node == null) return;

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
        var character = currentDialogue.GetCharacter(currentNode.speakerID);

        string speakerName = character != null ? character.displayName : "???";

        DialogueUI.Instance.ShowLine(character, speakerName, currentNode.text);

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

            if (validChoices.Count > 0)
            {
                DialogueUI.Instance.ShowChoices(validChoices);
                return;
            }
        }

        DialogueUI.Instance.ShowContinue();
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

        if (choice.endsDialogue)
        {
            EndDialogue();
            return;
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

    public void EndDialogue()
    {
        DialogueUI.Instance.Hide();

        if (player != null)
            player.canMove = true;
    }
}