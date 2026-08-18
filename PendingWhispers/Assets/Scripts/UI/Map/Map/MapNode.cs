using UnityEngine;
using UnityEngine.UI;

public class MapNode : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private NodeData data;

    [Header("References")]
    [SerializeField] private Button button;
    [SerializeField] private Image icon;
    [SerializeField] private MapWaypoint waypoint;

    [Header("Colors")]
    [SerializeField] private Color lockedColor = Color.gray;
    [SerializeField] private Color unlockedColor = Color.white;
    [SerializeField] private Color completedColor = Color.green;

    public NodeData Data => data;
    public MapWaypoint Waypoint => waypoint;

    public bool IsUnlocked { get; private set; }
    public bool IsCompleted { get; private set; }

    private void Awake()
    {
        if (button != null)
            button.onClick.AddListener(OnNodeClicked);
    }

    private void Start()
    {
        UpdateVisual();
    }

    private void OnDestroy()
    {
        if (button != null)
            button.onClick.RemoveListener(OnNodeClicked);
    }

    private void OnNodeClicked()
    {
        if (!IsUnlocked)
            return;

        if (MapManager.Instance == null)
            return;

        if (!MapManager.Instance.CanAcceptInput)
            return;

        MapManager.Instance.TravelTo(this);
    }

    public void SetUnlocked(bool value)
    {
        IsUnlocked = value;
        UpdateVisual();
    }

    public void SetCompleted(bool value)
    {
        IsCompleted = value;
        UpdateVisual();
    }

    private void UpdateVisual()
    {
        if (icon != null)
        {
            if (!IsUnlocked)
                icon.color = lockedColor;
            else if (IsCompleted)
                icon.color = completedColor;
            else
                icon.color = unlockedColor;
        }

        if (button != null)
            button.interactable = IsUnlocked;
    }

    public string GetName()
    {
        return data != null ? data.displayName : "";
    }

    public string GetDescription()
    {
        return data != null ? data.description : "";
    }

    public string GetScene()
    {
        return data != null ? data.sceneName : "";
    }

    public string GetID()
    {
        return data != null ? data.nodeID : "";
    }
}