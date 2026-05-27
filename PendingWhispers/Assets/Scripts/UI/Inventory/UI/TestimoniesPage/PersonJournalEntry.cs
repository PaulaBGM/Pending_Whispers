using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class PersonJournalEntry
{
    public string id;

    public string personName;
    public Sprite portrait;

    public string shortDialogue;
    public string fullDialogue;
    
    public List<string> dialogues = new List<string>();
}