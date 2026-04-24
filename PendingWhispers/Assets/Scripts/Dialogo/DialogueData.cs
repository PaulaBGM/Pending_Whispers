using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Dialogue", menuName = "Dialogue/New Dialogue")]
public class DialogueData : ScriptableObject
{
    [Header("Characters")]
    public List<DialogueCharacter> characters;

    [Header("Nodes")]
    public List<DialogueNode> nodes;

    private Dictionary<string, DialogueNode> nodeDict;
    private Dictionary<string, DialogueCharacter> characterDict;

    public void Initialize()
    {
        nodeDict = new Dictionary<string, DialogueNode>();
        characterDict = new Dictionary<string, DialogueCharacter>();

        // Nodos
        foreach (var node in nodes)
        {
            if (string.IsNullOrEmpty(node.id))
            {
                Debug.LogError("Nodo sin ID");
                continue;
            }

            if (nodeDict.ContainsKey(node.id))
            {
                Debug.LogError("ID duplicado: " + node.id);
                continue;
            }

            nodeDict[node.id] = node;
        }

        // Personajes
        foreach (var character in characters)
        {
            if (string.IsNullOrEmpty(character.id))
            {
                Debug.LogError("Personaje sin ID");
                continue;
            }

            if (characterDict.ContainsKey(character.id))
            {
                Debug.LogError("Character ID duplicado: " + character.id);
                continue;
            }

            characterDict[character.id] = character;
        }
    }

    public DialogueNode GetNode(string id)
    {
        if (nodeDict == null)
            Initialize();

        if (!nodeDict.ContainsKey(id))
        {
            Debug.LogError("Nodo no encontrado: " + id);
            return null;
        }

        return nodeDict[id];
    }

    public DialogueCharacter GetCharacter(string id)
    {
        if (characterDict == null)
            Initialize();

        if (!characterDict.ContainsKey(id))
        {
            Debug.LogError("Personaje no encontrado: " + id);
            return null;
        }

        return characterDict[id];
    }
}