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
    private DialogueCondition currentDialogueTopic;
    private readonly HashSet<string> seenDialogueTopicKeys = new();
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

    public void StartDialogue(DialogueData dialogue, NPC npc, DialogueCondition dialogueTopic = null)
    {
        if (dialogue == null)
        {
            Debug.LogError("[DialogueManager] Dialogue es NULL");
            return;
        }

        currentNPC = npc;
        currentDialogueTopic = dialogueTopic;

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

        if (ShouldRegisterImportantLine(node, charData))
        {
            onTestimonyRegistered?.Raise(new TestimonyEntry(charData.displayName, charData.portrait, node.text));
        }

        var validChoices = GetValidChoices(node);

        if (validChoices.Count > 0)
            dialogueUI.ShowChoices(validChoices);
    }


    private bool ShouldRegisterImportantLine(DialogueNode node, DialogueCharacter character)
    {
        if (!node.isImportantLine || character == null)
            return false;

        return PeopleJournalSystem.Instance == null || !PeopleJournalSystem.Instance.HasEntry(character.displayName, node.text);
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
        MarkCurrentDialogueTopicAsSeen();

        SetDialogueActive(false);

        DialogueUI.Instance?.Hide();

        currentNPC?.TryTransform();

        runner = null;
        currentNode = null;
        currentDialogue = null;
        currentNPC = null;
        currentDialogueTopic = null;
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

    public void ShowDialogueSelector(List<DialogueCondition> topics, NPC npc)
    {
        if (topics == null || topics.Count == 0)
            return;

        currentNPC = npc;
        SetDialogueActive(true);

        if (!TryGetDialogueUI(out DialogueUI dialogueUI))
            return;

        dialogueUI.ShowDialogueSelector(topics, HasSeenDialogueTopic, SelectDialogueTopic);
    }

    private void SelectDialogueTopic(DialogueCondition topic)
    {
        if (topic?.dialogue == null)
            return;

        DialogueUI.Instance?.ClearChoices();
        StartDialogue(topic.dialogue, currentNPC, topic);
    }

    private bool HasSeenDialogueTopic(DialogueCondition topic)
    {
        string key = GetDialogueTopicKey(topic);
        return !string.IsNullOrEmpty(key) && seenDialogueTopicKeys.Contains(key);
    }

    private void MarkCurrentDialogueTopicAsSeen()
    {
        string key = GetDialogueTopicKey(currentDialogueTopic);

        if (!string.IsNullOrEmpty(key))
            seenDialogueTopicKeys.Add(key);
    }

    private string GetDialogueTopicKey(DialogueCondition topic)
    {
        string caseId = GetCurrentCaseId();
        string npcId = currentNPC != null ? currentNPC.DialogueId : "global";

        if (topic == null)
        {
            string dialogueId = currentDialogue != null ? currentDialogue.name : string.Empty;
            return string.IsNullOrEmpty(dialogueId) ? string.Empty : $"{caseId}|{npcId}|{dialogueId}";
        }

        string topicId = !string.IsNullOrWhiteSpace(topic.topicId)
            ? topic.topicId.Trim()
            : topic.dialogue != null ? topic.dialogue.name : string.Empty;

        return string.IsNullOrEmpty(topicId) ? string.Empty : $"{caseId}|{npcId}|{topicId}";
    }

    private string GetCurrentCaseId()
    {
        CaseData currentCaseData = CaseManager.Instance != null ? CaseManager.Instance.GetCurrentCaseData() : null;

        if (currentCaseData == null)
            return "global";

        return !string.IsNullOrWhiteSpace(currentCaseData.caseID)
            ? currentCaseData.caseID.Trim()
            : currentCaseData.name;
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