using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System.Collections.Generic;
using FMOD.Studio;
using FMODUnity;

public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance;

    [Header("UI")]
    public GameObject panel;

    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI dialogueText;

    public Transform choicesContainer;
    public GameObject choiceButtonPrefab;

    [Header("Typewriter")]
    public float typingSpeed = 0.03f;

    [Header("Topic Selector")]
    [SerializeField] private string topicSelectorSpeaker = "Talk";
    [SerializeField] private string seenTopicPrefix = "✓  ";
    [SerializeField] private Color seenTopicColor = new Color(0.55f, 0.12f, 0.24f, 1f);
    [SerializeField] private Color unseenTopicColor = Color.white;

    private readonly List<GameObject> choiceButtonPool = new();
    private Coroutine typingCoroutine;
    private bool isTyping;
    private string fullText;
    private int visibleChoiceCount;
    
    [Header("FMOD")]
    public string dialogueEventPath = "event:/Dialogue";

    void Awake()
    {
        Instance = this;
        panel.SetActive(false);
    }

    void Update()
    {
        if (!panel.activeSelf)
            return;

        if (Input.GetMouseButtonDown(0))
        {
            // Si el texto se est� escribiendo, completarlo
            if (isTyping)
            {
                SkipTyping();
                return;
            }

            // Si hay elecciones visibles, no avanzar
            if (visibleChoiceCount > 0)
                return;

            // Avanzar al siguiente nodo
            DialogueManager.Instance.Next();
        }
    }

    public void ShowLine(DialogueCharacter character, string speaker, string text, Sprite expressionSprite )
    {
        panel.SetActive(true);

        speakerText.text = speaker;

        if (CharacterUIController.Instance != null)
        {
            CharacterUIController.Instance.SetCharacter(character,expressionSprite);
        }
        
        StartTyping(text);
    }

    void StartTyping(string text)
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        typingCoroutine = StartCoroutine(TypeText(text));
    }

    IEnumerator TypeText(string text)
    {
        isTyping = true;
        fullText = text;
        dialogueText.text = "";

        foreach (char c in text)
        {
            dialogueText.text += c;
            
            if (c != ' ' && Random.value < 0.35f)
            {
                RuntimeManager.PlayOneShot(dialogueEventPath);
            }

            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
    }

    void SkipTyping()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        dialogueText.text = fullText;
        isTyping = false;
    }

    public void ShowChoices(List<DialogueChoice> choices)
    {
        ClearChoices();

        foreach (var choice in choices)
        {
            GameObject btn = GetChoiceButton();

            btn.GetComponentInChildren<TextMeshProUGUI>().text = choice.text;

            btn.GetComponent<Button>().onClick.AddListener(() =>
            {
                DialogueManager.Instance.ChooseChoice(choice);
            });
        }
    }

    public void ShowDialogueSelector(List<DialogueCondition> topics, Func<DialogueCondition, bool> isTopicSeen, Action<DialogueCondition> onTopicSelected)
    {
        panel.SetActive(true);
        speakerText.text = topicSelectorSpeaker;
        dialogueText.text = string.Empty;

        if (CharacterUIController.Instance != null)
            CharacterUIController.Instance.ResetCharacters();

        ClearChoices();

        foreach (DialogueCondition topic in topics)
        {
            GameObject btn = GetChoiceButton();
            TextMeshProUGUI topicText = btn.GetComponentInChildren<TextMeshProUGUI>();
            bool wasSeen = !topic.hideSeenCheckmark && (isTopicSeen?.Invoke(topic) ?? false);

            if (topicText != null)
            {
                topicText.text = wasSeen ? $"{seenTopicPrefix}{topic.DisplayTitle}" : topic.DisplayTitle;
                topicText.color = wasSeen ? seenTopicColor : unseenTopicColor;
            }

            btn.GetComponent<Button>().onClick.AddListener(() =>
            {
                onTopicSelected?.Invoke(topic);
            });
        }
    }

    public void Hide()
    {
        panel.SetActive(false);

        ClearChoices();

        if (CharacterUIController.Instance != null)
            CharacterUIController.Instance.ResetCharacters();
    }

    public void ClearChoices()
    {
        for (int i = 0; i < visibleChoiceCount; i++)
        {
            if (choiceButtonPool[i].TryGetComponent(out Button button))
                button.onClick.RemoveAllListeners();

            choiceButtonPool[i].SetActive(false);
        }

        visibleChoiceCount = 0;
    }

    private GameObject GetChoiceButton()
    {
        GameObject buttonObject;

        if (visibleChoiceCount < choiceButtonPool.Count)
        {
            buttonObject = choiceButtonPool[visibleChoiceCount];
        }
        else
        {
            buttonObject = Instantiate(choiceButtonPrefab, choicesContainer);
            choiceButtonPool.Add(buttonObject);
        }

        buttonObject.SetActive(true);
        buttonObject.transform.SetSiblingIndex(visibleChoiceCount);
        visibleChoiceCount++;

        if (buttonObject.TryGetComponent(out Button button))
            button.onClick.RemoveAllListeners();

        return buttonObject;
    }
}
