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
    }

    
        void InitializeMap()
        {
            foreach (var node in nodes)
            {
                // nodo inicial
                if (node.data.nodeID == "Catacombs")
                {
                    node.SetUnlocked(true);
                }

                // NEW: desbloquear según GameState
                if (GameState.Instance.HasFlag("unlocked_case_1") &&
                    node.data.nodeID == "House1")
                {
                    node.SetUnlocked(true);
                }
            }
        }
    

    public void SelectNode(MapNode node)
    {
        if (!node.IsUnlocked()) return;

        currentNode = node;

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

    // NEW: método llamado por evento
    void UnlockSurface()
    {
        UnlockNode("surface"); // ID del nodo que quieres desbloquear
    }
}