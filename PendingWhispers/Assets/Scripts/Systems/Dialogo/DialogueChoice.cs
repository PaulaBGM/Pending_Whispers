using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogueChoice
{
    public string text;

    public string nextNodeID;

    public List<FlagSO> addFlags;
    public List<FlagSO> requiredFlags;

    [Header("Events")]
    public GameEventSO onSelectedEvent;

    public bool endsDialogue;
}