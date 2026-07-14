using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MapManager : MonoBehaviour
{
    public static MapManager Instance;

    [SerializeField] private List<MapNode> nodes;
    [SerializeField] private PlayerIcon player;

    private readonly Dictionary<string, MapNode> nodeLookup = new();

    private MapNode currentNode;

    private void Awake()
    {
        Instance = this;

        foreach (var node in nodes)
            nodeLookup.Add(node.data.nodeID, node);
    }

    private void Start()
    {
        InitializeMap();
        SetPlayerToCurrentNode();

        player.OnDestinationReached += EnterNode;
    }

    private void OnDestroy()
    {
        player.OnDestinationReached -= EnterNode;
    }

    void InitializeMap()
    {
        foreach (var node in nodes)
            node.SetUnlocked(MapState.Instance.IsUnlocked(node.data.nodeID));
    }

    void SetPlayerToCurrentNode()
    {
        string id = MapState.Instance.GetCurrentNode();

        if (string.IsNullOrEmpty(id))
            id = "start";

        currentNode = nodeLookup[id];

        player.transform.position = currentNode.PathNode.Position;
    }

    public void SelectNode(MapNode destination)
    {
        if (player.IsMoving)
            return;

        if (!destination.IsUnlocked())
            return;

        List<PathNode> path =Pathfinder.Instance.FindPath(currentNode.PathNode,destination.PathNode);

        if (path.Count == 0)
            return;

        currentNode = destination;

        player.FollowPath(path);
    }

    void EnterNode()
    {
        MapState.Instance.SetCurrentNode(currentNode.data.nodeID);

        SceneManager.LoadScene(currentNode.GetScene());
    }
}