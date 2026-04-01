using UnityEngine;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;

    private DialogueData currentDialogue;
    private DialogueNode currentNode;

    void Awake()
    {
        Instance = this;
    }

    public void StartDialogue(DialogueData dialogue)
    {
        currentDialogue = dialogue;
        currentDialogue.Initialize();

        GoToNode("start");
    }

    void GoToNode(string nodeID)
    {
        DialogueNode node = currentDialogue.GetNode(nodeID);

        if (node == null)
        {
            Debug.LogError("Nodo no encontrado: " + nodeID);
            return;
        }

        // comprobar condiciones
        if (!GameState.Instance.HasAllFlags(node.requiredFlags))
        {
            Debug.Log("Nodo bloqueado por flags");
            return;
        }

        currentNode = node;
        ShowNode();
    }

    void ShowNode()
    {
        Debug.Log(currentNode.speaker + ": " + currentNode.text);

        if (currentNode.choices != null && currentNode.choices.Count > 0)
        {
            for (int i = 0; i < currentNode.choices.Count; i++)
            {
                var choice = currentNode.choices[i];

                if (GameState.Instance.HasAllFlags(choice.requiredFlags))
                {
                    Debug.Log($"[{i}] {choice.text}");
                }
            }
        }
    }

    public void Choose(int index)
    {
        var choice = currentNode.choices[index];

        if (choice.addFlags != null)
        {
            foreach (var flag in choice.addFlags)
            {
                GameState.Instance.AddFlag(flag);
            }
        }

        GoToNode(choice.nextNodeID);
    }

    public void Next()
    {
        if (!string.IsNullOrEmpty(currentNode.nextNodeID))
        {
            GoToNode(currentNode.nextNodeID);
        }
        else
        {
            EndDialogue();
        }
    }

    void EndDialogue()
    {
        Debug.Log("Fin del dialogo");
    }
}