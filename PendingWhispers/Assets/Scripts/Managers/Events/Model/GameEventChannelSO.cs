using System;
using UnityEngine;

public abstract class GameEventChannelSO<T> : ScriptableObject
{
    public event Action<T> OnRaised;

    public void Raise(T payload)
    {
        OnRaised?.Invoke(payload);
    }
}
