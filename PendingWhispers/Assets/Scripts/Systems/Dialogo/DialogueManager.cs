using UnityEngine;
using System.Collections.Generic;
using Inventory.Model;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    private DialogueRunner runner;
    private DialogueData currentDialogue;
    private DialogueNode currentNode;
    private NPC currentNPC;

    private PlayerController_Actions player;

    void Awake()
    {
        Instance = this;
    }

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

        currentDialogue = dialogue;
        runner = new DialogueRunner(dialogue);

        if (player != null)
            player.canMove = false;

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

        if (DialogueUI.Instance == null)
        {
            Debug.LogError("[DialogueManager] DialogueUI no existe en escena");
            return;
        }

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

        DialogueUI.Instance.ShowLine(
            charData,
            speakerName,
            node.text,
            expressionSprite
        );

        RegisterDialogueToJournal(charData, node);

        if (node.choices != null && node.choices.Count > 0)
        {
            List<DialogueChoice> validChoices = new();

            foreach (var choice in node.choices)
            {
                bool hasFlags =
                    GameProgress.Instance.HasAllFlags(choice.requiredFlags);

                bool hasReputation =
                    ReputationManager.Instance == null ||
                    ReputationManager.Instance.HasReputation(choice.requiredReputation);

                if (hasFlags && hasReputation)
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
    }

    void ApplyNodeEffects(DialogueNode node)
    {
        if (node.onEnterFlags != null)
        {
            foreach (var flag in node.onEnterFlags)
            {
                if (flag == null)
                    continue;

                Debug.Log("[Dialogue] Adding flag: " + flag.id);

                GameProgress.Instance.AddFlag(flag);
            }
        }

        if (node.onEnterEvents != null)
        {
            foreach (var evt in node.onEnterEvents)
            {
                evt?.Raise();
            }
        }
    }

    public void ChooseChoice(DialogueChoice choice)
    {
        if (choice == null)
        {
            Debug.LogError("[DialogueManager] Choice NULL");
            return;
        }

        if (choice.addFlags != null)
        {
            foreach (var flag in choice.addFlags)
            {
                if (flag == null)
                    continue;

                GameProgress.Instance.AddFlag(flag);
            }
        }

        if (choice.reputationChange != 0)
        {
            ReputationManager.Instance?.AddReputation(choice.reputationChange);
        }

        if (choice.onSelectedEvent != null)
        {
            choice.onSelectedEvent.Raise();
        }

        if (choice.endsDialogue)
        {
            EndDialogue();
            return;
        }

        DialogueUI.Instance.ClearChoices();

        GoToNode(choice.nextNodeID);
    }

    public void EndDialogue()
    {
        if (DialogueUI.Instance != null)
            DialogueUI.Instance.Hide();

        if (player != null)
            player.canMove = true;
        
        if (currentNPC != null)
        {
            currentNPC.TryTransform();
        }
        runner = null;
        currentNode = null;
        currentDialogue = null;
        currentNPC = null;

    }

    void RegisterDialogueToJournal(
        DialogueCharacter charData,
        DialogueNode node)
    {
        if (charData == null || node == null)
            return;

        if (!node.isImportantLine)
            return;

        var item = ScriptableObject.CreateInstance<TestimonyItemSO>();

        item.Name = charData.displayName;
        item.ItemImage = charData.portrait;
        item.Description = node.text;
        item.ItemType = ItemType.Testimony;

        PeopleJournalSystem.Instance.AddEntry(charData.displayName,charData.portrait,node.text);
        FindFirstObjectByType<HUDController>()?.AddClueNotification();

    }
}