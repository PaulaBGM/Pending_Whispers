using System;
using System.Collections.Generic;
using UnityEngine;

public class GameProgress : BaseSingleton<GameProgress>
{
    [SerializeField] private FlagEventChannelSO onFlagAddedChannel;

    private readonly HashSet<FlagSO> flags = new();

    public event Action<FlagSO> OnFlagAdded;

    public int FlagCount => flags.Count;

    public void AddFlag(FlagSO flag)
    {
        if (flag == null || !flags.Add(flag))
            return;

        Debug.Log($"[GameProgress] Flag added: {flag.id}");

        OnFlagAdded?.Invoke(flag);
        onFlagAddedChannel?.Raise(flag);
    }

    public bool HasFlag(FlagSO flag)
    {
        return flag != null && flags.Contains(flag);
    }

    public bool HasAllFlags(List<FlagSO> requiredFlags)
    {
        if (requiredFlags == null || requiredFlags.Count == 0)
            return true;

        foreach (var flag in requiredFlags)
        {
            if (flag != null && !flags.Contains(flag))
                return false;
        }

        return true;
    }

    public List<FlagSO> GetFlags()
    {
        return new List<FlagSO>(flags);
    }
}