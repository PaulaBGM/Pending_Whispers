using System;
using UnityEngine;
using FMODUnity;
using FMOD.Studio;

public class NPCDialogue : MonoBehaviour
{
    public StudioEventEmitter audioEmitter;

    private void Awake()
    {
        audioEmitter = GetComponent<StudioEventEmitter>();
    }

    public void StartDialogue()
    {
        if (audioEmitter != null)
        {
            audioEmitter.Stop();
        }
    }
}