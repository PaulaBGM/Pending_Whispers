using System;
using System.Collections;
using System.Collections.Generic;
using FMODUnity;
using Inventory.Model;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;
using UnityEngine.UIElements;

/// <summary>
/// Identidad de cada pestaña del diario. Separada de ItemType porque hay pestañas
/// que no filtran items (Settings, Instructions) y varias pueden compartir página.
/// </summary>
public enum UIT_JournalTab
{
    Inventory,
    Instructions,
    Clues,
    People,
    Hypotheses,
    Settings
}

/// <summary>
/// Diario en UI Toolkit. Sustituye a la versión uGUI:
/// - Las páginas ya no son GameObjects: son VisualElements que se muestran/ocultan con display.
/// - Las animaciones (abrir, cerrar, pasar página) son PlayableDirectors (Timeline).
/// - Las pestañas se configuran desde el Inspector (nombre en UXML → página + filtro).
/// La API pública (IsOpen, ToggleJournal, OpenToCluesTab, OpenToPeopleTab) se mantiene.
/// </summary>
public class UIT_JournalController : BaseSingleton<UIT_JournalController>
{
    [Serializable]
    private class TabBinding
    {
        public UIT_JournalTab tab;
        [Tooltip("Nombre del botón de la pestaña en el UXML (p. ej. Tab_Inventory).")]
        public string elementName;
        public UIT_JournalPageView page;
        [Tooltip("Filtro que recibe la página. Solo lo usan las páginas que listan items.")]
        public ItemType itemFilter;

        [NonSerialized] public VisualElement element;
    }

    private enum State { Closed, Opening, Open, TurningPage, Closing }

    private const string SelectedTabClass = "journal-tab--selected";

    protected override bool PersistAcrossScenes => false;

    /// <summary>Igual que antes: true desde que se pide abrir hasta que se pide cerrar.</summary>
    public bool IsOpen => state == State.Opening || state == State.Open || state == State.TurningPage;

    [Header("UI Toolkit")]
    [SerializeField] private UIDocument document;
    [SerializeField] private string rootElementName = "Journal_Root";
    [SerializeField] private string closeButtonName = "B_CloseButton";
    [Tooltip("Elementos que se ocultan mientras el libro se abre/cierra.")]
    [SerializeField] private string[] contentElementNames = { "VE_Content_Container", "Journal_Tabs" };

    [Header("Pestañas")]
    [SerializeField] private List<TabBinding> tabs = new();
    [SerializeField] private UIT_JournalTab defaultTab = UIT_JournalTab.Inventory;

    [Header("Timelines")]
    [SerializeField] private PlayableDirector openDirector;
    [SerializeField] private PlayableDirector closeDirector;
    [SerializeField] private PlayableDirector pageTurnDirector;

    [Header("Input")]
    [Tooltip("UI/Inventory (Tab).")]
    [SerializeField] private InputActionReference toggleAction;

    [Header("Audio")]
    [SerializeField] private EventReference pageTurnSFX;

    [Header("Events")]
    [SerializeField] private BoolEventChannelSO onJournalStateChannel;

    private State state = State.Closed;
    private VisualElement root;
    private readonly List<VisualElement> contentElements = new();

    private UIT_JournalTab? currentTab;
    private UIT_JournalTab pendingTab;

    private PlayableDirector activeDirector;
    private Action pendingDirectorCallback;
    private Coroutine pendingCallbackRoutine;
    private bool hasBoundOnce;

    // =========================
    // LIFECYCLE
    // =========================
    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
            return;

        SubscribeDirector(openDirector);
        SubscribeDirector(closeDirector);
        SubscribeDirector(pageTurnDirector);
    }

    private void OnEnable()
    {
        if (toggleAction != null)
            toggleAction.action.performed += OnToggleInput;

        // Si alguien desactivó y reactivó este GameObject, el UIDocument ha reconstruido
        // su árbol y todas las referencias anteriores apuntan a elementos muertos.
        if (hasBoundOnce)
            StartCoroutine(RebindNextFrame());
    }

    private void OnDisable()
    {
        if (toggleAction != null)
            toggleAction.action.performed -= OnToggleInput;

        if (pendingCallbackRoutine != null)
        {
            StopCoroutine(pendingCallbackRoutine);
            pendingCallbackRoutine = null;
        }

        activeDirector = null;
        pendingDirectorCallback = null;

        if (state != State.Closed)
        {
            // La pila de este aviso indica qué script o timeline ha desactivado el diario.
            Debug.LogWarning($"UIT_JournalController: '{gameObject.name}' se ha desactivado con el diario en estado {state}. " +
                             "No desactives el GameObject del UIDocument (destruye el árbol visual). " +
                             "Mira la pila de llamadas de este aviso para ver quién lo hace.", this);

            bool wasOpen = state != State.Closing;
            state = State.Closed;
            currentTab = null;
            if (wasOpen)
                onJournalStateChannel?.Raise(false);
        }
    }

    private IEnumerator RebindNextFrame()
    {
        // El UIDocument recrea rootVisualElement en su propio OnEnable; esperamos a que termine.
        yield return null;
        BindDocument();
    }

    // El rootVisualElement del UIDocument se crea en su OnEnable: en Start ya es seguro.
    private void Start()
    {
        if (Instance != this)
            return;

        BindDocument();
    }

    protected override void OnDestroy()
    {
        UnsubscribeDirector(openDirector);
        UnsubscribeDirector(closeDirector);
        UnsubscribeDirector(pageTurnDirector);
        base.OnDestroy();
    }

    // =========================
    // BINDING
    // =========================
    private void BindDocument()
    {
        if (document == null)
        {
            Debug.LogError("UIT_JournalController: falta el UIDocument.", this);
            return;
        }

        contentElements.Clear();
        root = document.rootVisualElement.Q<VisualElement>(rootElementName);
        if (root == null)
        {
            Debug.LogError($"UIT_JournalController: no se encontró '{rootElementName}'.", this);
            return;
        }

        foreach (string elementName in contentElementNames)
        {
            var element = root.Q<VisualElement>(elementName);
            if (element != null)
                contentElements.Add(element);
            else
                Debug.LogWarning($"UIT_JournalController: no se encontró '{elementName}'.", this);
        }

        var closeButton = root.Q<Button>(closeButtonName);
        if (closeButton != null)
            closeButton.clicked += RequestClose;

        // Cada página se enlaza una sola vez aunque la compartan varias pestañas.
        var boundPages = new HashSet<UIT_JournalPageView>();
        foreach (var binding in tabs)
        {
            if (binding.page != null && boundPages.Add(binding.page))
                binding.page.Bind(root);

            BindTabButton(binding);
        }

        root.style.display = DisplayStyle.None;
        SetContentVisible(false);
        hasBoundOnce = true;

        Debug.Log($"UIT_JournalController: enlazado. Páginas: {boundPages.Count}, " +
                  $"pestañas: {tabs.Count}, botón cerrar: {(closeButton != null ? "OK" : "NO ENCONTRADO")}.", this);
    }

    private void BindTabButton(TabBinding binding)
    {
        // Un nombre vacío haría que Q devolviera el propio root y cualquier clic cambiaría de pestaña.
        binding.element = string.IsNullOrEmpty(binding.elementName)
            ? null
            : root.Q<VisualElement>(binding.elementName);
        if (binding.element == null)
        {
            Debug.LogWarning($"UIT_JournalController: no se encontró la pestaña '{binding.elementName}'.", this);
            return;
        }

        UIT_JournalTab tab = binding.tab;
        if (binding.element is Button button)
            button.clicked += () => SelectTab(tab);
        else
            binding.element.RegisterCallback<ClickEvent>(_ => SelectTab(tab));
    }

    // =========================
    // OPEN / CLOSE
    // =========================
    private void OnToggleInput(InputAction.CallbackContext _) => ToggleJournal();

    public void ToggleJournal()
    {
        if (state != State.Closed && state != State.Open)
            Debug.Log($"UIT_JournalController: Toggle ignorado, estado actual {state}.", this);

        if (state == State.Closed)
            Open(defaultTab);
        else if (state == State.Open)
            RequestClose();
        // Durante una animación se ignora, como antes con isAnimating.
    }

    private void Open(UIT_JournalTab tab)
    {
        if (root == null)
            return;

        pendingTab = tab;
        state = State.Opening;
        Debug.Log($"UIT_JournalController: abriendo (timeScale = {Time.timeScale}).", this);

        root.style.display = DisplayStyle.Flex;
        SetContentVisible(false);
        HideAllPages();

        PlayDirector(openDirector, HandleOpenFinished);
    }

    private void HandleOpenFinished()
    {
        if (state != State.Opening)
            return;

        state = State.Open;
        SetContentVisible(true);
        ShowTabImmediate(pendingTab);

        onJournalStateChannel?.Raise(true);

        if (TutorialPopup.Instance != null)
            TutorialPopup.Instance.ShowTutorialOnce("journal", "Journal",
                "Here you'll find clues, testimonies, known people, and hypotheses. Use this information to reconstruct each case.");
        else
            Debug.LogWarning("UIT_JournalController: TutorialPopup.Instance es null; no se muestra el tutorial.", this);
    }

    private void RequestClose()
    {
        if (state != State.Open)
        {
            Debug.Log($"UIT_JournalController: cierre ignorado, estado actual {state}.", this);
            return;
        }

        state = State.Closing;
        HideAllPages();
        currentTab = null;
        UpdateTabHighlight();
        SetContentVisible(false);

        PlayDirector(closeDirector, HandleCloseFinished);
    }

    private void HandleCloseFinished()
    {
        if (state != State.Closing)
            return;

        state = State.Closed;
        // Ojo: NO desactivar el GameObject del UIDocument; eso reconstruye el árbol
        // y se pierden todas las referencias y callbacks.
        root.style.display = DisplayStyle.None;
        onJournalStateChannel?.Raise(false);
    }

    // =========================
    // TABS
    // =========================
    private void SelectTab(UIT_JournalTab tab)
    {
        if (state != State.Open || currentTab == tab)
            return;

        if (FindBinding(tab) == null)
            return;

        pendingTab = tab;
        state = State.TurningPage;
        HideAllPages();

        if (!pageTurnSFX.IsNull)
            RuntimeManager.PlayOneShot(pageTurnSFX);

        PlayDirector(pageTurnDirector, HandlePageTurnFinished);
    }

    private void HandlePageTurnFinished()
    {
        if (state != State.TurningPage)
            return;

        state = State.Open;
        ShowTabImmediate(pendingTab);
    }

    private void ShowTabImmediate(UIT_JournalTab tab)
    {
        var binding = FindBinding(tab) ?? FindBinding(defaultTab);
        if (binding == null || binding.page == null)
        {
            Debug.LogWarning($"UIT_JournalController: no hay página configurada para la pestaña '{tab}' " +
                             $"(ni para la pestaña por defecto '{defaultTab}'). Revisa la lista Tabs.", this);
            return;
        }

        if (!binding.page.IsBound)
            Debug.LogWarning($"UIT_JournalController: la página '{binding.page.name}' no está enlazada; " +
                             "revisa sus Element Names.", binding.page);

        HideAllPages();
        currentTab = binding.tab;
        binding.page.Show(binding.itemFilter);
        UpdateTabHighlight();
    }

    // =========================
    // EXTERNAL OPENERS
    // =========================
    public void OpenToTab(UIT_JournalTab tab)
    {
        switch (state)
        {
            case State.Closed: Open(tab); break;
            case State.Opening: pendingTab = tab; break;
            case State.Open: SelectTab(tab); break;
        }
    }

    public void OpenToCluesTab() => OpenToTab(UIT_JournalTab.Clues);
    public void OpenToPeopleTab() => OpenToTab(UIT_JournalTab.People);

    // =========================
    // TIMELINE
    // =========================
    private void SubscribeDirector(PlayableDirector director)
    {
        if (director == null)
            return;

        // Se fuerza por código para no depender del Inspector:
        // - Unscaled: si el juego se pausa (timeScale = 0) el timeline sigue avanzando.
        //   Con Game Time se quedaría congelado y el diario atascado en "abriendo"/"cerrando".
        // - Wrap None: con Hold o Loop el director nunca se para y no llega el evento stopped.
        director.playOnAwake = false;
        director.timeUpdateMode = DirectorUpdateMode.UnscaledGameTime;
        director.extrapolationMode = DirectorWrapMode.None;

        director.stopped += HandleDirectorStopped;
    }

    private void UnsubscribeDirector(PlayableDirector director)
    {
        if (director != null)
            director.stopped -= HandleDirectorStopped;
    }

    private void PlayDirector(PlayableDirector director, Action onFinished)
    {
        if (director == null || director.playableAsset == null)
        {
            onFinished?.Invoke();
            return;
        }

        activeDirector = director;
        pendingDirectorCallback = onFinished;

        director.time = 0;
        director.Evaluate();
        director.Play();
    }

    // Requiere Wrap Mode = None en el director; con Hold o Loop nunca se dispara stopped.
    private void HandleDirectorStopped(PlayableDirector director)
    {
        if (director != activeDirector)
            return;

        activeDirector = null;
        var callback = pendingDirectorCallback;
        pendingDirectorCallback = null;

        // Al parar, el plugin de animación restaura los estilos que tenían los elementos
        // al empezar el timeline (display incluido). Si mostramos la página o cerramos el
        // diario aquí mismo, esa restauración lo deshace. Esperamos un frame para aplicar
        // nuestros cambios después de la restauración.
        // Si el director se ha parado porque se desactivó el GameObject, no hay nada que hacer:
        // OnDisable ya ha dejado el diario cerrado.
        // Ojo: durante la desactivación el director se para ANTES de que llegue nuestro OnDisable,
        // así que isActiveAndEnabled aún es true; activeInHierarchy ya es false.
        if (!gameObject.activeInHierarchy)
            return;

        if (pendingCallbackRoutine != null)
            StopCoroutine(pendingCallbackRoutine);
        pendingCallbackRoutine = StartCoroutine(InvokeNextFrame(callback));
    }

    private IEnumerator InvokeNextFrame(Action callback)
    {
        yield return null;
        pendingCallbackRoutine = null;
        callback?.Invoke();
    }

    // =========================
    // HELPERS
    // =========================
    private TabBinding FindBinding(UIT_JournalTab tab)
    {
        foreach (var binding in tabs)
            if (binding.tab == tab)
                return binding;
        return null;
    }

    private void HideAllPages()
    {
        foreach (var binding in tabs)
            binding.page?.Hide();
    }

    private void SetContentVisible(bool visible)
    {
        foreach (var element in contentElements)
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void UpdateTabHighlight()
    {
        foreach (var binding in tabs)
            binding.element?.EnableInClassList(SelectedTabClass, currentTab == binding.tab);
    }
}