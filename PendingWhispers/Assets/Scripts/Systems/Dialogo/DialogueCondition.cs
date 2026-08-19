using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogueCondition
{
    public DialogueData dialogue;

    [Header("Topic Selector")]
    [Tooltip("Text shown in the Phoenix Wright-style topic selector. Uses the dialogue asset name when empty.")]
    public string topicTitle;

    [Tooltip("Optional stable id used to remember whether this whole dialogue topic has already been completed.")]
    public string topicId;

    [Tooltip("Disable this only for dialogue topics that should never show the seen check mark.")]
    public bool hideSeenCheckmark;

    public List<FlagSO> requiredFlags;

    public string DisplayTitle => !string.IsNullOrWhiteSpace(topicTitle)
        ? topicTitle.Trim()
        : dialogue != null ? dialogue.name : "???";
}
