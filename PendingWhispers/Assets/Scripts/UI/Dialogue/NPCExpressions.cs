using UnityEngine;

public class NPCExpressions : MonoBehaviour
{
    [System.Serializable]
    public class ExpressionEntry
    {
        public DialogueExpression expression;
        public Sprite sprite;
    }

    [SerializeField]
    private ExpressionEntry[] expressions;

    public Sprite GetSprite(DialogueExpression expression)
    {
        foreach (var entry in expressions)
        {
            if (entry.expression == expression)
                return entry.sprite;
        }

        if (expressions.Length > 0)
            return expressions[0].sprite;

        return null;
    }
}