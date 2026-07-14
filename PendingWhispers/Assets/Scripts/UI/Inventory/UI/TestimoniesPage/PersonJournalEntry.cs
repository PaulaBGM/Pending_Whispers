using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PersonJournalEntry : IJournalEntry
{
    public string id;

    public string personName;

    public Sprite portrait;

    public string shortDialogue;

    public string fullDialogue;

    public List<string> dialogues;

    public Sprite Icon => portrait;

    public string Title => personName;

    public string Description => fullDialogue;
}