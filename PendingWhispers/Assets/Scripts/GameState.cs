using System.Collections.Generic;
using UnityEngine;

public class GameState : MonoBehaviour
{
    public static GameState Instance;

    private HashSet<string> flags = new HashSet<string>();

    void Awake()
    {
        Instance = this;
    }

    public void AddFlag(string id)
    {
        flags.Add(id);
    }

    public bool HasFlag(string id)
    {
        return flags.Contains(id);
    }

    public bool HasAllFlags(List<string> required)
    {
        if (required == null || required.Count == 0) return true;

        foreach (var flag in required)
        {
            if (!flags.Contains(flag))
                return false;
        }

        return true;
    }
}