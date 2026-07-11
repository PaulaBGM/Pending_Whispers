using System;
using UnityEngine;

public class ReputationManager : BaseSingleton<ReputationManager>
{

    [SerializeField, Range(0, 100)]
    private int reputation = 50;

    public event Action<int> OnReputationChanged;

    public int Reputation => reputation;

    public void SetReputation(int value)
    {
        reputation = Mathf.Clamp(value, 0, 100);

        Debug.Log($"[Reputation] Set -> {reputation}%");

        OnReputationChanged?.Invoke(reputation);
    }

    public void AddReputation(int amount)
    {
        reputation = Mathf.Clamp(reputation + amount, 0, 100);

        Debug.Log($"[Reputation] {amount:+#;-#;0} -> {reputation}%");

        OnReputationChanged?.Invoke(reputation);
    }

    public bool HasReputation(int requiredPercent)
    {
        return reputation >= requiredPercent;
    }
}