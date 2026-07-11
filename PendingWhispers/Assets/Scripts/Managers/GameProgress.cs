using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class GameProgress : BaseSingleton<GameProgress>
{
    [SerializeField] private FlagEventChannelSO onFlagAddedChannel;

    private HashSet<FlagSO> flags = new();

    public event Action<FlagSO> OnFlagAdded;

    public void AddFlag(FlagSO flag)
    {
        if (flag == null) return;

        if (flags.Add(flag))
        {
            Debug.Log("[GameProgress] Flag added: " + flag.id);

            OnFlagAdded?.Invoke(flag);
            onFlagAddedChannel?.Raise(flag);
            UIGameEvents.RaiseFlagAdded(flag);
        }
    }

    public bool HasFlag(FlagSO flag)
    {
        if (flag == null) return false;
        return flags.Contains(flag);
    }

    public bool HasAllFlags(List<FlagSO> required)
    {
        if (required == null || required.Count == 0) return true;

        foreach (var flag in required)
        {
            if (flag == null) continue;

            if (!flags.Contains(flag))
                return false;
        }

        return true;
    }

    public List<FlagSO> GetFlags()
    {
        return new List<FlagSO>(flags);
    }

    public int GetFlagCount()
    {
        return flags.Count;
    }
}