using System.Collections.Generic;
using UnityEngine;

public class MapState : MonoBehaviour
{
    public static MapState Instance;

    private string currentNodeID;

    private HashSet<string> unlockedNodes = new HashSet<string>();

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            Debug.Log("[MapState] Inicializado");
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void SetCurrentNode(string nodeID)
    {
        currentNodeID = nodeID;
        Debug.Log("[MapState] Nodo actual: " + nodeID);
    }

    public string GetCurrentNode()
    {
        return currentNodeID;
    }

    public void UnlockNode(string nodeID)
    {
        if (!unlockedNodes.Contains(nodeID))
        {
            unlockedNodes.Add(nodeID);
            Debug.Log("[MapState] Nodo desbloqueado: " + nodeID);
        }
    }

    public bool IsUnlocked(string nodeID)
    {
        return unlockedNodes.Contains(nodeID);
    }
}