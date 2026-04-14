using UnityEngine;
using System.Collections.Generic;

public class MapProgress : MonoBehaviour
{
    public static MapProgress Instance;

    public List<string> unlockedNodes = new List<string>();
    public List<string> completedNodes = new List<string>();

    private void Awake()
    {
        Instance = this;
    }

    public void UnlockNode(string id)
    {
        if (!unlockedNodes.Contains(id))
            unlockedNodes.Add(id);
    }

    public bool IsUnlocked(string id)
    {
        return unlockedNodes.Contains(id);
    }
}