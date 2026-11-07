using System;
using UnityEngine;

/// <summary>
/// Bloquea la interacción con el mundo mientras haya una UI encima
/// (cuaderno, diálogo o pausa). Los interactuables consultan IsBlocked.
/// </summary>
public class InteractionBlocker : BaseSingleton<InteractionBlocker>
{
    [Header("Event Channels")]
    [SerializeField] private BoolEventChannelSO journalStateChannel;
    [SerializeField] private BoolEventChannelSO dialogueStateChannel;
    [SerializeField] private BoolEventChannelSO pauseStateChannel;

    private bool journalOpen;
    private bool dialogueOpen;
    private bool pauseOpen;
    private bool blocked;

    public static bool IsBlocked => Instance != null && Instance.blocked;

    /// <summary>Se lanza cuando cambia el estado de bloqueo.</summary>
    public static event Action<bool> OnBlockedChanged;

    private void OnEnable()
    {
        if (journalStateChannel != null) journalStateChannel.OnRaised += HandleJournal;
        if (dialogueStateChannel != null) dialogueStateChannel.OnRaised += HandleDialogue;
        if (pauseStateChannel != null) pauseStateChannel.OnRaised += HandlePause;
    }

    private void OnDisable()
    {
        if (journalStateChannel != null) journalStateChannel.OnRaised -= HandleJournal;
        if (dialogueStateChannel != null) dialogueStateChannel.OnRaised -= HandleDialogue;
        if (pauseStateChannel != null) pauseStateChannel.OnRaised -= HandlePause;
    }

    private void HandleJournal(bool open) { journalOpen = open; Refresh(); }
    private void HandleDialogue(bool open) { dialogueOpen = open; Refresh(); }
    private void HandlePause(bool open) { pauseOpen = open; Refresh(); }

    private void Refresh()
    {
        bool newBlocked = journalOpen || dialogueOpen || pauseOpen;

        if (newBlocked == blocked)
            return;

        blocked = newBlocked;
        OnBlockedChanged?.Invoke(blocked);
    }
}