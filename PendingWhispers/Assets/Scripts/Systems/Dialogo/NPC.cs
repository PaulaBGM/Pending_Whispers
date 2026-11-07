using UnityEngine;
using System.Collections.Generic;
using FMODUnity;

public class NPC : MonoBehaviour, IInteractable
{
    [Tooltip("Stable id used to scope dialogue topic seen-state across cases. Uses the GameObject name when empty.")]
    [SerializeField] private string npcId;

    [Tooltip("Nombre que sale en el selector de temas. Vacío = el displayName de su personaje en sus diálogos.")]
    [SerializeField] private string displayName;
    [Tooltip("Id del personaje del jugador en los DialogueData, para no confundirlo con el NPC.")]
    [SerializeField] private string playerCharacterId = "lyra";

    public List<DialogueCondition> dialogues;

    private readonly List<DialogueCondition> validDialoguesCache = new();

    private NPCExpressions expressions;
    private GhostTransformationController transformationController;

    // FMOD: puede ser null si el NPC no tiene emitter
    private StudioEventEmitter audioEmitter;

    private void Awake()
    {
        expressions = GetComponentInChildren<NPCExpressions>();
        transformationController = GetComponent<GhostTransformationController>();

        // Busca el emitter en el mismo GameObject
        audioEmitter = GetComponent<StudioEventEmitter>();
    }

    public string DialogueId => !string.IsNullOrWhiteSpace(npcId) ? npcId.Trim() : name;

    /// <summary>Nombre del NPC: el del Inspector o, si está vacío, el de su personaje en los diálogos.</summary>
    public string DisplayName
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(displayName))
                return displayName.Trim();

            if (dialogues == null)
                return null;

            foreach (DialogueCondition condition in dialogues)
            {
                if (condition?.dialogue?.characters == null)
                    continue;

                foreach (DialogueCharacter character in condition.dialogue.characters)
                {
                    if (character != null && !string.IsNullOrWhiteSpace(character.displayName) && character.id != playerCharacterId)
                        return character.displayName;
                }
            }

            return null;
        }
    }

    public Sprite GetExpression(DialogueExpression expression)
    {
        if (expressions == null)
            return null;

        return expressions.GetSprite(expression);
    }

    public void TryTransform(DialogueData endedDialogue)
    {
        transformationController?.TryTransform(endedDialogue);
    }

    public void Interact(PlayerController_Actions player)
    {
        List<DialogueCondition> validDialogues = GetValidDialogues();

        if (validDialogues.Count == 0)
        {
            Debug.Log("No hay dialogo valido");
            return;
        }

        // Detener audio FMOD si el NPC tiene un emitter
        audioEmitter?.Stop();

        if (validDialogues.Count == 1)
        {
            DialogueManager.Instance.StartDialogue(
                validDialogues[0].dialogue,
                this,
                validDialogues[0]
            );

            return;
        }

        DialogueManager.Instance.ShowDialogueSelector(validDialogues, this);
    }

    private List<DialogueCondition> GetValidDialogues()
    {
        validDialoguesCache.Clear();

        if (dialogues == null)
            return validDialoguesCache;

        foreach (DialogueCondition dialogueCondition in dialogues)
        {
            if (dialogueCondition?.dialogue == null)
                continue;

            bool hasFlags = GameProgress.Instance == null ||
                            GameProgress.Instance.HasAllFlags(dialogueCondition.requiredFlags);

            if (hasFlags)
                validDialoguesCache.Add(dialogueCondition);
        }

        return validDialoguesCache;
    }

    public Transform GetTransform()
    {
        return transform;
    }
}
