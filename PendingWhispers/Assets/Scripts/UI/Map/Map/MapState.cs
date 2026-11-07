using System.Collections.Generic;
using UnityEngine;

public class MapState : BaseSingleton<MapState>
{

    private string currentNodeID;

    private HashSet<string> unlockedNodes = new HashSet<string>();

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

    // Nodos cuyo boton ya ha aparecido en el mapa (con su animacion de pop-up).
    // Los nuevos salen con animacion la primera vez que se abre el mapa tras desbloquearlos.
    private readonly HashSet<string> revealedNodes = new HashSet<string>();

    public bool WasRevealed(string nodeID)
    {
        return !string.IsNullOrEmpty(nodeID) && revealedNodes.Contains(nodeID);
    }

    public void MarkRevealed(string nodeID)
    {
        if (!string.IsNullOrEmpty(nodeID))
            revealedNodes.Add(nodeID);
    }

    public void ClearRevealed()
    {
        revealedNodes.Clear();
    }
}