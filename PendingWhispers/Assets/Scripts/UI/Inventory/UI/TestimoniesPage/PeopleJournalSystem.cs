using System.Collections.Generic;
using UnityEngine;

public class PeopleJournalSystem : MonoBehaviour
{
    public static PeopleJournalSystem Instance;

    private List<PersonJournalEntry> entries = new();

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
        PersonJournalEntry existing = entries.Find(e => e.personName == name);

        if (existing != null)
        {
            existing.fullDialogue += "\n" + dialogue;
            existing.shortDialogue = Trim(dialogue);
            return;
        }

        PersonJournalEntry entry = new PersonJournalEntry
        {
            personName = name,
            portrait = portrait,
            shortDialogue = Trim(dialogue),
            fullDialogue = dialogue
        };

        entries.Add(entry);
    }

    private string Trim(string text)
    {
        if (string.IsNullOrEmpty(text)) return "";

        if (text.Length > 120)
            return text.Substring(0, 120) + "...";

        return text;
    }

    public List<PersonJournalEntry> GetEntries()
    {
        return entries;
    }
}