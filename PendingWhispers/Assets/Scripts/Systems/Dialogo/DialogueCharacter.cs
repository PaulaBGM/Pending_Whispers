using UnityEngine;

[System.Serializable]
public class DialogueCharacter
{
    public string id;
    public string displayName;

    public Sprite portrait;

    public bool isLeftSide;

    [Header("Expressions")]
    public Sprite neutral;
    public Sprite happy;
    public Sprite sad;
    public Sprite angry;
    public Sprite surprised;
    public Sprite thinking;

    public Sprite GetExpression(DialogueExpression expression)
    {
        switch (expression)
        {
            case DialogueExpression.Happy: return happy;
            case DialogueExpression.Sad: return sad;
            case DialogueExpression.Angry: return angry;
            case DialogueExpression.Surprised: return surprised;
            case DialogueExpression.Thinking: return thinking;
            default: return neutral;
        }
    }
}