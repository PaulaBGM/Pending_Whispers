using UnityEngine;
using UnityEngine.UI;

public class MapNode : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private NodeData data;

    [Header("Waypoint")]
    [SerializeField] private MapWaypoint waypoint;

    [Header("UI")]
    [SerializeField] private Button button;
    [SerializeField] private Image icon;

    [Header("State")]
    [SerializeField] private bool isUnlocked;
    [SerializeField] private bool isCompleted;

    [Header("Colors")]
    [SerializeField] private Color lockedColor;
    [SerializeField] private Color unlockedColor;
    [SerializeField] private Color completedColor;

    public NodeData Data => data;

    public MapWaypoint Waypoint => waypoint;

    public bool IsUnlocked => isUnlocked;

    public bool IsCompleted => isCompleted;

    private void Start()
    {
        if (data == null)
        {
            Debug.LogError(
                $"[MapNode] {name} no tiene NodeData asignado.",
                this
            );
            return;
        }

        if (button == null)
        {
            Debug.LogError(
                $"[MapNode] {name} no tiene Button asignado.",
                this
            );
            return;
        }

        if (waypoint == null)
        {
            Debug.LogError(
                $"[MapNode] {name} no tiene MapWaypoint asignado.",
                this
            );
            return;
        }

        if (icon != null)
            icon.sprite = data.icon;

        UpdateVisual();

        button.onClick.AddListener(OnNodeClicked);
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnNodeClicked);
    }

    // =========================================================
    // CLICK
    // =========================================================

    private void OnNodeClicked()
    {
        Debug.Log(
            $"[MapNode] Click en {GetID()} | Unlocked: {isUnlocked}",
            this
        );

        if (!isUnlocked)
        {
            Debug.LogWarning(
                $"[MapNode] {GetID()} está bloqueado.",
                this
            );
            return;
        }

        Debug.Log(
            $"[MapNode] MapManager.Instance = {MapManager.Instance}",
            this
        );

        if (MapManager.Instance == null)
        {
            Debug.LogError(
                "[MapNode] MapManager.Instance es NULL.",
                this
            );
            return;
        }

        Debug.Log($"[MapNode] CanAcceptInput = {MapManager.Instance.CanAcceptInput}",this);

        if (!MapManager.Instance.CanAcceptInput)
        {
            Debug.LogWarning("[MapNode] MapManager no acepta input todavía.", this);
            return;
        }
        Debug.Log($"[MapNode] Llamando a TravelTo({GetID()})",this);
        MapManager.Instance.TravelTo(this);
    }

    // =========================================================
    // VISUAL
    // =========================================================

    private void UpdateVisual()
    {
        if (button == null)
            return;

        if (!isUnlocked)
        {
            if (icon != null)
                icon.color = lockedColor;

            button.interactable = false;
        }
        else if (isCompleted)
        {
            if (icon != null)
                icon.color = completedColor;

            button.interactable = true;
        }
        else
        {
            if (icon != null)
                icon.color = unlockedColor;

            button.interactable = true;
        }
    }

    // =========================================================
    // STATE
    // =========================================================

    public void SetUnlocked(bool value)
    {
        isUnlocked = value;
        UpdateVisual();
    }

    public void SetCompleted(bool value)
    {
        isCompleted = value;
        UpdateVisual();
    }

    // =========================================================
    // DATA
    // =========================================================

    public string GetID()
    {
        return data != null
            ? data.nodeID
            : string.Empty;
    }

    public string GetName()
    {
        return data != null
            ? data.displayName
            : string.Empty;
    }

    public string GetDescription()
    {
        return data != null
            ? data.description
            : string.Empty;
    }

    public string GetScene()
    {
        return data != null
            ? data.sceneName
            : string.Empty;
    }
}