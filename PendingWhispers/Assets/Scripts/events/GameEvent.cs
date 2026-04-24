using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Events/Game Event")]
public class GameEvent : ScriptableObject
{
    private List<GameEventListener> listeners = new();

    public void Raise()
    {
        for (int i = listeners.Count - 1; i >= 0; i--)
        {
            listeners[i].OnEventRaised();
        }
    }

    public void RegisterListener(GameEventListener l)
    {
        if (!listeners.Contains(l)) listeners.Add(l);
    }

    public void UnregisterListener(GameEventListener l)
    {
        listeners.Remove(l);
    }
}