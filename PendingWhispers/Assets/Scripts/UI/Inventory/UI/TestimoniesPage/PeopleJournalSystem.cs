using System.Collections.Generic;
using UnityEngine;

public class PeopleJournalSystem : MonoBehaviour
{
    public static PeopleJournalSystem Instance;

    private List<PersonJournalEntry> entries = new();

    private HashSet<string> seenLines = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    public void AddEntry(string name, Sprite portrait, string dialogue)
    {
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(dialogue))
            return;

        string npcId = name;
        string lineKey = name + "|" + dialogue.Trim();

        if (seenLines.Contains(lineKey))
            return;

        seenLines.Add(lineKey);

        PersonJournalEntry existing = entries.Find(e => e.personName == name);

        if (existing != null)
        {
            if (existing.dialogues == null)
                existing.dialogues = new List<string>();

            if (!existing.dialogues.Contains(dialogue))
                existing.dialogues.Add(dialogue);

            existing.shortDialogue = Trim(dialogue);
            existing.fullDialogue = BuildFullDialogue(existing.dialogues);

            return;
        }

        PersonJournalEntry entry = new PersonJournalEntry
        {
            id = npcId,
            personName = name,
            portrait = portrait,
            shortDialogue = Trim(dialogue),
            dialogues = new List<string> { dialogue },
            fullDialogue = dialogue
        };

        entries.Add(entry);
    }

    private string BuildFullDialogue(List<string> dialogues)
    {
        if (dialogues == null || dialogues.Count == 0)
            return "";

        return string.Join("\n\n• ", dialogues);
    }

    private string Trim(string text)
    {
        if (string.IsNullOrEmpty(text))
            return "";

        return text.Length > 120 ? text.Substring(0, 120) + "..." : text;
    }

    public List<PersonJournalEntry> GetEntries()
    {
        return entries;
    }
}