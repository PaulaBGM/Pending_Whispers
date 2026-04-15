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
            // Accedemos al ID desde el ScriptableObject
            if (node.data.nodeID == "start")
            {
                node.SetUnlocked(true);
            }
        }
    }

    public void SelectNode(MapNode node)
    {
        if (!node.IsUnlocked()) return;

        currentNode = node;

        // Título dinámico
        MapUI.Instance.UpdateTitle(node.GetName());

        player.MoveTo(node.transform.position);

        Invoke(nameof(EnterNode), 0.5f);
    }

    void EnterNode()
    {
        Debug.Log("Entrando a: " + currentNode.data.nodeID);

        SceneManager.LoadScene(currentNode.GetScene());
    }

    public void UnlockNode(string nodeID)
    {
        MapNode node = nodes.Find(n => n.data.nodeID == nodeID);

        if (node != null)
        {
            node.SetUnlocked(true);
        }
    }
}