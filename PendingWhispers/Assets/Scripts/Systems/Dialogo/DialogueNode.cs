using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogueNode
{
    public string id;

    public string speakerID;

    [TextArea(3, 6)]
    public string text;

    public string nextNodeID;

    public List<DialogueChoice> choices;

    public List<FlagSO> requiredFlags;

    public List<FlagSO> onEnterFlags;

    public List<GameEventSO> onEnterEvents;

    public bool isImportantLine;

    [Header("Expression")]
    public DialogueExpression expression = DialogueExpression.Neutral;
}