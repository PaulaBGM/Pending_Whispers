using UnityEngine;
using System;
using System.Collections.Generic;

public class EventManager : MonoBehaviour
{
    public static EventManager Instance;

    private Dictionary<string, Action> eventDictionary = new Dictionary<string, Action>();

    void Awake()
    {
        Instance = this;
    }

    public void Subscribe(string eventID, Action action)
    {
        if (!eventDictionary.ContainsKey(eventID))
            eventDictionary[eventID] = action;
        else
            eventDictionary[eventID] += action;
    }

    // NEW
    public void Unsubscribe(string eventID, Action action)
    {
        if (eventDictionary.ContainsKey(eventID))
        {
            eventDictionary[eventID] -= action;
        }
    }

    public void Trigger(string eventID)
    {
        if (eventDictionary.ContainsKey(eventID))
        {
            eventDictionary[eventID]?.Invoke();
        }
        else
        {
            Debug.Log("Evento no registrado: " + eventID);
        }
    }
}