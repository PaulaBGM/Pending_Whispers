using UnityEngine;

[CreateAssetMenu(fileName = "NewNode", menuName = "Map/Node Data")]
public class NodeData : ScriptableObject
{
    public string nodeID;
    public string displayName;
    public string sceneName;

    public Sprite icon;

    [TextArea]
    public string description;
}