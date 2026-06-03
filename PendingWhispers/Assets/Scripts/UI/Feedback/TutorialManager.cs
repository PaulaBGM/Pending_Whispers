using System.Collections.Generic;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    private readonly HashSet<string> shownTutorials = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
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