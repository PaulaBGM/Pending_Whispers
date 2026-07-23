using System.Collections.Generic;
using UnityEngine;

public class TutorialManager : BaseSingleton<TutorialManager>
{
    private readonly HashSet<string> shownTutorials = new();
    protected override void Awake()
    {
        base.Awake();       
    }

    public bool HasSeen(string tutorialID)
    {
        return shownTutorials.Contains(tutorialID);
    }

    public void MarkSeen(string tutorialID)
    {
        shownTutorials.Add(tutorialID);
    }

    public bool TryShowTutorial(string tutorialID)
    {
        if (shownTutorials.Contains(tutorialID))
            return false;

        shownTutorials.Add(tutorialID);
        return true;
    }
}