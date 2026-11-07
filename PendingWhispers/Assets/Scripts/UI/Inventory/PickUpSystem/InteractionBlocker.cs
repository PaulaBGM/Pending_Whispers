using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    // Bloqueos de otras pantallas (p. ej. la pizarra del HUB), por quien los pide.
    private readonly HashSet<object> extraBlockers = new();

    public static bool IsBlocked => Instance != null && Instance.blocked;

    /// <summary>Se lanza cuando cambia el estado de bloqueo.</summary>
    public static event Action<bool> OnBlockedChanged;

    /// <summary>Bloquea el mundo mientras 'source' lo pida (pizarra del HUB, etc.).</summary>
    public static void AddBlock(object source)
    {
        if (Instance == null || source == null)
            return;

        if (Instance.extraBlockers.Add(source))
            Instance.Refresh();
    }

    public static void RemoveBlock(object source)
    {
        if (Instance == null || source == null)
            return;

        if (Instance.extraBlockers.Remove(source))
            Instance.Refresh();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        if (journalStateChannel != null) journalStateChannel.OnRaised += HandleJournal;
        if (dialogueStateChannel != null) dialogueStateChannel.OnRaised += HandleDialogue;
        if (pauseStateChannel != null) pauseStateChannel.OnRaised += HandlePause;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        if (journalStateChannel != null) journalStateChannel.OnRaised -= HandleJournal;
        if (dialogueStateChannel != null) dialogueStateChannel.OnRaised -= HandleDialogue;
        if (pauseStateChannel != null) pauseStateChannel.OnRaised -= HandlePause;
    }

    private void HandleJournal(bool open) { journalOpen = open; Refresh(); }
    private void HandleDialogue(bool open) { dialogueOpen = open; Refresh(); }
    private void HandlePause(bool open) { pauseOpen = open; Refresh(); }

    // Persiste entre escenas y todas esas pantallas son de la escena: al cambiar de escena
    // se empieza sin bloqueos, para que nada se quede bloqueado si se cambia con una abierta.
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single)
            return;

        journalOpen = dialogueOpen = pauseOpen = false;
        extraBlockers.Clear();
        Refresh();
    }

    private void Refresh()
    {
        extraBlockers.RemoveWhere(source => source is UnityEngine.Object obj && obj == null);

        bool newBlocked = journalOpen || dialogueOpen || pauseOpen || extraBlockers.Count > 0;

        if (newBlocked == blocked)
            return;

        blocked = newBlocked;
        OnBlockedChanged?.Invoke(blocked);
    }
}