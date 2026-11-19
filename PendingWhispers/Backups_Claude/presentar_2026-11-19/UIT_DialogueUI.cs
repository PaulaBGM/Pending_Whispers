using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using RuntimeManager = FMODUnity.RuntimeManager;
using Inventory.Model;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using ListView = UnityEngine.UIElements.ListView;

/// <summary>
/// Vista de diálogo en UI Toolkit. Sustituye a DialogueUI + CharacterUIController
/// con la misma API (IDialogueView), así DialogueManager no cambia de comportamiento.
///
/// Diferencias con la versión uGUI:
/// - Avanzar es un clic sobre el panel de diálogo (o UI/Submit), no Input.GetMouseButtonDown
///   global: un clic en una opción ya no salta el texto de la línea siguiente.
/// - Las opciones van en el ListView del UXML (LV_ButtonsContainer); si se cambia por un
///   VisualElement/ScrollView también funciona, con un pool de botones.
/// - Retratos: clases USS (activo/inactivo) en vez de colores en el Inspector.
/// - El GameObject del UIDocument NO se desactiva nunca: se oculta con display.
/// </summary>
public class UIT_DialogueUI : MonoBehaviour, IDialogueView
{
    public static UIT_DialogueUI Instance { get; private set; }

    private const string ChoiceClass = "dialogue-choice";
    private const string ChoiceSeenClass = "dialogue-choice--seen";
    private const string CharacterActiveClass = "dialogue-character--active";
    private const string CharacterInactiveClass = "dialogue-character--inactive";
    private const string CharacterHiddenClass = "dialogue-character--hidden";

    private sealed class ChoiceEntry
    {
        public string Text;
        public bool Seen;
        public Action OnSelected;
    }

    [Header("UI Toolkit")]
    [SerializeField] private UIDocument document;
    [SerializeField] private string dialogueRootName = "Dialogue_Root";
    [SerializeField] private string speakerLabelName = "T_SpeakerName";
    [SerializeField] private string dialogueLabelName = "T_DialogueText";
    [SerializeField] private string choicesContainerName = "LV_ButtonsContainer";
    [SerializeField] private string leftCharacterName = "VE_LeftCharacter";
    [SerializeField] private string rightCharacterName = "VE_RightCharacter";
    [SerializeField] private string journalButtonName = "BT_JournalButton";
    [Tooltip("Opcional. Botón para presentar pruebas; vacío si no existe todavía.")]
    [SerializeField] private string presentEvidenceButtonName = "BT_PresentEvidence";
    [Tooltip("Opcional. Indicador de 'continuar' que se muestra al acabar de escribir.")]
    [SerializeField] private string continueIndicatorName = "";

    [Header("Typewriter")]
    [SerializeField] private float typingSpeed = 0.03f;
    [Tooltip("Escribe el texto completo con el resto transparente, para que las palabras no salten de línea.")]
    [SerializeField] private bool preventWordJump = true;

    [Header("Topic Selector")]
    [SerializeField] private string topicSelectorSpeaker = "Talk";
    [SerializeField] private string seenTopicPrefix = "✓  ";
    [Tooltip("Color del texto de los temas ya hablados (el mismo que tenía el DialogueUI antiguo).")]
    [SerializeField] private Color seenTopicColor = new Color(0.55f, 0.12f, 0.24f, 1f);
    [Tooltip("Color del texto de las opciones normales (la plantilla usa blanco).")]
    [SerializeField] private Color unseenTopicColor = Color.white;

    [Header("Botones de respuesta")]
    [Tooltip("Plantilla de cada opción (ButtonReplyDialogue_Template). Vacío = botón simple.")]
    [SerializeField] private VisualTreeAsset replyButtonTemplate;
    [SerializeField] private string replyButtonName = "BT_ReplyDialogueButton";
    [SerializeField] private string replyLabelName = "T_DialogueReply";

    [Header("Input")]
    [Tooltip("Opcional: para avanzar con teclado/mando. El ratón ya avanza con el clic sobre el diálogo.")]
    [SerializeField] private InputActionReference advanceAction;

    [Header("Evidence Presentation")]
    [Tooltip("De momento el selector uGUI; se migrará a UI Toolkit más adelante.")]
    [SerializeField] private EvidencePickerUI evidencePicker;

    [Header("FMOD")]
    [SerializeField] private string dialogueEventPath = "event:/Dialogue";

    public event Action<ItemSO> OnEvidenceItemSelected;

    public bool IsShowing { get; private set; }

    private VisualElement documentRoot;
    private VisualElement dialogueRoot;
    private Label speakerLabel;
    private Label dialogueLabel;
    private VisualElement choicesContainer;
    private ListView choicesList;
    private VisualElement leftCharacter;
    private VisualElement rightCharacter;
    private VisualElement presentEvidenceButton;
    private VisualElement continueIndicator;

    private readonly List<ChoiceEntry> choiceEntries = new();
    private readonly List<VisualElement> choiceButtonPool = new(); // solo si el contenedor no es un ListView
    private int lastAdvanceFrame = -1;
    private readonly StringBuilder typingBuilder = new();

    private Coroutine typingCoroutine;
    private bool isTyping;
    private string fullText = string.Empty;
    private bool isBound;

    // =========================
    // LIFECYCLE
    // =========================
    private void Awake()
    {
        Instance = this;

        if (evidencePicker != null)
            evidencePicker.OnItemSelected += HandleEvidenceSelected;
    }

    private void OnEnable()
    {
        if (advanceAction != null)
            advanceAction.action.performed += OnAdvanceInput;
    }

    private void OnDisable()
    {
        if (advanceAction != null)
            advanceAction.action.performed -= OnAdvanceInput;
    }

    private void Start()
    {
        EnsureBound();
        if (!IsShowing)
            SetVisible(false);
    }

    private void OnDestroy()
    {
        if (evidencePicker != null)
            evidencePicker.OnItemSelected -= HandleEvidenceSelected;

        if (Instance == this)
            Instance = null;
    }

    // =========================
    // BINDING
    // =========================
    /// <summary>El árbol del UIDocument existe tras su OnEnable; se enlaza la primera vez que hace falta.</summary>
    private bool EnsureBound()
    {
        if (isBound)
            return true;

        if (document == null || document.rootVisualElement == null)
        {
            Debug.LogError("UIT_DialogueUI: falta el UIDocument o aún no tiene árbol.", this);
            return false;
        }

        documentRoot = document.rootVisualElement;
        dialogueRoot = Require<VisualElement>(dialogueRootName);
        speakerLabel = Require<Label>(speakerLabelName);
        dialogueLabel = Require<Label>(dialogueLabelName);
        choicesContainer = Require<VisualElement>(choicesContainerName);
        leftCharacter = Require<VisualElement>(leftCharacterName);
        rightCharacter = Require<VisualElement>(rightCharacterName);
        presentEvidenceButton = Optional<VisualElement>(presentEvidenceButtonName);
        continueIndicator = Optional<VisualElement>(continueIndicatorName);

        if (dialogueRoot == null || dialogueLabel == null || choicesContainer == null)
            return false;

        SetupChoicesContainer();

        // Clic en cualquier parte del panel = avanzar. Las opciones paran la propagación.
        dialogueRoot.RegisterCallback<ClickEvent>(OnDialogueClicked);

        var journalButton = Optional<Button>(journalButtonName);
        if (journalButton != null)
            journalButton.clicked += OnJournalButtonClicked;

        if (presentEvidenceButton is Button presentButton)
            presentButton.clicked += OnPresentEvidenceButtonClicked;
        else
            presentEvidenceButton?.RegisterCallback<ClickEvent>(evt =>
            {
                evt.StopPropagation();
                OnPresentEvidenceButtonClicked();
            });

        // Los sprites del UXML son de muestra para UI Builder: se limpian al arrancar.
        ResetCharacters();
        SetEvidenceButtonVisible(false);
        SetChoicesVisible(false);
        SetContinueVisible(false);

        isBound = true;
        return true;
    }

    private T Require<T>(string elementName) where T : VisualElement
    {
        var element = documentRoot.Q<T>(elementName);
        if (element == null)
            Debug.LogError($"UIT_DialogueUI: no se encontró '{elementName}' ({typeof(T).Name}) en el UXML.", this);
        return element;
    }

    private T Optional<T>(string elementName) where T : VisualElement
    {
        if (string.IsNullOrEmpty(elementName))
            return null;

        var element = documentRoot.Q<T>(elementName);
        if (element == null)
            Debug.LogWarning($"UIT_DialogueUI: no se encontró '{elementName}'. Se ignora.", this);
        return element;
    }

    private void SetupChoicesContainer()
    {
        choicesList = choicesContainer as ListView;
        if (choicesList == null)
            return;

        choicesList.selectionType = SelectionType.None;
        choicesList.virtualizationMethod = CollectionVirtualizationMethod.DynamicHeight;
        choicesList.itemsSource = choiceEntries;
        choicesList.makeItem = CreateChoiceButton;
        choicesList.bindItem = BindChoiceButton;
    }

    // =========================
    // IDialogueView
    // =========================
    public void ShowLine(DialogueCharacter character, string speaker, string text, Sprite expressionSprite)
    {
        if (!EnsureBound())
            return;

        SetVisible(true);
        SetSpeaker(speaker);
        SetCharacter(character, expressionSprite);
        StartTyping(text ?? string.Empty);
    }

    public void ShowChoices(List<DialogueChoice> choices)
    {
        if (!EnsureBound())
            return;

        choiceEntries.Clear();
        foreach (var choice in choices)
        {
            DialogueChoice captured = choice;
            choiceEntries.Add(new ChoiceEntry
            {
                Text = captured.text,
                OnSelected = () => DialogueManager.Instance.ChooseChoice(captured)
            });
        }

        RefreshChoices();
    }

    public void ShowDialogueSelector(string title,
                                     List<DialogueCondition> topics,
                                     Func<DialogueCondition, bool> isTopicSeen,
                                     Action<DialogueCondition> onTopicSelected)
    {
        if (!EnsureBound())
            return;

        SetVisible(true);
        StopTyping();
        // Nombre del NPC con el que se habla; "Talk" solo si no se sabe.
        SetSpeaker(string.IsNullOrEmpty(title) ? topicSelectorSpeaker : title);
        dialogueLabel.text = string.Empty;
        ResetCharacters();

        choiceEntries.Clear();
        foreach (DialogueCondition topic in topics)
        {
            DialogueCondition captured = topic;
            bool seen = !captured.hideSeenCheckmark && (isTopicSeen?.Invoke(captured) ?? false);

            choiceEntries.Add(new ChoiceEntry
            {
                Text = seen ? $"{seenTopicPrefix}{captured.DisplayTitle}" : captured.DisplayTitle,
                Seen = seen,
                OnSelected = () => onTopicSelected?.Invoke(captured)
            });
        }

        RefreshChoices();
    }

    public void ClearChoices()
    {
        choiceEntries.Clear();
        RefreshChoices();
    }

    public void Hide()
    {
        StopTyping();
        ClearChoices();
        SetEvidenceButtonVisible(false);
        evidencePicker?.Hide();
        ResetCharacters();
        SetVisible(false);
    }

    // Los botones del diálogo se muestran/ocultan con SetEnabled: el USS (#...:disabled) decide cómo
    // se esconden (y puede animarlo). Un botón desactivado tampoco recibe clics.
    public void SetEvidenceButtonVisible(bool visible)
    {
        presentEvidenceButton?.SetEnabled(visible);
    }

    public void OpenEvidencePicker()
    {
        evidencePicker?.Show();
    }

    public void OnPresentEvidenceButtonClicked()
    {
        DialogueManager.Instance.OpenEvidencePicker();
    }

    // =========================
    // ADVANCE
    // =========================
    private void OnDialogueClicked(ClickEvent evt)
    {
        if (evt.button != 0)
            return;

        // Clics en botones (opciones, diario, presentar) no avanzan el diálogo.
        if (evt.target is VisualElement target && IsInsideButton(target))
            return;

        TryAdvance();
    }

    private void OnAdvanceInput(InputAction.CallbackContext context)
    {
        // El clic del ratón ya lo gestiona OnDialogueClicked. Si la acción también está
        // enlazada al ratón, cada clic avanzaba DOS líneas (una al pulsar y otra al soltar).
        if (context.control != null && context.control.device is Pointer)
            return;

        TryAdvance();
    }

    private void TryAdvance()
    {
        if (!IsShowing)
            return;

        // Segunda protección: nunca más de un avance por frame.
        if (Time.frameCount == lastAdvanceFrame)
            return;
        lastAdvanceFrame = Time.frameCount;

        if (isTyping)
        {
            SkipTyping();
            return;
        }

        if (choiceEntries.Count > 0)
            return;

        if (evidencePicker != null && evidencePicker.IsShowing)
            return;

        if (UIT_JournalController.Instance != null && UIT_JournalController.Instance.IsOpen)
            return;

        DialogueManager.Instance.Next();
    }

    private static bool IsInsideButton(VisualElement element)
    {
        for (var current = element; current != null; current = current.parent)
        {
            // Opciones ocultas (desactivadas): un clic ahí avanza el diálogo como en el resto del panel.
            if (!current.enabledSelf)
                return false;

            if (current is Button || current is ListView)
                return true;
        }
        return false;
    }

    private void OnJournalButtonClicked()
    {
        UIT_JournalController.Instance?.ToggleJournal();
    }

    // =========================
    // TYPEWRITER
    // =========================
    private void StartTyping(string text)
    {
        StopTyping();
        typingCoroutine = StartCoroutine(TypeText(text));
    }

    private IEnumerator TypeText(string text)
    {
        isTyping = true;
        fullText = text;
        SetContinueVisible(false);

        // Realtime: si algo pausa el juego (timeScale = 0) el texto sigue escribiéndose.
        var wait = new WaitForSecondsRealtime(typingSpeed);

        for (int i = 1; i <= text.Length; i++)
        {
            dialogueLabel.text = BuildPartialText(text, i);

            char c = text[i - 1];
            if (c != ' ' && UnityEngine.Random.value < 0.35f && !string.IsNullOrEmpty(dialogueEventPath))
                RuntimeManager.PlayOneShot(dialogueEventPath);

            yield return wait;
        }

        FinishTyping();
    }

    private string BuildPartialText(string text, int visibleCount)
    {
        if (!preventWordJump || visibleCount >= text.Length)
            return text.Substring(0, visibleCount);

        // El resto se pinta transparente: el texto ocupa ya su sitio final y no salta de línea.
        typingBuilder.Clear();
        typingBuilder.Append(text, 0, visibleCount);
        typingBuilder.Append("<alpha=#00>");
        typingBuilder.Append(text, visibleCount, text.Length - visibleCount);
        return typingBuilder.ToString();
    }

    private void SkipTyping()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        dialogueLabel.text = fullText;
        FinishTyping();
    }

    private void FinishTyping()
    {
        typingCoroutine = null;
        isTyping = false;
        SetContinueVisible(choiceEntries.Count == 0);
    }

    private void StopTyping()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = null;
        isTyping = false;
    }

    // =========================
    // CHOICES
    // =========================
    private void RefreshChoices()
    {
        if (!isBound)
            return;

        if (choicesList != null)
        {
            choicesList.RefreshItems();
        }
        else
        {
            while (choiceButtonPool.Count < choiceEntries.Count)
            {
                var button = CreateChoiceButton();
                choiceButtonPool.Add(button);
                choicesContainer.Add(button);
            }

            for (int i = 0; i < choiceButtonPool.Count; i++)
            {
                bool used = i < choiceEntries.Count;
                choiceButtonPool[i].style.display = used ? DisplayStyle.Flex : DisplayStyle.None;
                if (used)
                    BindChoiceButton(choiceButtonPool[i], i);
            }
        }

        bool hasChoices = choiceEntries.Count > 0;
        SetChoicesVisible(hasChoices);
        SetContinueVisible(!hasChoices && !isTyping && IsShowing);
    }

    /// <summary>
    /// Crea una opción. Con plantilla: ButtonReplyDialogue_Template (botón + label con el texto).
    /// El índice de la opción se guarda en userData de la raíz del elemento.
    /// </summary>
    private VisualElement CreateChoiceButton()
    {
        VisualElement root;
        Button button;

        if (replyButtonTemplate != null)
        {
            root = replyButtonTemplate.CloneTree();
            button = root.Q<Button>(replyButtonName) ?? root.Q<Button>();
            if (button == null)
                Debug.LogError($"UIT_DialogueUI: la plantilla de respuesta no tiene ningún Button '{replyButtonName}'.", this);
        }
        else
        {
            button = new Button();
            root = button;
        }

        root.AddToClassList(ChoiceClass);
        if (button != null)
            button.clicked += () => OnChoiceClicked(root);
        return root;
    }

    private void BindChoiceButton(VisualElement root, int index)
    {
        var entry = choiceEntries[index];
        root.userData = index;
        root.EnableInClassList(ChoiceSeenClass, entry.Seen);

        Button button = root as Button ?? root.Q<Button>(replyButtonName) ?? root.Q<Button>();
        Label label = root.Q<Label>(replyLabelName);

        // El texto va en el label de la plantilla; si no hay, en el propio botón.
        if (label != null)
        {
            label.text = entry.Text;
            if (button != null)
                button.text = string.Empty;
        }
        else if (button != null)
        {
            button.text = entry.Text;
        }

        // Rojo para los temas ya hablados, color normal para el resto. Se pinta en el label
        // porque la plantilla le pone el color directamente. Siempre se asigna uno de los dos:
        // el ListView reutiliza elementos y si no, un rojo antiguo se quedaría en otra opción.
        var colorTarget = (VisualElement)label ?? button;
        if (colorTarget != null)
            colorTarget.style.color = entry.Seen ? seenTopicColor : unseenTopicColor;
    }

    private void OnChoiceClicked(VisualElement root)
    {
        if (root.userData is not int index || index < 0 || index >= choiceEntries.Count)
            return;

        // Se copia antes: la acción suele limpiar las opciones.
        Action onSelected = choiceEntries[index].OnSelected;
        onSelected?.Invoke();
    }

    private void SetChoicesVisible(bool visible)
    {
        choicesContainer?.SetEnabled(visible);
    }

    // =========================
    // CHARACTERS (antes CharacterUIController)
    // =========================
    private void SetCharacter(DialogueCharacter character, Sprite expressionSprite)
    {
        if (character == null)
            return;

        VisualElement speakerSide = character.isLeftSide ? leftCharacter : rightCharacter;
        VisualElement otherSide = character.isLeftSide ? rightCharacter : leftCharacter;

        Sprite sprite = expressionSprite != null ? expressionSprite : character.portrait;

        if (speakerSide != null)
        {
            speakerSide.style.backgroundImage = sprite != null
                ? new StyleBackground(sprite)
                : new StyleBackground(StyleKeyword.None);
            speakerSide.EnableInClassList(CharacterHiddenClass, sprite == null);
            speakerSide.EnableInClassList(CharacterActiveClass, true);
            speakerSide.EnableInClassList(CharacterInactiveClass, false);
        }

        // El otro personaje se queda en pantalla, oscurecido (si ya había alguien en ese lado).
        if (otherSide != null)
        {
            otherSide.EnableInClassList(CharacterActiveClass, false);
            otherSide.EnableInClassList(CharacterInactiveClass, true);
        }
    }

    private void ResetCharacters()
    {
        ResetCharacter(leftCharacter);
        ResetCharacter(rightCharacter);
    }

    private static void ResetCharacter(VisualElement element)
    {
        if (element == null)
            return;

        element.style.backgroundImage = new StyleBackground(StyleKeyword.None);
        element.RemoveFromClassList(CharacterActiveClass);
        element.RemoveFromClassList(CharacterInactiveClass);
        element.AddToClassList(CharacterHiddenClass);
    }

    // =========================
    // HELPERS
    // =========================
    private void SetSpeaker(string speaker)
    {
        if (speakerLabel == null)
            return;

        speakerLabel.text = speaker ?? string.Empty;
        speakerLabel.style.visibility = string.IsNullOrEmpty(speaker) ? Visibility.Hidden : Visibility.Visible;
    }

    private void SetContinueVisible(bool visible)
    {
        if (continueIndicator != null)
            continueIndicator.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void SetVisible(bool visible)
    {
        IsShowing = visible;

        // Se oculta todo el documento (incluye BT_JournalButton, que está fuera de Dialogue_Root).
        // Nunca SetActive(false): destruiría el árbol visual.
        if (documentRoot != null)
            documentRoot.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // =========================
    // EVIDENCE
    // =========================
    private void HandleEvidenceSelected(ItemSO item)
    {
        evidencePicker.Hide();
        OnEvidenceItemSelected?.Invoke(item);
    }
}