using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance;

    public List<MapNode> nodes;
    public PlayerIcon player;

    private MapNode currentNode;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        InitializeMap();
    }

    void InitializeMap()
    {
        foreach (var node in nodes)
        {
            if (node.nodeID == "start")
                node.Unlock();
        }
    }

    public void SelectNode(MapNode node)
    {
        if (!node.isUnlocked) return;

        currentNode = node;

        player.MoveTo(node.transform.position);

        Invoke(nameof(EnterNode), 0.5f);
    }

    void EnterNode()
    {
        Debug.Log("Entrando a: " + currentNode.nodeID);

        SceneManager.LoadScene(currentNode.nodeID);
    }

    public void UnlockNode(string nodeID)
    {
        MapNode node = nodes.Find(n => n.nodeID == nodeID);
        if (node != null)
            node.Unlock();
    }
}