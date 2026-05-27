using UnityEngine;

public class DialogueRunner
{
    private DialogueData data;
    private DialogueNode currentNode;

    public DialogueNode CurrentNode => currentNode;

    public DialogueRunner(DialogueData dialogue)
    {
        data = dialogue;

        if (data == null)
        {
            Debug.LogError("[DialogueRunner] DialogueData es NULL");
            return;
        }

        data.Initialize();
    }

    // -------------------------
    // START
    // -------------------------
    public DialogueNode Start()
    {
        currentNode = data.GetNode("start");

        if (currentNode == null)
        {
            Debug.LogError("[DialogueRunner] Nodo 'start' no encontrado");
        }

        return currentNode;
    }

    // -------------------------
    // NEXT (por ID)
    // -------------------------
    public DialogueNode Next(string nodeID)
    {
        if (string.IsNullOrEmpty(nodeID))
        {
            Debug.LogError("[DialogueRunner] nodeID vacío");
            return null;
        }

        var node = data.GetNode(nodeID);

        if (node == null)
        {
            Debug.LogError("[DialogueRunner] Nodo no encontrado: " + nodeID);
            return null;
        }

        currentNode = node;
        return currentNode;
    }

    // -------------------------
    // GET CURRENT
    // -------------------------
    public DialogueNode GetCurrent()
    {
        return currentNode;
    }
}