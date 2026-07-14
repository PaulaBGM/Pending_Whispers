using UnityEngine;

public interface IJournalEntry
{
    Sprite Icon { get; }

    string Title { get; }

    string Description { get; }
}