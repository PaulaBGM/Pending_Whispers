using System.Collections.Generic;
using UnityEngine;

public class GameState : MonoBehaviour
{
    public static GameState Instance;

    private HashSet<string> flags = new HashSet<string>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[GameState] Inicializado");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddFlag(string id)
    {
        if (!flags.Contains(id))
        {
            flags.Add(id);
            Debug.Log("[GameState] Flag añadida: " + id);
        }
        else
        {
            Debug.Log("[GameState] Flag ya existía: " + id);
        }
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