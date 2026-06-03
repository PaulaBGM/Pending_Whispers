using UnityEngine;
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

    private Coroutine typingCoroutine;
    private bool isTyping;
    private string fullText;
    
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
            if (choicesContainer.childCount > 0)
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
            GameObject btn = Instantiate(choiceButtonPrefab, choicesContainer);

            btn.GetComponentInChildren<TextMeshProUGUI>().text = choice.text;

            btn.GetComponent<UnityEngine.UI.Button>().onClick.AddListener(() =>
            {
                DialogueManager.Instance.ChooseChoice(choice);
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
        for (int i = choicesContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(choicesContainer.GetChild(i).gameObject);
        }
    }
}