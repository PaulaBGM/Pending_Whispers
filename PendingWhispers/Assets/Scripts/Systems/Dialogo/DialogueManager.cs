using UnityEngine;
using System.Collections.Generic;
using System;
using Inventory.Model;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    private DialogueRunner runner;
    private DialogueData currentDialogue;
    private DialogueNode currentNode;

    private PlayerController_MovementInteraction player;

    void Awake()
    {
        Instance = this;
    }

    void OnEnable()
    {
        PlayerController_MovementInteraction.OnPlayerSpawned += SetPlayer;
    }

    void OnDisable()
    {
        PlayerController_MovementInteraction.OnPlayerSpawned -= SetPlayer;
    }

    void Start()
    {
        if (player == null)
            player = FindFirstObjectByType<PlayerController_MovementInteraction>();
    }

    void SetPlayer(PlayerController_MovementInteraction p)
    {
        player = p;
    }
    public void StartDialogue(DialogueData dialogue)
    {
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
        string speakerName = charData != null ? charData.displayName : "???";

        DialogueUI.Instance.ShowLine(charData, speakerName, node.text);
        RegisterDialogueToJournal(charData, node);

        if (node.choices != null && node.choices.Count > 0)
        {
            List<DialogueChoice> validChoices = new();

            foreach (var choice in node.choices)
            {
                if (GameProgress.Instance.HasAllFlags(choice.requiredFlags))
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

    void ApplyNodeEffects(DialogueNode node)
    {
        // Flags
        if (node.onEnterFlags != null)
        {
            foreach (var flag in node.onEnterFlags)
            {
                Debug.Log("[Dialogue] A�adiendo flag: " + flag.id);
                GameProgress.Instance.AddFlag(flag);
            }
        }

        // Eventos
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
                GameProgress.Instance.AddFlag(flag);
            }
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

        runner = null;
        currentNode = null;
        currentDialogue = null;
        
        if (JournalController.Instance != null) JournalController.Instance.OpenToPeopleTab();
    }
    
    void RegisterDialogueToJournal(DialogueCharacter charData, DialogueNode node)
    {
        if (charData == null || node == null)
            return;

        if (!node.isImportantLine)
            return;

        if (PeopleJournalSystem.Instance == null)
        {
            Debug.LogError("PeopleJournalSystem INSTANCE NULL");
            return;
        }

        if (InventoryRuntime.Instance == null)
        {
            Debug.LogError("InventoryRuntime INSTANCE NULL");
            return;
        }

        if (InventoryRuntime.Instance.GetInventory() == null)
        {
            Debug.LogError("InventorySO NULL");
            return;
        }

        var item = ScriptableObject.CreateInstance<TestimonyItemSO>();

        item.Name = charData.displayName;
        item.ItemImage = charData.portrait;
        item.Description = node.text;
        item.ItemType = ItemType.Testimony;

        PeopleJournalSystem.Instance.AddEntry(
            charData.displayName,
            charData.portrait,
            node.text
        );

        InventoryRuntime.Instance.GetInventory().AddItem(item, 1);
    }
}