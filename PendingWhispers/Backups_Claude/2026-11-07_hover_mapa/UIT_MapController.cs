using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UIElements.Button;

/// <summary>
/// Capa de UI Toolkit encima del mapa del mundo.
///
/// El mapa sigue siendo la escena: el tilemap, los MapNode, los MapWaypoint, el MapAvatar
/// y el MapManager se quedan como están (son los que ya mueven al avatar y cargan la escena).
/// Este script solo pone por encima:
/// - Un MapButton_Template por cada MapNode, colocado sobre su posición en pantalla.
///   Al pulsarlo llama a MapManager.TravelTo.
/// - El nombre del sitio (al pasar el ratón por un nodo) y las instrucciones de la misión.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class UIT_MapController : BaseSingleton<UIT_MapController>
{
    private enum InstructionsSource { FixedText, CurrentObjective, CurrentNodeDescription, Empty }

    private const string NodeClass = "map-node";
    private const string LockedClass = "map-node--locked";
    private const string CurrentClass = "map-node--current";

    protected override bool PersistAcrossScenes => false;

    [Header("UI Toolkit")]
    [SerializeField] private UIDocument document;
    [Tooltip("MapButton_Template.uxml")]
    [SerializeField] private VisualTreeAsset nodeButtonTemplate;
    [SerializeField] private string nodeButtonName = "BT_Map";
    [SerializeField] private string nameContainerName = "VE_NameOfSiteContainer";
    [SerializeField] private string nameLabelName = "T_NameOfSite";
    [SerializeField] private string instructionsLabelName = "T_MissionInstructions";
    [Tooltip("Activado: el nombre se muestra/oculta con SetEnabled, para que el USS lo anime con :disabled. " +
             "Desactivado: se muestra/oculta con visibility.")]
    [SerializeField] private bool animateNameWithDisabled = false;

    [Header("Botones de nodo")]
    [Tooltip("Tamaño de cada botón en píxeles de panel.")]
    [SerializeField] private Vector2 buttonSize = new(120f, 120f);
    [Tooltip("Desplazamiento respecto al centro del nodo, en píxeles de panel (Y positivo = hacia abajo).")]
    [SerializeField] private Vector2 buttonOffset = Vector2.zero;
    [Tooltip("Ocultar el botón del nodo donde ya está el jugador.")]
    [SerializeField] private bool hideCurrentNodeButton = false;

    [Header("Pop-up de nodos nuevos")]
    [Tooltip("Los nodos bloqueados se ocultan con SetEnabled(false) (el USS de MapButton los pone a escala 0). " +
             "Los recién desbloqueados aparecen con la transición de :enabled la primera vez que se abre el mapa.")]
    [SerializeField] private bool popNewlyUnlockedNodes = true;
    [Tooltip("Espera tras abrir el mapa antes del pop-up, para que se vea.")]
    [SerializeField] private float popDelay = 0.4f;
    [Tooltip("Separación entre pop-ups si aparecen varios nodos a la vez.")]
    [SerializeField] private float popInterval = 0.2f;
    [Tooltip("Desactiva las imágenes/botones uGUI antiguos de cada MapNode para que no se vean dos selectores.")]
    [SerializeField] private bool hideLegacyNodeGraphics = true;

    [Header("Cámara")]
    [Tooltip("Cámara que renderiza el mapa. Vacío = Camera.main.")]
    [SerializeField] private Camera worldCamera;

    [Header("Textos")]
    [Tooltip("Texto del recuadro del nombre (el mismo que tenía el mapa uGUI).")]
    [SerializeField] private string mapTitle = "Map of the suburbs of Paris";
    [Tooltip("De dónde salen las instrucciones de la misión.")]
    [SerializeField] private InstructionsSource instructionsSource = InstructionsSource.CurrentObjective;
    [TextArea]
    [SerializeField] private string missionText = "Investigate the catacombs for more information on Vivienne and her close ones";
    [Tooltip("Al pasar el ratón por un nodo, mostrar el displayName/description de su NodeData. " +
             "Desactivado = los textos no cambian (como el mapa uGUI).")]
    [SerializeField] private bool showNodeInfoOnHover = false;
    [SerializeField] private string lockedNodeName = "???";

    private sealed class NodeView
    {
        public MapNode Node;
        public VisualElement Element;
        public Button Button;
        public Vector2 LastPosition = new(float.NaN, float.NaN);
        // False mientras el nodo espera su pop-up: el botón sigue desactivado aunque ya esté desbloqueado.
        public bool Revealed = true;
    }

    private readonly List<NodeView> views = new();

    private VisualElement root;
    private VisualElement nodeLayer;
    private VisualElement nameContainer;
    private Label nameLabel;
    private Label instructionsLabel;
    private NodeView hoveredView;

    // =========================
    // LIFECYCLE
    // =========================
    protected override void Awake()
    {
        base.Awake();

        if (Instance != this)
            return;

        if (document == null)
            document = GetComponent<UIDocument>();

        // En Awake (antes de cualquier Start): así MapManager y el avatar leen ya
        // posiciones válidas de los nodos y waypoints.
        PrepareLegacyNodes();
    }

    private void PrepareLegacyNodes()
    {
        var legacyCanvases = new HashSet<Canvas>();

        foreach (MapNode node in FindObjectsByType<MapNode>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (node == null || node.gameObject.scene != gameObject.scene)
                continue;

            if (hideLegacyNodeGraphics)
                HideLegacyGraphics(node);

            if (!node.gameObject.activeSelf)
                node.gameObject.SetActive(true);

            Canvas canvas = node.GetComponentInParent<Canvas>(true);
            if (canvas != null)
                legacyCanvases.Add(canvas.rootCanvas);
        }

        // El Canvas uGUI del mapa antiguo no debe quitarle los clics a los botones de UI Toolkit.
        foreach (Canvas canvas in legacyCanvases)
            foreach (Graphic graphic in canvas.GetComponentsInChildren<Graphic>(true))
                graphic.raycastTarget = false;
    }

    private void Start()
    {
        if (Instance != this)
            return;

        Bind();
        CreateNodeButtons();
        ShowIdleTexts();

        StartCoroutine(RevealNewNodes());
    }

    /// <summary>Pop-up de los nodos desbloqueados que aún no se habían visto en el mapa (p. ej. la mansión).</summary>
    private IEnumerator RevealNewNodes()
    {
        if (popDelay > 0f)
            yield return new WaitForSecondsRealtime(popDelay);

        foreach (NodeView view in views)
        {
            if (view.Revealed || view.Node == null || !view.Node.IsUnlocked)
                continue;

            // UpdateView lo activa en el siguiente LateUpdate: la transición del USS hace el pop.
            view.Revealed = true;
            MapState.Instance?.MarkRevealed(view.Node.GetID());

            if (popInterval > 0f)
                yield return new WaitForSecondsRealtime(popInterval);
        }
    }

    // Se recoloca cada frame: así aguanta cambios de resolución y una cámara que se mueva.
    private void LateUpdate()
    {
        if (nodeLayer?.panel == null)
            return;

        string currentID = MapState.Instance != null ? MapState.Instance.GetCurrentNode() : null;

        foreach (NodeView view in views)
            UpdateView(view, currentID);
    }

    // =========================
    // BINDING
    // =========================
    private void Bind()
    {
        root = document.rootVisualElement;

        nameContainer = root.Q<VisualElement>(nameContainerName);
        nameLabel = root.Q<Label>(nameLabelName);
        instructionsLabel = root.Q<Label>(instructionsLabelName);

        if (nameLabel == null)
            Debug.LogWarning($"[UIT_MapController] No se encontró '{nameLabelName}'.", this);
        if (instructionsLabel == null)
            Debug.LogWarning($"[UIT_MapController] No se encontró '{instructionsLabelName}'.", this);

        // Map_Root ocupa toda la pantalla: que no se trague los clics.
        root.Query<VisualElement>().ForEach(e => e.pickingMode = PickingMode.Ignore);

        nodeLayer = new VisualElement { name = "MapNodeLayer", pickingMode = PickingMode.Ignore };
        nodeLayer.style.position = Position.Absolute;
        nodeLayer.style.left = 0;
        nodeLayer.style.top = 0;
        nodeLayer.style.right = 0;
        nodeLayer.style.bottom = 0;
        root.Add(nodeLayer);
    }

    private void CreateNodeButtons()
    {
        if (nodeButtonTemplate == null)
        {
            Debug.LogError("[UIT_MapController] Falta asignar Node Button Template (MapButton_Template).", this);
            return;
        }

        foreach (MapNode node in FindObjectsByType<MapNode>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            if (node == null || node.Data == null || node.gameObject.scene != gameObject.scene)
                continue;

            TemplateContainer element = nodeButtonTemplate.CloneTree();
            element.name = $"MapNode_{node.GetID()}";
            element.style.position = Position.Absolute;
            element.style.width = buttonSize.x;
            element.style.height = buttonSize.y;

            Button button = element.Q<Button>(nodeButtonName) ?? element.Q<Button>();
            if (button == null)
            {
                Debug.LogError($"[UIT_MapController] MapButton_Template no tiene ningún botón '{nodeButtonName}'.", this);
                return;
            }

            // Solo el botón recibe el ratón; el contenedor de la plantilla no.
            element.Query<VisualElement>().ForEach(e =>
            {
                if (e is not Button)
                    e.pickingMode = PickingMode.Ignore;
            });

            button.AddToClassList(NodeClass);

            var view = new NodeView { Node = node, Element = element, Button = button };

            // Siempre abiertos (sin unlockFlag, como el HUB y las catacumbas) o ya vistos: aparecen sin animación.
            // El resto empieza desactivado (escala 0) y, si está desbloqueado, hace pop en RevealNewNodes.
            bool alwaysOpen = node.Data.unlockFlag == null;
            bool seenBefore = MapState.Instance != null && MapState.Instance.WasRevealed(node.GetID());
            view.Revealed = !popNewlyUnlockedNodes || alwaysOpen || seenBefore;
            if (!view.Revealed)
                button.SetEnabled(false);
            button.clicked += () => OnNodeClicked(view);
            button.RegisterCallback<PointerEnterEvent>(_ => OnNodeHovered(view));
            button.RegisterCallback<PointerLeaveEvent>(_ => OnNodeUnhovered(view));

            nodeLayer.Add(element);
            views.Add(view);

        }

        if (views.Count == 0)
            Debug.LogWarning("[UIT_MapController] No se encontró ningún MapNode activo en la escena.", this);
    }

    private static void HideLegacyGraphics(MapNode node)
    {
        foreach (Selectable selectable in node.GetComponentsInChildren<Selectable>(true))
            selectable.interactable = false;

        foreach (Graphic graphic in node.GetComponentsInChildren<Graphic>(true))
            graphic.enabled = false;
    }

    // =========================
    // POSITION & STATE
    // =========================
    private void UpdateView(NodeView view, string currentID)
    {
        if (view.Node == null)
            return;

        // Posición: centro del nodo en pantalla -> coordenadas del panel.
        Vector2 screen = GetScreenPoint(view.Node.transform);
        Vector2 panelPos = RuntimePanelUtils.ScreenToPanel(
            nodeLayer.panel, new Vector2(screen.x, Screen.height - screen.y));
        Vector2 local = nodeLayer.WorldToLocal(panelPos);

        Vector2 topLeft = local - buttonSize * 0.5f + buttonOffset;
        if (topLeft != view.LastPosition)
        {
            view.LastPosition = topLeft;
            view.Element.style.left = topLeft.x;
            view.Element.style.top = topLeft.y;
        }

        // Desbloqueo: lo decide MapManager (MapNode.IsUnlocked). Se muestra/oculta con SetEnabled:
        // el USS (:disabled -> escala 0) lo esconde y la transición de :enabled hace el pop-up.
        bool unlocked = view.Node.IsUnlocked;
        bool shown = unlocked && view.Revealed;
        if (view.Button.enabledSelf != shown)
            view.Button.SetEnabled(shown);
        view.Button.EnableInClassList(LockedClass, !unlocked);

        bool isCurrent = !string.IsNullOrEmpty(currentID) && view.Node.GetID() == currentID;
        view.Button.EnableInClassList(CurrentClass, isCurrent);

        DisplayStyle display = hideCurrentNodeButton && isCurrent ? DisplayStyle.None : DisplayStyle.Flex;
        if (view.Element.resolvedStyle.display != display)
            view.Element.style.display = display;
    }

    private Vector2 GetScreenPoint(Transform target)
    {
        Camera cam = worldCamera != null ? worldCamera : Camera.main;

        // Nodo dentro de un Canvas uGUI.
        if (target is RectTransform rect)
        {
            Canvas canvas = rect.GetComponentInParent<Canvas>();
            if (canvas != null)
            {
                Camera canvasCam = canvas.renderMode == RenderMode.ScreenSpaceOverlay
                    ? null
                    : (canvas.worldCamera != null ? canvas.worldCamera : cam);
                return RectTransformUtility.WorldToScreenPoint(canvasCam, rect.position);
            }
        }

        // Nodo en el mundo.
        return cam != null ? (Vector2)cam.WorldToScreenPoint(target.position) : Vector2.zero;
    }

    // =========================
    // INPUT
    // =========================
    private void OnNodeClicked(NodeView view)
    {
        MapManager manager = MapManager.Instance;

        if (manager == null)
        {
            Debug.LogError("[UIT_MapController] No hay MapManager en la escena.", this);
            return;
        }

        if (!view.Node.IsUnlocked || !view.Revealed || !manager.CanAcceptInput)
            return;

        manager.TravelTo(view.Node);
    }

    private void OnNodeHovered(NodeView view)
    {
        hoveredView = view;

        if (!showNodeInfoOnHover)
            return;

        NodeData data = view.Node.Data;
        bool unlocked = view.Node.IsUnlocked;

        ShowName(unlocked ? data.displayName : lockedNodeName);

        if (unlocked && !string.IsNullOrEmpty(data.description))
            SetInstructions(data.description);
    }

    private void OnNodeUnhovered(NodeView view)
    {
        if (hoveredView != view)
            return;

        hoveredView = null;
        ShowIdleTexts();
    }

    // =========================
    // TEXTS
    // =========================

    /// <summary>Compatibilidad con MapUI.UpdateTitle.</summary>
    public void UpdateTitle(string newTitle, string newDescription)
    {
        ShowName(newTitle);
        SetInstructions(newDescription);
    }

    private void ShowIdleTexts()
    {
        ShowName(mapTitle);

        switch (instructionsSource)
        {
            case InstructionsSource.FixedText:
                SetInstructions(missionText);
                break;
            case InstructionsSource.CurrentObjective:
                // Sin caso activo (o sin objetivos) se cae al texto fijo.
                string objective = GetCurrentObjective();
                SetInstructions(string.IsNullOrEmpty(objective) ? missionText : objective);
                break;
            case InstructionsSource.CurrentNodeDescription:
                SetInstructions(GetCurrentNodeDescription());
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

    private string GetCurrentNodeDescription()
    {
        string currentID = MapState.Instance != null ? MapState.Instance.GetCurrentNode() : null;

        foreach (NodeView view in views)
            if (view.Node != null && view.Node.GetID() == currentID)
                return view.Node.Data.description;

        return string.Empty;
    }

    private void ShowName(string text)
    {
        bool visible = !string.IsNullOrEmpty(text);

        if (nameLabel != null && visible)
            nameLabel.text = text;

        if (nameContainer == null)
            return;

        if (animateNameWithDisabled)
            nameContainer.SetEnabled(visible);
        else
            nameContainer.style.visibility = visible ? Visibility.Visible : Visibility.Hidden;
    }

    private void SetInstructions(string text)
    {
        if (instructionsLabel != null)
            instructionsLabel.text = text ?? string.Empty;
    }
}