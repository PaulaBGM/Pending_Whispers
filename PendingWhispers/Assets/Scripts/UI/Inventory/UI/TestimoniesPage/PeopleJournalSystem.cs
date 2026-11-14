using System.Collections.Generic;
using UnityEngine;
using Inventory.Model;

public class PeopleJournalSystem : BaseSingleton<PeopleJournalSystem>
{
    protected override bool PersistAcrossScenes => true;

    [SerializeField] private TestimonyEventChannelSO onTestimonyRegistered;

    private readonly List<PersonJournalEntry> entries = new();
    private readonly Dictionary<string, PersonJournalEntry> entriesByName = new();
    private readonly HashSet<string> seenLines = new();

    private void OnEnable()
    {
        if (onTestimonyRegistered == null)
        {
            return;
        }

        onTestimonyRegistered.OnRaised += AddEntry;
    }

    private void AddEntry(TestimonyEntry entry)
    {
        AddEntry(entry.Name, entry.Portrait, entry.Dialogue, entry.TestimonyItem);
    }

    private void OnDisable()
    {
        if (onTestimonyRegistered != null)
        {
            onTestimonyRegistered.OnRaised -= AddEntry;
        }
    }
    public void AddEntry(string name, Sprite portrait, string dialogue, TestimonyItemSO testimonyItem)
    {
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(dialogue))
            return;

        string npcId = name;
        string lineKey = GetLineKey(name, dialogue);

        if (!seenLines.Add(lineKey))
            return;

        if (entriesByName.TryGetValue(name, out PersonJournalEntry existing))
        {
            if (existing.dialogues == null)
                existing.dialogues = new List<string>();

            if (existing.testimonies == null)
                existing.testimonies = new List<TestimonyItemSO>();

            existing.dialogues.Add(dialogue);

            existing.shortDialogue = Trim(dialogue);
            existing.fullDialogue = BuildFullDialogue(existing.dialogues);

            existing.testimonies.Add(testimonyItem);

            // Si la primera frase llegó sin retrato, se completa con la primera que lo traiga.
            if (existing.portrait == null && portrait != null)
                existing.portrait = portrait;

            return;
        }

        PersonJournalEntry entry = new PersonJournalEntry
        {
            id = npcId,
            personName = name,
            portrait = portrait,
            shortDialogue = Trim(dialogue),
            dialogues = new List<string> { dialogue },
            testimonies = new List<TestimonyItemSO> { testimonyItem },
            fullDialogue = dialogue
        };

        entries.Add(entry);
        entriesByName[name] = entry;
    }

    public bool HasEntry(string name, string dialogue)
    {
        if (string.IsNullOrEmpty(name) || string.IsNullOrWhiteSpace(dialogue))
            return false;

        return seenLines.Contains(GetLineKey(name, dialogue));
    }

    private string GetLineKey(string name, string dialogue)
    {
        return name + "|" + dialogue.Trim();
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