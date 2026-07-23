using System;
using System.Collections.Generic;
using UnityEngine;

public class DialogueManager : BaseSingleton<DialogueManager>
{
    [SerializeField] private TestimonyEventChannelSO onTestimonyRegistered;

    [SerializeField] private BoolEventChannelSO onDialogueStateChannel;
    private DialogueRunner runner;
    private DialogueData currentDialogue;
    private DialogueNode currentNode;
    private NPC currentNPC;
    private PlayerController_Actions player;

    private void OnEnable()
    {
        PlayerController_Actions.OnPlayerSpawned += SetPlayer;
    }

    private void OnDisable()
    {
        PlayerController_Actions.OnPlayerSpawned -= SetPlayer;
    }

    private void Start()
    {
        player ??= FindFirstObjectByType<PlayerController_Actions>();
    }

    private void SetPlayer(PlayerController_Actions p)
    {
        player = p;
    }

    public void StartDialogue(DialogueData dialogue, NPC npc)
    {
        if (dialogue == null)
        {
            Debug.LogError("[DialogueManager] Dialogue es NULL");
            return;
        }

        currentNPC = npc;

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
            GoToNode(currentNode.nextNodeID);
        else
            EndDialogue();
    }

    private void ShowNode(DialogueNode node)
    {
        if (node == null)
        {
            Debug.LogError("[DialogueManager] Nodo NULL");
            return;
        }

        if (!TryGetDialogueUI(out DialogueUI dialogueUI))
            return;

        ApplyNodeEffects(node);
        DialogueCharacter charData = currentDialogue.GetCharacter(node.speakerID);
        string speakerName = charData != null? charData.displayName: "???";
        Sprite expressionSprite = charData?.GetExpression(node.expression);
        dialogueUI.ShowLine(charData,speakerName,node.text,expressionSprite);

        if (node.isImportantLine && charData != null)
        {
            onTestimonyRegistered.Raise(new TestimonyEntry(charData.displayName,charData.portrait,node.text ));          
        }

        var validChoices = GetValidChoices(node);

        if (validChoices.Count > 0)
            dialogueUI.ShowChoices(validChoices);
    }

    private void ApplyNodeEffects(DialogueNode node)
    {
        AddFlags(node.onEnterFlags, true);
        RaiseEvents(node.onEnterEvents);
    }

    public void ChooseChoice(DialogueChoice choice)
    {
        if (choice == null)
        {
            return;
        }

        AddFlags(choice.addFlags);

        if (choice.reputationChange != 0)
            ReputationManager.Instance?.AddReputation(choice.reputationChange);
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

        currentNPC?.TryTransform();

        runner = null;
        currentNode = null;
        currentDialogue = null;
        currentNPC = null;
    }

    private void SetDialogueActive(bool isActive)
    {
        onDialogueStateChannel?.Raise(isActive);
        if (player != null)
            player.canMove = !isActive;
    }

    private bool TryGetDialogueUI(out DialogueUI dialogueUI)
    {
        dialogueUI = DialogueUI.Instance;

        if (dialogueUI != null)
            return true;

        return false;
    }

    private List<DialogueChoice> GetValidChoices(DialogueNode node)
    {
        List<DialogueChoice> validChoices = new();

        if (node.choices == null || node.choices.Count == 0)
            return validChoices;

        foreach (DialogueChoice choice in node.choices)
        {
            if (choice == null)
                continue;

            bool hasFlags =
                GameProgress.Instance == null ||
                GameProgress.Instance.HasAllFlags(choice.requiredFlags);

            bool hasReputation =
                ReputationManager.Instance == null ||
                ReputationManager.Instance.HasReputation(choice.requiredReputation);

            if (hasFlags && hasReputation)
                validChoices.Add(choice);
        }

        return validChoices;
    }

    private void AddFlags(List<FlagSO> flags, bool logAddedFlags = false)
    {
        if (flags == null || GameProgress.Instance == null)
            return;

        foreach (FlagSO flag in flags)
        {
            if (flag == null)
                continue;

            if (logAddedFlags)
                Debug.Log($"[Dialogue] Adding flag: {flag.id}");

            GameProgress.Instance.AddFlag(flag);
        }
    }

    private void RaiseEvents(List<GameEventSO> events)
    {
        if (events == null)
            return;

        foreach (GameEventSO evt in events)
            evt?.Raise();
    }
}