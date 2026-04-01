using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogueNode
{
    public string id;

    public string speaker;
    [TextArea] public string text;

    public List<DialogueChoice> choices;

    public string nextNodeID;

    public List<string> requiredFlags;
}