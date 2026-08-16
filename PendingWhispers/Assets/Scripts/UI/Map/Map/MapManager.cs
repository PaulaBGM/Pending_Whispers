using System.Collections.Generic;
using UnityEngine;

public class MapManager : BaseSingleton<MapManager>
{
    [Header("Map")]
    [SerializeField] private List<MapNode> nodes;

    [Header("Player")]
    [SerializeField] private MapAvatar avatar;

    [Header("Start")]
    [SerializeField] private string startNodeID = "start";

    [Header("Input")]
    [SerializeField] private float inputCooldown = 0.15f;

    protected override bool PersistAcrossScenes => false;

    private readonly Dictionary<string, MapNode> nodeLookup = new();

    private MapNode currentNode;

    private float inputUnlockTime;

    public bool CanAcceptInput =>
        Time.unscaledTime >= inputUnlockTime;

    protected override void Awake()
    {
        base.Awake();

        if (Instance != this)
            return;

        BuildNodeLookup();
    }

    private void Start()
    {
        if (Instance != this)
            return;

        inputUnlockTime =
            Time.unscaledTime + inputCooldown;

        InitializeMap();

        SetAvatarToCurrentNode();

        if (avatar != null)
            avatar.OnDestinationReached += EnterNode;
    }

    protected override void OnDestroy()
    {
        if (avatar != null)
            avatar.OnDestinationReached -= EnterNode;

        base.OnDestroy();
    }

    // =========================================================
    // INITIALIZATION
    // =========================================================

    private void BuildNodeLookup()
    {
        nodeLookup.Clear();

        foreach (MapNode node in nodes)
        {
            if (node == null)
                continue;

            if (node.Data == null)
            {
                Debug.LogWarning(
                    $"[MapManager] {node.name} no tiene NodeData.",
                    node
                );

                continue;
            }

            string id = node.Data.nodeID;

            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning(
                    $"[MapManager] {node.name} no tiene nodeID.",
                    node
                );

                continue;
            }

            if (!nodeLookup.TryAdd(id, node))
            {
                Debug.LogError(
                    $"[MapManager] nodeID duplicado: {id}",
                    node
                );
            }
        }
    }

    private void InitializeMap()
    {
        if (GameProgress.Instance == null)
        {
            Debug.LogError("[MapManager] GameProgress.Instance es NULL.");
            return;
        }

        foreach (MapNode node in nodes)
        {
            if (node == null || node.Data == null)
                continue;

            FlagSO unlockFlag = node.Data.unlockFlag;

            bool unlocked =
                unlockFlag == null ||
                GameProgress.Instance.HasFlag(unlockFlag);

            node.SetUnlocked(unlocked);
        }
    }

    // =========================================================
    // CURRENT NODE
    // =========================================================

    private void SetAvatarToCurrentNode()
    {
        if (MapState.Instance == null)
        {
            Debug.LogError(
                "[MapManager] MapState.Instance es NULL."
            );

            return;
        }

        string currentID =
            MapState.Instance.GetCurrentNode();

        if (string.IsNullOrEmpty(currentID))
            currentID = startNodeID;

        if (!nodeLookup.TryGetValue(currentID, out currentNode))
        {
            Debug.LogWarning(
                $"[MapManager] No existe el nodo '{currentID}'. " +
                $"Se utilizará '{startNodeID}'."
            );

            if (!nodeLookup.TryGetValue(
                    startNodeID,
                    out currentNode))
            {
                Debug.LogError(
                    "[MapManager] Tampoco existe el nodo inicial."
                );

                return;
            }
        }

        if (avatar == null)
        {
            Debug.LogError(
                "[MapManager] Avatar no asignado."
            );

            return;
        }

        avatar.SetPosition(currentNode.Waypoint);
    }

    // =========================================================
    // TRAVEL
    // =========================================================

    public void TravelTo(MapNode destination)
    {
        if (avatar == null)
            return;

        if (avatar.IsMoving)
            return;

        if (destination == null)
            return;

        if (!destination.IsUnlocked)
            return;

        if (currentNode == null)
            return;

        if (destination == currentNode)
            return;

        if (destination.Waypoint == null)
        {
            Debug.LogError(
                $"[MapManager] '{destination.name}' " +
                "no tiene Waypoint.",
                destination
            );

            return;
        }

        List<MapWaypoint> path =
            MapRoute.FindPath(
                currentNode.Waypoint,
                destination.Waypoint
            );

        if (path == null || path.Count == 0)
        {
            Debug.LogWarning(
                $"[MapManager] No existe ruta entre " +
                $"'{currentNode.GetID()}' y " +
                $"'{destination.GetID()}'.",
                this
            );

            return;
        }

        Debug.Log(
            $"[MapManager] Viajando de " +
            $"{currentNode.GetID()} -> " +
            $"{destination.GetID()}"
        );

        // Guardamos el destino inmediatamente.
        currentNode = destination;

        // Bloqueamos nuevos clicks durante el movimiento.
        inputUnlockTime =
            Time.unscaledTime + inputCooldown;

        avatar.FollowPath(path);
    }

    // =========================================================
    // ARRIVAL
    // =========================================================

    private void EnterNode()
    {
        if (currentNode == null)
            return;

        if (MapState.Instance == null)
        {
            Debug.LogError(
                "[MapManager] MapState.Instance es NULL."
            );

            return;
        }

        string nodeID =
            currentNode.GetID();

        Debug.Log(
            $"[MapManager] Llegamos a: {nodeID}"
        );

        MapState.Instance.SetCurrentNode(nodeID);

        string sceneName =
            currentNode.GetScene();

        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError(
                $"[MapManager] El nodo '{nodeID}' " +
                "no tiene sceneName."
            );

            return;
        }

        if (SceneController.Instance == null)
        {
            Debug.LogError(
                "[MapManager] SceneController.Instance es NULL."
            );

            return;
        }

        SceneController.Instance.LoadScene(sceneName);
    }
}