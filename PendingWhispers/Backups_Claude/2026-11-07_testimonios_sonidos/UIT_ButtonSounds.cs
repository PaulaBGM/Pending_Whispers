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
/// Escucha los clics en la raíz del documento, así que también suenan los botones
/// creados por código (nodos del mapa, opciones de diálogo, post-its, slots...).
///
/// - Click Event: sonido por defecto de cualquier botón del documento.
/// - Overrides: sonido distinto para botones concretos (por nombre del UXML),
///   o "silenciar" uno dejando el evento vacío y marcando Mute.
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

    private VisualElement root;
    private readonly Dictionary<string, ButtonSoundOverride> overridesByName = new();
    private Button lastHovered;

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

        // El árbol del UIDocument se crea en su OnEnable; puede no existir aún en el primer frame.
        TryBind();
    }

    private void Start() => TryBind();

    // Si el UIDocument se desactiva y reactiva (el diario lo hace), crea un árbol nuevo y los
    // callbacks del anterior se pierden: se vuelve a enganchar al nuevo.
    private void LateUpdate()
    {
        if (document != null && document.rootVisualElement != root)
        {
            Unbind();
            TryBind();
        }
    }

    private void OnDisable() => Unbind();

    private void Unbind()
    {
        if (root == null)
            return;

        root.UnregisterCallback<ClickEvent>(OnClick, TrickleDown.TrickleDown);
        root.UnregisterCallback<PointerOverEvent>(OnPointerOver, TrickleDown.TrickleDown);
        root = null;
        lastHovered = null;
    }

    private void TryBind()
    {
        if (root != null || document == null || document.rootVisualElement == null)
            return;

        root = document.rootVisualElement;

        // TrickleDown: se recibe antes de que ningún botón pare la propagación del clic.
        root.RegisterCallback<ClickEvent>(OnClick, TrickleDown.TrickleDown);
        root.RegisterCallback<PointerOverEvent>(OnPointerOver, TrickleDown.TrickleDown);
    }

    private void OnClick(ClickEvent evt)
    {
        if (evt.button != 0)
            return;

        Button button = FindButton(evt.target as VisualElement);
        if (button == null || !button.enabledInHierarchy)
            return;

        EventReference sound = clickEvent;

        if (!string.IsNullOrEmpty(button.name) && overridesByName.TryGetValue(button.name, out var entry))
        {
            if (entry.mute)
                return;
            if (!entry.clickEvent.IsNull)
                sound = entry.clickEvent;
        }

        Play(sound);
    }

    private void OnPointerOver(PointerOverEvent evt)
    {
        if (hoverEvent.IsNull)
            return;

        Button button = FindButton(evt.target as VisualElement);

        // Al moverse entre los hijos de un mismo botón no se repite el sonido.
        if (button == lastHovered)
            return;

        lastHovered = button;

        if (button != null && button.enabledInHierarchy)
            Play(hoverEvent);
    }

    private static Button FindButton(VisualElement element)
    {
        for (var current = element; current != null; current = current.parent)
        {
            if (current is Button button)
                return button;
        }
        return null;
    }

    private static void Play(EventReference sound)
    {
        if (!sound.IsNull)
            RuntimeManager.PlayOneShot(sound);
    }
}
