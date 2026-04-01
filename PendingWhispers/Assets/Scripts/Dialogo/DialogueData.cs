using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Dialogue", menuName = "Dialogue/New Dialogue")]
public class DialogueData : ScriptableObject
{
    public List<DialogueNode> nodes;

    private Dictionary<string, DialogueNode> nodeDict;

    public void Initialize()
    {
        nodeDict = new Dictionary<string, DialogueNode>();

        foreach (var node in nodes)
        {
            nodeDict[node.id] = node;
        }
    }

    public DialogueNode GetNode(string id)
    {
        if (nodeDict == null)
            Initialize();

        return nodeDict.ContainsKey(id) ? nodeDict[id] : null;
    }
}