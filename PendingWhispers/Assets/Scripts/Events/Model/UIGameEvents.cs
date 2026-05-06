using System;
using UnityEngine;
using Inventory.Model;

public static class UIGameEvents
{
    // Feedback genérico
    public static Action<string> OnFeedback;

    // Gameplay events
    public static Action<ItemSO> OnItemCollected;
    public static Action<FlagSO> OnFlagAdded;
    public static Action<string> OnCaseResolved;
    public static Action<string> OnLocationUnlocked;
    public static Action<string> OnDeductionFailed;
    public static Action<string> OnDeductionSuccess;

    public static void RaiseLocationUnlocked(string name)
       => OnLocationUnlocked?.Invoke(name);

    public static void RaiseFeedback(string msg)
        => OnFeedback?.Invoke(msg);

    public static void RaiseFlagAdded(FlagSO flag)
        => OnFlagAdded?.Invoke(flag);
}