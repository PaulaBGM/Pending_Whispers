using UnityEngine;
using Inventory.Model;

[CreateAssetMenu(menuName = "Deduction/Rule")]
public class DeductionRule : ScriptableObject
{
    public ItemSO itemA;
    public ItemSO itemB;

    public FlagSO resultFlag;
    public string feedbackText;
}