using System.Collections.Generic;
using UnityEngine;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance;

    private readonly HashSet<TutorialID> shownTutorials = new();

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

    public bool HasSeen(TutorialID id)
    {
        return shownTutorials.Contains(id);
    }

    public void ShowTutorial(
        TutorialID id,
        string title,
        string description)
    {
        if (HasSeen(id))
            return;

        shownTutorials.Add(id);

        TutorialPopup.Instance.ShowTutorial(
            title,
            description
        );
    }
}