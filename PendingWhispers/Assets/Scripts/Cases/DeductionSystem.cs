using UnityEngine;
using System.Collections.Generic;
using Inventory.Model;

public class DeductionSystem : BaseSingleton<DeductionSystem>
{
    [SerializeField] private List<DeductionRule> rules;

    public void TryCombine(ItemSO a, ItemSO b)
    {
        foreach (var rule in rules)
        {
            bool match =
                (rule.itemA == a && rule.itemB == b) ||
                (rule.itemA == b && rule.itemB == a);

            if (!match) continue;

            GameProgress.Instance.AddFlag(rule.resultFlag);

            UIGameEvents.OnDeductionSuccess?.Invoke(rule.feedbackText);
            return;
        }

        UIGameEvents.OnDeductionFailed?.Invoke("No tiene sentido...");
    }
}