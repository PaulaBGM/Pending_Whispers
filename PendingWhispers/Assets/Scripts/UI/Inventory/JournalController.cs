using System;
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
public enum JournalTab
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
/// - Las pestañas se configuran desde el Inspector (nombre en UXML ? página + filtro).
/// La API pública (IsOpen, ToggleJournal, OpenToCluesTab, OpenToPeopleTab) se mantiene.
/// </summary>
public class JournalController : BaseSingleton<JournalController>
{
    [Serializable]
    private class TabBinding
    {
        public JournalTab tab;
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
    [SerializeField] private JournalTab defaultTab = JournalTab.Inventory;

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

    private JournalTab? currentTab;
    private JournalTab pendingTab;

    private PlayableDirector activeDirector;
    private Action pendingDirectorCallback;

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
    }

    private void OnDisable()
    {
        if (toggleAction != null)
            toggleAction.action.performed -= OnToggleInput;
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
            Debug.LogError("JournalController: falta el UIDocument.", this);
            return;
        }

        root = document.rootVisualElement.Q<VisualElement>(rootElementName);
        if (root == null)
        {
            Debug.LogError($"JournalController: no se encontró '{rootElementName}'.", this);
            return;
        }

        foreach (string elementName in contentElementNames)
        {
            var element = root.Q<VisualElement>(elementName);
            if (element != null)
                contentElements.Add(element);
            else
                Debug.LogWarning($"JournalController: no se encontró '{elementName}'.", this);
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
    }

    private void BindTabButton(TabBinding binding)
    {
        binding.element = root.Q<VisualElement>(binding.elementName);
        if (binding.element == null)
        {
            Debug.LogWarning($"JournalController: no se encontró la pestaña '{binding.elementName}'.", this);
            return;
        }

        JournalTab tab = binding.tab;
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
        if (state == State.Closed)
            Open(defaultTab);
        else if (state == State.Open)
            RequestClose();
        // Durante una animación se ignora, como antes con isAnimating.
    }

    private void Open(JournalTab tab)
    {
        if (root == null)
            return;

        pendingTab = tab;
        state = State.Opening;

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
        TutorialPopup.Instance.ShowTutorialOnce("journal", "Journal",
            "Here you'll find clues, testimonies, known people, and hypotheses. Use this information to reconstruct each case.");
    }

    private void RequestClose()
    {
        if (state != State.Open)
            return;

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
    private void SelectTab(JournalTab tab)
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

    private void ShowTabImmediate(JournalTab tab)
    {
        var binding = FindBinding(tab) ?? FindBinding(defaultTab);
        if (binding == null || binding.page == null)
            return;

        HideAllPages();
        currentTab = binding.tab;
        binding.page.Show(binding.itemFilter);
        UpdateTabHighlight();
    }

    // =========================
    // EXTERNAL OPENERS
    // =========================
    public void OpenToTab(JournalTab tab)
    {
        switch (state)
        {
            case State.Closed: Open(tab); break;
            case State.Opening: pendingTab = tab; break;
            case State.Open: SelectTab(tab); break;
        }
    }

    public void OpenToCluesTab() => OpenToTab(JournalTab.Clues);
    public void OpenToPeopleTab() => OpenToTab(JournalTab.People);

    // =========================
    // TIMELINE
    // =========================
    private void SubscribeDirector(PlayableDirector director)
    {
        if (director != null)
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
        callback?.Invoke();
    }

    // =========================
    // HELPERS
    // =========================
    private TabBinding FindBinding(JournalTab tab)
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