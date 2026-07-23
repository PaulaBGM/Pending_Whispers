using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MapManager : BaseSingleton<MapManager>
{
    protected override bool PersistAcrossScenes => false; 

    [SerializeField] private List<MapNode> nodes;
    [SerializeField] private PlayerIcon player;
    [SerializeField] private string startNodeID = "start";

    private readonly Dictionary<string, MapNode> nodeLookup = new();
    private MapNode currentNode;

    protected override void Awake()
    {
        base.Awake();
        BuildNodeLookup();
    }

    private void Start()
    {
        InitializeMap();
        SetPlayerToCurrentNode();
        player.OnDestinationReached += EnterNode;
    }

    protected override void OnDestroy()
    {
        base.OnDestroy();
        player.OnDestinationReached -= EnterNode;
    }

    private void BuildNodeLookup()
    {
        foreach (var node in nodes)
        {
            if (node == null || node.data == null)
            {
                Debug.LogWarning("[MapManager] Hay un MapNode en la lista sin asignar o sin NodeData.", this);
                continue;
            }

            string id = node.data.nodeID;
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning($"[MapManager] El NodeData '{node.data.name}' no tiene nodeID asignado.", node);
                continue;
            }

            if (!nodeLookup.TryAdd(id, node))
            {
                Debug.LogError($"[MapManager] nodeID duplicado: '{id}'. Ya estaba asignado a '{nodeLookup[id].data.name}', " +
                                $"conflicto con '{node.data.name}'. Corrige uno de los dos NodeData.", node);
            }
        }
    }

    // Desbloqueo basado en GameProgress/FlagSO. Un nodo sin unlockFlag asignado
    // se considera siempre desbloqueado (útil para el nodo "start").
    void InitializeMap()
    {
        foreach (var node in nodes)
        {
            if (node == null || node.data == null) continue;

            bool unlocked = node.data.unlockFlag == null
                             || GameProgress.Instance.HasFlag(node.data.unlockFlag);

            node.SetUnlocked(unlocked);
        }
    }

    void SetPlayerToCurrentNode()
    {
        string id = MapState.Instance.GetCurrentNode();
        if (string.IsNullOrEmpty(id))
            id = startNodeID;

        if (!nodeLookup.TryGetValue(id, out currentNode))
        {
            Debug.LogError($"[MapManager] No existe ningún nodo con ID '{id}'. " +
                            $"Revisa que algún NodeData tenga nodeID = \"{id}\", o cambia 'Start Node ID' en el Inspector.", this);

            foreach (var kvp in nodeLookup)
            {
                currentNode = kvp.Value;
                break;
            }

            if (currentNode == null)
            {
                Debug.LogError("[MapManager] No hay ningún nodo válido en la lista. El mapa no puede inicializarse.", this);
                return;
            }
        }

        player.transform.position = currentNode.PathNode.Position;
    }

    public void SelectNode(MapNode destination)
    {
        if (player.IsMoving)
            return;
        if (destination == null || !destination.IsUnlocked())
            return;
        if (currentNode == null)
        {
            Debug.LogError("[MapManager] currentNode es null, el mapa no se inicializó correctamente.", this);
            return;
        }

        List<PathNode> path = Pathfinder.Instance.FindPath(currentNode.PathNode, destination.PathNode);
        if (path.Count == 0)
        {
            Debug.LogWarning($"[MapManager] No se encontró ruta entre '{currentNode.data.nodeID}' y '{destination.data.nodeID}'. " +
                              $"¿Están conectados sus PathNode?", this);
            return;
        }

        currentNode = destination;
        player.FollowPath(path);
    }

    void EnterNode()
    {
        MapState.Instance.SetCurrentNode(currentNode.data.nodeID);
        SceneManager.LoadScene(currentNode.GetScene());
    }
}