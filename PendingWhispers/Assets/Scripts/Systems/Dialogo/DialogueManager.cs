using System;
using System.Collections.Generic;
using UnityEngine;

public class DialogueManager : BaseSingleton<DialogueManager>
{
    [SerializeField] private TestimonyEventChannelSO onTestimonyRegistered;

    public static event Action<bool> OnDialogueStateChanged;

    private DialogueRunner runner;
    private DialogueData currentDialogue;
    private DialogueNode currentNode;
    private NPC currentNPC;
    private PlayerController_Actions player;

    void OnEnable()
    {
        PlayerController_Actions.OnPlayerSpawned += SetPlayer;
    }

    void OnDisable()
    {
        PlayerController_Actions.OnPlayerSpawned -= SetPlayer;
    }

    void Start()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerController_Actions>();
    }

    void SetPlayer(PlayerController_Actions p)
    {
        player = p;
    }

    public void StartDialogue(DialogueData dialogue, NPC npc)
    {
        currentNPC = npc;

        if (dialogue == null)
        {
            Debug.LogError("[DialogueManager] Dialogue es NULL");
            return;
        }

        SetDialogueActive(true);

        currentDialogue = dialogue;
        runner = new DialogueRunner(dialogue);

        currentNode = runner.Start();

        ShowNode(currentNode);
    }

    public void GoToNode(string nodeID)
    {
        if (runner == null)
        {
            Debug.LogError("[DialogueManager] Runner es NULL");
            return;
        }

        currentNode = runner.Next(nodeID);

        ShowNode(currentNode);
    }

    public void Next()
    {
        if (currentNode == null)
        {
            Debug.LogError("[DialogueManager] currentNode es NULL");
            return;
        }

        if (!string.IsNullOrEmpty(currentNode.nextNodeID))
        {
            GoToNode(currentNode.nextNodeID);
        }
        else
        {
            EndDialogue();
        }
    }

    void ShowNode(DialogueNode node)
    {
        if (node == null)
        {
            Debug.LogError("[DialogueManager] Nodo NULL");
            return;
        }

        if (!TryGetDialogueUI(out DialogueUI dialogueUI))
            return;

        ApplyNodeEffects(node);

        var charData = currentDialogue.GetCharacter(node.speakerID);

        string speakerName = charData != null
            ? charData.displayName
            : "???";

        Sprite expressionSprite = null;

        if (charData != null)
        {
            expressionSprite = charData.GetExpression(node.expression);
        }

        dialogueUI.ShowLine(
            charData,
            speakerName,
            node.text,
            expressionSprite
        );

        RegisterDialogueToJournal(charData, node);

        List<DialogueChoice> validChoices = GetValidChoices(node);

        if (validChoices.Count > 0)
        {
            dialogueUI.ShowChoices(validChoices);
        }
    }

    void ApplyNodeEffects(DialogueNode node)
    {
        AddFlags(node.onEnterFlags, true);
        RaiseEvents(node.onEnterEvents);
    }

    public void ChooseChoice(DialogueChoice choice)
    {
        if (choice == null)
        {
            Debug.LogError("[DialogueManager] Choice NULL");
            return;
        }

        AddFlags(choice.addFlags);

        if (choice.reputationChange != 0)
        {
            ReputationManager.Instance?.AddReputation(choice.reputationChange);
        }

        choice.onSelectedEvent?.Raise();

        if (choice.endsDialogue)
        {
            EndDialogue();
            return;
        }

        DialogueUI.Instance?.ClearChoices();

        GoToNode(choice.nextNodeID);
    }

    public void EndDialogue()
    {
        SetDialogueActive(false);

        DialogueUI.Instance?.Hide();

        if (currentNPC != null)
        {
            currentNPC.TryTransform();
        }

        runner = null;
        currentNode = null;
        currentDialogue = null;
        currentNPC = null;
    }

    private void SetDialogueActive(bool isActive)
    {
        OnDialogueStateChanged?.Invoke(isActive);

        if (player != null)
        {
            player.canMove = !isActive;
        }
    }

    private bool TryGetDialogueUI(out DialogueUI dialogueUI)
    {
        dialogueUI = DialogueUI.Instance;

        if (dialogueUI != null)
            return true;

        Debug.LogError("[DialogueManager] DialogueUI no existe en escena");
        return false;
    }

    private List<DialogueChoice> GetValidChoices(DialogueNode node)
    {
        List<DialogueChoice> validChoices = new();

        if (node.choices == null || node.choices.Count == 0)
            return validChoices;

        foreach (var choice in node.choices)
        {
            if (choice == null)
                continue;

            bool hasFlags = GameProgress.Instance == null ||
                GameProgress.Instance.HasAllFlags(choice.requiredFlags);

            if (hasFlags && HasRequiredReputation(choice))
            {
                validChoices.Add(choice);
            }
        }

        return validChoices;
    }

    private bool HasRequiredReputation(DialogueChoice choice)
    {
        return ReputationManager.Instance == null ||
            ReputationManager.Instance.HasReputation(choice.requiredReputation);
    }

    private void AddFlags(List<FlagSO> flags, bool logAddedFlags = false)
    {
        if (flags == null || GameProgress.Instance == null)
            return;

        foreach (var flag in flags)
        {
            if (flag == null)
                continue;

            if (logAddedFlags)
            {
                Debug.Log("[Dialogue] Adding flag: " + flag.id);
            }

            GameProgress.Instance.AddFlag(flag);
        }
    }

    private void RaiseEvents(List<GameEventSO> events)
    {
        if (events == null)
            return;

        foreach (var evt in events)
        {
            evt?.Raise();
        }
    }

    void RegisterDialogueToJournal(DialogueCharacter charData, DialogueNode node)
    {
        if (charData == null || node == null)
            return;

        if (!node.isImportantLine)
            return;

        onTestimonyRegistered?.Raise(new TestimonyEntry(charData.displayName, charData.portrait, node.text));
    }

}
