using UnityEngine;
using UnityEngine.UI;

public class MapNode : MonoBehaviour
{
    public NodeData data;

    public bool isUnlocked = false;
    public bool isCompleted = false;

    public Button button;
    public Image icon;

    public Color lockedColor;
    public Color unlockedColor;
    public Color completedColor;
    [SerializeField] private PathNode pathNode;

    public PathNode PathNode => pathNode;
    private void Start()
    {
        icon.sprite = data.icon;
        UpdateVisual();
        button.onClick.AddListener(OnNodeClicked);
    }

    void OnNodeClicked()
    {
        if (!isUnlocked) return;

        MapManager.Instance.SelectNode(this);
    }

    public string GetName()
    {
        return data.displayName;
    }
    public string GetDescription()
    {
        return data.description;
    }

    public string GetScene()
    {
        return data.sceneName;
    }

    void UpdateVisual()
    {
        if (!isUnlocked)
        {
            icon.color = lockedColor;
            button.interactable = false;
        }
        else if (isCompleted)
        {
            icon.color = completedColor;
            button.interactable = true;
        }
        else
        {
            icon.color = unlockedColor;
            button.interactable = true;
        }
    }
    public void SetUnlocked(bool value)
    {
        isUnlocked = value;
        UpdateVisual();
    }

    public bool IsUnlocked()
    {
        return isUnlocked;
    }
}