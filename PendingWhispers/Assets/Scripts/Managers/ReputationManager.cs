using System;
using UnityEngine;

public class ReputationManager : MonoBehaviour
{
    public static ReputationManager Instance;

    [SerializeField] private int reputation;

    public event Action<int> OnReputationChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public int GetReputation()
    {
        return reputation;
    }

    public void AddReputation(int amount)
    {
        reputation += amount;

        Debug.Log($"[Reputation] {amount:+#;-#;0} -> {reputation}");

        OnReputationChanged?.Invoke(reputation);
    }

    public bool HasReputation(int required)
    {
        return reputation >= required;
    }
}