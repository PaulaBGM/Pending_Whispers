using UnityEngine;
using System.Collections.Generic;
using UnityEngine.SceneManagement;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance;

    public List<MapNode> nodes;
    public PlayerIcon player;

    private MapNode currentNode;
    private string node;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        EventManager.Instance.Subscribe("unlock_surface_map", UnlockSurface);
    }

    private void OnDisable()
    {
        EventManager.Instance.Unsubscribe("unlock_surface_map", UnlockSurface);
    }

    private void Start()
    {
        InitializeMap();
        SetPlayerToCurrentNode();
    }


    void InitializeMap()
    {
        foreach (var node in nodes)
        {
            // Nodo inicial
            if (node.data.nodeID == "start")
            {
                MapState.Instance.UnlockNode("start");
            }

            // Desbloqueo por progreso
            if (GameState.Instance.HasFlag("unlocked_case_1"))
            {
                MapState.Instance.UnlockNode("House1");
            }

            // Aplicar estado al nodo visual
            node.SetUnlocked(MapState.Instance.IsUnlocked(node.data.nodeID));
        }
    }
    void SetPlayerToCurrentNode()
    {
        string nodeID = MapState.Instance.GetCurrentNode();

        if (string.IsNullOrEmpty(nodeID))
        {
            nodeID = "start";
        }

        MapNode node = nodes.Find(n => n.data.nodeID == nodeID);

        if (node != null)
        {
            currentNode = node;
            player.transform.position = node.transform.position;
        }
        else
        {
            Debug.LogWarning("Nodo no encontrado: " + nodeID);
        }
    }

    public void SelectNode(MapNode node)
    {
        if (!MapState.Instance.IsUnlocked(node.data.nodeID))
            return;

        currentNode = node;

        MapUI.Instance.UpdateTitle(node.GetName(), node.GetDescription());

        player.MoveTo(node.transform.position);

        Invoke(nameof(EnterNode), 0.5f);
    }

    void EnterNode()
    {
        Debug.Log("Entrando a: " + currentNode.data.nodeID);

        MapState.Instance.SetCurrentNode(currentNode.data.nodeID);

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

    void UnlockSurface()
    {
        UnlockNode("node"); 
    }
}