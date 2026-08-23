using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogueCondition
{
    public DialogueData dialogue;

    [Header("Topic Selector")]
    public string topicTitle;
    public string topicId;
    public bool hideSeenCheckmark;

    public List<FlagSO> requiredFlags;

    public string DisplayTitle => !string.IsNullOrWhiteSpace(topicTitle)
        ? topicTitle.Trim()
        : dialogue != null ? dialogue.name : "???";
}
