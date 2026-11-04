using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Mapa en UI Toolkit. Sustituye a MapManager + MapNode + MapWaypoint + MapUI.
///
/// Reparto de trabajo:
/// - En UI Builder se colocan los nodos (instancias de MapButton_Template), los waypoints
///   (VisualElements vacíos) y el avatar, todos con position: absolute dentro de VE_MapArea.
/// - Aquí, en el Inspector, se dice qué NodeData va en cada botón y qué waypoints están conectados.
///
/// La lógica es la misma que MapManager: desbloqueo por FlagSO, ruta en anchura,
/// MapState guarda el nodo actual y al llegar se carga la escena del nodo.
/// </summary>
public class UIT_MapController : BaseSingleton<UIT_MapController>
{
    [Serializable]
    private class NodeBinding
    {
        public NodeData data;
        [Tooltip("Nombre del botón (o de la instancia de MapButton_Template) en el UXML, p. ej. Node_start.")]
        public string buttonName;
        [Tooltip("Waypoint donde se coloca el avatar. Vacío = el centro del propio botón.")]
        public string waypointName;
        public bool completed;

        [NonSerialized] public Button button;
        [NonSerialized] public bool unlocked;

        public string ID => data != null ? data.nodeID : string.Empty;
        public string PositionKey => string.IsNullOrEmpty(waypointName) ? buttonName : waypointName;
    }

    [Serializable]
    private class WaypointLink
    {
        [Tooltip("Nombre de un waypoint o de un botón de nodo.")]
        public string from;
        public List<string> to = new();
    }

    private enum IdleInstructions { CurrentObjective, CurrentNodeDescription, Empty }

    private const string LockedClass = "map-node--locked";
    private const string CompletedClass = "map-node--completed";
    private const string CurrentClass = "map-node--current";

    protected override bool PersistAcrossScenes => false;

    [Header("UI Toolkit")]
    [SerializeField] private UIDocument document;
    [SerializeField] private string mapAreaName = "VE_MapArea";
    [SerializeField] private string avatarName = "VE_MapAvatar";
    [Tooltip("Contenedor con la clase .nameOfSite: se desactiva/activa para animar el nombre.")]
    [SerializeField] private string nameContainerName = "VE_UpSection_Left";
    [SerializeField] private string nameLabelName = "T_NameOfSite";
    [SerializeField] private string instructionsLabelName = "T_MissionInstructions";

    [Header("Nodos")]
    [SerializeField] private List<NodeBinding> nodes = new();
    [SerializeField] private string startNodeID = "start";
    [Tooltip("Usar NodeData.icon como imagen del botón (si tiene).")]
    [SerializeField] private bool useNodeIcons = false;

    [Header("Caminos")]
    [SerializeField] private List<WaypointLink> links = new();
    [Tooltip("Si A conecta con B, B conecta con A sin tener que escribirlo.")]
    [SerializeField] private bool bidirectionalLinks = true;

    [Header("Avatar")]
    [SerializeField] private UIT_MapAvatar avatar;

    [Header("Textos")]
    [SerializeField] private IdleInstructions idleInstructions = IdleInstructions.CurrentObjective;
    [Tooltip("Al pasar por un nodo, mostrar su descripción abajo.")]
    [SerializeField] private bool showDescriptionOnHover = true;
    [SerializeField] private string lockedNodeName = "???";

    [Header("Input")]
    [SerializeField] private float inputCooldown = 0.15f;

    public bool CanAcceptInput => Time.unscaledTime >= inputUnlockTime;

    private readonly Dictionary<string, NodeBinding> nodeLookup = new();
    private readonly Dictionary<string, VisualElement> positionElements = new();
    private readonly Dictionary<string, HashSet<string>> graph = new();

    private VisualElement root;
    private VisualElement mapArea;
    private VisualElement nameContainer;
    private Label nameLabel;
    private Label instructionsLabel;

    private NodeBinding currentNode;
    private NodeBinding hoveredNode;
    private float inputUnlockTime;
    private bool placedAvatar;

    // =========================
    // LIFECYCLE
    // =========================
    private void Start()
    {
        if (Instance != this)
            return;

        inputUnlockTime = Time.unscaledTime + inputCooldown;

        if (!Bind())
            return;

        BuildGraph();
        InitializeNodes();
        ResolveCurrentNode();
        ShowIdleTexts();

        // Las posiciones de los waypoints solo existen cuando el layout está calculado.
        mapArea.RegisterCallback<GeometryChangedEvent>(OnMapGeometryChanged);
    }

    private void OnMapGeometryChanged(GeometryChangedEvent evt)
    {
        if (placedAvatar || avatar == null || currentNode == null)
            return;

        placedAvatar = true;
        avatar.SetPosition(GetPositionElement(currentNode));
    }

    // =========================
    // BINDING
    // =========================
    private bool Bind()
    {
        if (document == null)
        {
            Debug.LogError("[UIT_MapController] Falta el UIDocument.", this);
            return false;
        }

        root = document.rootVisualElement;

        // Los textos se enlazan siempre, aunque falte el mapa: así la capa de arriba funciona sola.
        nameContainer = root.Q<VisualElement>(nameContainerName);
        nameLabel = root.Q<Label>(nameLabelName);
        instructionsLabel = root.Q<Label>(instructionsLabelName);

        mapArea = root.Q<VisualElement>(mapAreaName);
        if (mapArea == null)
        {
            Debug.LogError($"[UIT_MapController] No se encontró '{mapAreaName}' en el UXML. " +
                           "Falta añadir la zona del mapa (fondo, nodos, waypoints y avatar). " +
                           "Mientras tanto solo funcionan los textos.", this);
            HideName();
            SetInstructions(GetCurrentObjective());
            return false;
        }

        if (avatar != null)
        {
            var avatarElement = root.Q<VisualElement>(avatarName);
            if (avatarElement == null)
                Debug.LogError($"[UIT_MapController] No se encontró el avatar '{avatarName}'.", this);
            avatar.Bind(avatarElement);
        }
        else
        {
            Debug.LogError("[UIT_MapController] Falta asignar el UIT_MapAvatar.", this);
        }

        nodeLookup.Clear();
        foreach (var node in nodes)
            BindNode(node);

        return true;
    }

    private void BindNode(NodeBinding node)
    {
        if (node.data == null)
        {
            Debug.LogWarning($"[UIT_MapController] El nodo '{node.buttonName}' no tiene NodeData.", this);
            return;
        }

        if (string.IsNullOrEmpty(node.ID))
        {
            Debug.LogWarning($"[UIT_MapController] El NodeData '{node.data.name}' no tiene nodeID.", this);
            return;
        }

        if (!nodeLookup.TryAdd(node.ID, node))
        {
            Debug.LogError($"[UIT_MapController] nodeID duplicado: {node.ID}", this);
            return;
        }

        // Puede ser el botón o la instancia de MapButton_Template que lo contiene.
        var element = root.Q<VisualElement>(node.buttonName);
        node.button = element as Button ?? element?.Q<Button>();
        if (node.button == null)
        {
            Debug.LogError($"[UIT_MapController] No se encontró el botón '{node.buttonName}' ({node.ID}).", this);
            return;
        }

        RegisterPositionElement(node.buttonName, element);
        if (!string.IsNullOrEmpty(node.waypointName))
            RegisterPositionElement(node.waypointName, root.Q<VisualElement>(node.waypointName));

        if (useNodeIcons && node.data.icon != null)
            node.button.style.backgroundImage = new StyleBackground(node.data.icon);

        NodeBinding captured = node;
        node.button.clicked += () => OnNodeClicked(captured);
        node.button.RegisterCallback<PointerEnterEvent>(_ => OnNodeHovered(captured));
        node.button.RegisterCallback<PointerLeaveEvent>(_ => OnNodeUnhovered(captured));
    }

    private void RegisterPositionElement(string elementName, VisualElement element)
    {
        if (string.IsNullOrEmpty(elementName))
            return;

        if (element == null)
        {
            Debug.LogError($"[UIT_MapController] No se encontró '{elementName}' en el UXML.", this);
            return;
        }

        positionElements[elementName] = element;
    }

    // =========================
    // GRAPH
    // =========================
    private void BuildGraph()
    {
        graph.Clear();

        foreach (var link in links)
        {
            if (string.IsNullOrEmpty(link.from))
                continue;

            EnsurePositionElement(link.from);

            foreach (string to in link.to)
            {
                if (string.IsNullOrEmpty(to))
                    continue;

                EnsurePositionElement(to);
                AddEdge(link.from, to);
                if (bidirectionalLinks)
                    AddEdge(to, link.from);
            }
        }
    }

    private void EnsurePositionElement(string elementName)
    {
        if (!positionElements.ContainsKey(elementName))
            RegisterPositionElement(elementName, root.Q<VisualElement>(elementName));
    }

    private void AddEdge(string from, string to)
    {
        if (!graph.TryGetValue(from, out var set))
            graph[from] = set = new HashSet<string>();
        set.Add(to);
    }

    // =========================
    // STATE
    // =========================
    private void InitializeNodes()
    {
        foreach (var node in nodeLookup.Values)
        {
            FlagSO unlockFlag = node.data.unlockFlag;
            node.unlocked = unlockFlag == null ||
                            (GameProgress.Instance != null && GameProgress.Instance.HasFlag(unlockFlag));
            RefreshNodeVisual(node);
        }
    }

    private void ResolveCurrentNode()
    {
        string currentID = MapState.Instance != null ? MapState.Instance.GetCurrentNode() : null;
        if (string.IsNullOrEmpty(currentID))
            currentID = startNodeID;

        if (!nodeLookup.TryGetValue(currentID, out currentNode) &&
            !nodeLookup.TryGetValue(startNodeID, out currentNode))
        {
            Debug.LogError($"[UIT_MapController] No existe el nodo '{currentID}' ni el inicial '{startNodeID}'.", this);
            return;
        }

        RefreshAllNodeVisuals();
    }

    private void RefreshAllNodeVisuals()
    {
        foreach (var node in nodeLookup.Values)
            RefreshNodeVisual(node);
    }

    private void RefreshNodeVisual(NodeBinding node)
    {
        if (node.button == null)
            return;

        // :disabled en el USS = nodo bloqueado (y no recibe clics).
        node.button.SetEnabled(node.unlocked);
        node.button.EnableInClassList(LockedClass, !node.unlocked);
        node.button.EnableInClassList(CompletedClass, node.unlocked && node.completed);
        node.button.EnableInClassList(CurrentClass, node == currentNode);
    }

    public void SetCompleted(string nodeID, bool completed)
    {
        if (!nodeLookup.TryGetValue(nodeID, out var node))
            return;

        node.completed = completed;
        RefreshNodeVisual(node);
    }

    // =========================
    // INPUT
    // =========================
    private void OnNodeClicked(NodeBinding destination)
    {
        if (!destination.unlocked || !CanAcceptInput)
            return;

        TravelTo(destination);
    }

    private void OnNodeHovered(NodeBinding node)
    {
        hoveredNode = node;
        ShowName(node.unlocked ? node.data.displayName : lockedNodeName);

        if (showDescriptionOnHover && node.unlocked && !string.IsNullOrEmpty(node.data.description))
            SetInstructions(node.data.description);
    }

    private void OnNodeUnhovered(NodeBinding node)
    {
        if (hoveredNode != node)
            return;

        hoveredNode = null;
        ShowIdleTexts();
    }

    // =========================
    // TRAVEL
    // =========================
    private void TravelTo(NodeBinding destination)
    {
        if (avatar == null || avatar.IsMoving || currentNode == null || destination == currentNode)
            return;

        List<string> route = UIT_MapRoute.FindPath(graph, currentNode.PositionKey, destination.PositionKey);
        if (route.Count == 0)
        {
            Debug.LogWarning($"[UIT_MapController] No hay ruta entre '{currentNode.ID}' y '{destination.ID}'. " +
                             "Revisa la lista Links.", this);
            return;
        }

        var path = new List<VisualElement>(route.Count);
        foreach (string key in route)
        {
            if (positionElements.TryGetValue(key, out var element))
                path.Add(element);
        }

        Debug.Log($"[UIT_MapController] Viajando de {currentNode.ID} -> {destination.ID}");

        currentNode = destination;
        inputUnlockTime = Time.unscaledTime + inputCooldown;
        RefreshAllNodeVisuals();

        avatar.FollowPath(path, EnterCurrentNode);
    }

    private void EnterCurrentNode()
    {
        if (currentNode == null)
            return;

        string nodeID = currentNode.ID;
        Debug.Log($"[UIT_MapController] Llegamos a: {nodeID}");

        MapState.Instance?.SetCurrentNode(nodeID);

        string sceneName = currentNode.data.sceneName;
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogError($"[UIT_MapController] El nodo '{nodeID}' no tiene sceneName.", this);
            return;
        }

        if (SceneController.Instance == null)
        {
            Debug.LogError("[UIT_MapController] SceneController.Instance es NULL.", this);
            return;
        }

        SceneController.Instance.LoadScene(sceneName);
    }

    // =========================
    // TEXTS (antes MapUI)
    // =========================

    /// <summary>Compatibilidad con MapUI.UpdateTitle.</summary>
    public void UpdateTitle(string newTitle, string newDescription)
    {
        ShowName(newTitle);
        SetInstructions(newDescription);
    }

    private void ShowIdleTexts()
    {
        HideName();

        switch (idleInstructions)
        {
            case IdleInstructions.CurrentObjective:
                SetInstructions(GetCurrentObjective());
                break;
            case IdleInstructions.CurrentNodeDescription:
                SetInstructions(currentNode != null ? currentNode.data.description : string.Empty);
                break;
            default:
                SetInstructions(string.Empty);
                break;
        }
    }

    private static string GetCurrentObjective()
    {
        CaseRuntime currentCase = CaseManager.Instance != null ? CaseManager.Instance.GetCurrentCase() : null;
        if (currentCase?.data?.objectives == null || currentCase.data.objectives.Count == 0)
            return string.Empty;

        return currentCase.GetCurrentObjective();
    }

    private void ShowName(string text)
    {
        if (nameLabel != null)
            nameLabel.text = text ?? string.Empty;

        // Vuestro USS anima .nameOfSite:disabled (entra/sale deslizándose).
        nameContainer?.SetEnabled(!string.IsNullOrEmpty(text));
    }

    private void HideName()
    {
        nameContainer?.SetEnabled(false);
    }

    private void SetInstructions(string text)
    {
        if (instructionsLabel != null)
            instructionsLabel.text = text ?? string.Empty;
    }

    private VisualElement GetPositionElement(NodeBinding node)
    {
        positionElements.TryGetValue(node.PositionKey, out var element);
        return element;
    }
}