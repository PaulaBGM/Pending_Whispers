using UnityEngine;
using System.Collections.Generic;

public class MapManager : BaseSingleton<MapManager>
{
    protected override bool PersistAcrossScenes => false;

    public List<MapNode> nodes;
    public PlayerIcon player;

    private MapNode currentNode;

    private void OnEnable()
    {
        if (GameProgress.Instance != null)
            GameProgress.Instance.OnFlagAdded += OnFlagAdded;
    }

    private void OnDisable()
    {
        if (GameProgress.Instance != null)
            GameProgress.Instance.OnFlagAdded -= OnFlagAdded;
    }

    private void Start()
    {
        InitializeMap();
        SetPlayerToCurrentNode();
        TutorialPopup.Instance.ShowTutorialOnce("map", "Map", "Select a location to travel.\n\nNew places will be unlocked as the investigation progresses.");
    }

    void InitializeMap()
    {
        foreach (var node in nodes)
        {
            // Nodo inicial
            if (node.data.nodeID == "Start")
                MapState.Instance.UnlockNode("Start");

            // Flags existentes
            if (node.data.unlockFlag != null &&
                GameProgress.Instance.HasFlag(node.data.unlockFlag))
            {
                MapState.Instance.UnlockNode(node.data.nodeID);
            }

            node.SetUnlocked(MapState.Instance.IsUnlocked(node.data.nodeID));
        }
    }

    void OnFlagAdded(FlagSO flag)
    {
        foreach (var node in nodes)
        {
            if (node.data.unlockFlag == flag)
            {
                UnlockNodeRuntime(node);
            }
        }
    }

    void UnlockNodeRuntime(MapNode node)
    {
        Debug.Log("[Map] Desbloqueado: " + node.data.nodeID);

        MapState.Instance.UnlockNode(node.data.nodeID);
        node.SetUnlocked(true);

        UIGameEvents.RaiseLocationUnlocked(node.GetName());
    }

    void SetPlayerToCurrentNode()
    {
        string nodeID = MapState.Instance.GetCurrentNode();

        if (string.IsNullOrEmpty(nodeID))
            nodeID = "Start";

        MapNode node = nodes.Find(n => n.data.nodeID == nodeID);

        if (node != null)
        {
            currentNode = node;
            player.transform.position = node.transform.position;
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
        MapState.Instance.SetCurrentNode(currentNode.data.nodeID);
        SceneController.Instance.LoadScene(currentNode.GetScene());
    }
}