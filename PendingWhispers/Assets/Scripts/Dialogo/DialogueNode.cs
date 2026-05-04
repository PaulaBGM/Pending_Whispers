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

    public List<string> requiredFlags;

    public List<string> onEnterFlags;

    public List<GameEventSO> onEnterEvents;
}