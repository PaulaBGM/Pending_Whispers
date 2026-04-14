using UnityEngine;
using UnityEngine.UI;

public class MapNode : MonoBehaviour
{
    public string nodeID;
    public bool isUnlocked = false;
    public bool isCompleted = false;

    public Button button;
    public Image icon;

    public Color lockedColor;
    public Color unlockedColor;
    public Color completedColor;

    private void Start()
    {
        UpdateVisual();
        button.onClick.AddListener(OnNodeClicked);
    }

    public void Unlock()
    {
        isUnlocked = true;
        UpdateVisual();
    }

    public void Complete()
    {
        isCompleted = true;
        UpdateVisual();
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

    void OnNodeClicked()
    {
        if (!isUnlocked) return;

        MapManager.Instance.SelectNode(this);
    }
}