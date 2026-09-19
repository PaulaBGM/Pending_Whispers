using System;
using UnityEngine;

public abstract class GameEventChannelSO<T> : ScriptableObject
{
    public event Action<T> OnRaised;

    public void Raise(T payload)
    {
        int listeners = OnRaised == null ? 0 : OnRaised.GetInvocationList().Length;

        Debug.Log($"[{name}] Raise() -> Listeners: {listeners}");

        OnRaised?.Invoke(payload);

        Debug.Log($"[{name}] Raise() finished");
    }
}