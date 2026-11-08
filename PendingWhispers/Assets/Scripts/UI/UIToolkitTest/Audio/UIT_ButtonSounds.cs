using System;
using System.Collections.Generic;
using FMODUnity;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Sonidos FMOD para los botones de un UIDocument (UI Toolkit).
/// Sustituye a los StudioEventEmitter que había en los botones uGUI.
///
/// Se pone en el mismo GameObject que el UIDocument (o se le asigna uno).
/// Se engancha al evento 'clicked' de cada Button del documento (el mismo que usa la lógica
/// del juego). Revisa el árbol cada poco, así que también suenan los botones creados por
/// código (nodos del mapa, opciones de diálogo, post-its, slots...).
///
/// - Click Event: sonido por defecto de cualquier botón del documento.
/// - Overrides: sonido distinto para botones concretos (por nombre del UXML),
///   o silenciar uno marcando Mute.
/// - Hover Event (opcional): al pasar el ratón por encima de un botón activo.
/// </summary>
public class UIT_ButtonSounds : MonoBehaviour
{
    [Serializable]
    private class ButtonSoundOverride
    {
        [Tooltip("Nombre del botón en el UXML (p. ej. HUD_Map_Button).")]
        public string buttonName;
        [Tooltip("Evento que suena al pulsarlo en lugar del de por defecto.")]
        public EventReference clickEvent;
        [Tooltip("Marcado: este botón no suena (p. ej. porque su script ya reproduce otro sonido).")]
        public bool mute;
    }

    [SerializeField] private UIDocument document;

    [Header("Por defecto")]
    [SerializeField] private EventReference clickEvent;
    [Tooltip("Opcional: vacío = sin sonido al pasar el ratón.")]
    [SerializeField] private EventReference hoverEvent;

    [Header("Botones concretos")]
    [SerializeField] private List<ButtonSoundOverride> overrides = new();

    [Header("Depuración")]
    [Tooltip("Escribe en consola cada sonido que reproduce.")]
    [SerializeField] private bool logPlays = false;

    // Cada cuántos frames se buscan botones nuevos en el documento.
    private const int ScanInterval = 15;

    private VisualElement root;
    private readonly Dictionary<string, ButtonSoundOverride> overridesByName = new();
    private readonly Dictionary<Button, Action> clickHandlers = new();
    private readonly Dictionary<Button, EventCallback<PointerEnterEvent>> hoverHandlers = new();
    private readonly List<Button> scratch = new();
    private int nextScanFrame;
    private bool loggedBind;
    
    private void Reset()
    {
        document = GetComponent<UIDocument>();
    }

    private void OnEnable()
    {
        if (document == null)
            document = GetComponent<UIDocument>();

        overridesByName.Clear();
        foreach (var entry in overrides)
        {
            if (entry != null && !string.IsNullOrEmpty(entry.buttonName))
                overridesByName[entry.buttonName] = entry;
        }

        nextScanFrame = 0;
    }

    private void OnDisable() => UnhookAll();

    // Se revisa en LateUpdate: el árbol del UIDocument puede no existir en OnEnable,
    // puede reconstruirse (el diario lo hace) y los scripts crean botones por código.
    private void LateUpdate()
    {
        if (document == null)
            return;

        VisualElement currentRoot = document.rootVisualElement;
        if (currentRoot != root)
        {
            UnhookAll();
            root = currentRoot;
            nextScanFrame = 0;
        }

        if (root == null || Time.frameCount < nextScanFrame)
            return;

        nextScanFrame = Time.frameCount + ScanInterval;
        ScanButtons();
    }

    private void ScanButtons()
    {
        scratch.Clear();
        root.Query<Button>().ToList(scratch);

        int added = 0;
        foreach (Button button in scratch)
        {
            if (clickHandlers.ContainsKey(button))
                continue;

            Button captured = button;
            Action onClick = () => OnButtonClicked(captured);
            captured.clicked += onClick;
            clickHandlers[captured] = onClick;

            if (!hoverEvent.IsNull)
            {
                EventCallback<PointerEnterEvent> onHover = _ => OnButtonHovered(captured);
                captured.RegisterCallback(onHover);
                hoverHandlers[captured] = onHover;
            }

            added++;
        }

        if (!loggedBind && clickHandlers.Count > 0)
        {
            loggedBind = true;
            Debug.Log($"[UIT_ButtonSounds] '{name}': sonido en {clickHandlers.Count} botones " +
                      $"(por defecto: {(clickEvent.IsNull ? "NINGUNO" : clickEvent.ToString())}).", this);
        }
    }

    private void UnhookAll()
    {
        foreach (var pair in clickHandlers)
        {
            if (pair.Key != null)
                pair.Key.clicked -= pair.Value;
        }

        foreach (var pair in hoverHandlers)
            pair.Key?.UnregisterCallback(pair.Value);

        clickHandlers.Clear();
        hoverHandlers.Clear();
        root = null;
    }

    private void OnButtonClicked(Button button)
    {
        EventReference sound = clickEvent;

        if (!string.IsNullOrEmpty(button.name) && overridesByName.TryGetValue(button.name, out var entry))
        {
            if (entry.mute)
                return;
            if (!entry.clickEvent.IsNull)
                sound = entry.clickEvent;
        }

        Play(sound, button);
    }

    private void OnButtonHovered(Button button)
    {
        if (button.enabledInHierarchy)
            Play(hoverEvent, button);
    }

    private void Play(EventReference sound, Button button)
    {
        if (sound.IsNull)
            return;

        if (logPlays)
            Debug.Log($"[UIT_ButtonSounds] {button.name} -> {sound}", this);

        RuntimeManager.PlayOneShot(sound);
    }
}
