using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogueCondition
{
    public DialogueData dialogue;

    public List<string> requiredFlags;
}