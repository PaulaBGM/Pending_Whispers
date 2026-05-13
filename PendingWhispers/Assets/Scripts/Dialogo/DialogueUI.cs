using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

public class DialogueUI : MonoBehaviour
{
    public static DialogueUI Instance;

    public GameObject panel;

    public TextMeshProUGUI speakerText;
    public TextMeshProUGUI dialogueText;

    public Transform choicesContainer;
    public GameObject choiceButtonPrefab;

    public Button continueButton;

    [Header("Typewriter")]
    public float typingSpeed = 0.03f;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip blipSound;

    private Coroutine typingCoroutine;
    private bool isTyping;
    private string fullText;

    void Awake()
    {
        Instance = this;
        panel.SetActive(false);
    }

    void Update()
    {
        if (!panel.activeSelf) return;

        if (Input.GetMouseButtonDown(0))
        {
            if (isTyping)
                SkipTyping();
        }
    }

    public void ShowLine(DialogueCharacter character, string speaker, string text)
    {
        panel.SetActive(true);

        speakerText.text = speaker;

        CharacterUIController.Instance.SetCharacter(character);

        StartTyping(text);

        continueButton.gameObject.SetActive(false);
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

            if (blipSound != null && audioSource != null)
                audioSource.PlayOneShot(blipSound);

            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;

        ShowContinue();
    }

    void SkipTyping()
    {
        if (typingCoroutine != null)
            StopCoroutine(typingCoroutine);

        dialogueText.text = fullText;
        isTyping = false;

        ShowContinue();
    }

    public void ShowContinue()
    {
        continueButton.gameObject.SetActive(true);
    }

    public void ShowChoices(List<DialogueChoice> choices)
    {
        ClearChoices();
        continueButton.gameObject.SetActive(false);

        foreach (var choice in choices)
        {
            GameObject btn = Instantiate(choiceButtonPrefab, choicesContainer);

            btn.GetComponentInChildren<TextMeshProUGUI>().text = choice.text;

            btn.GetComponent<Button>().onClick.AddListener(() =>
            {
                DialogueManager.Instance.ChooseChoice(choice);
            });
        }
    }

    public void Hide()
    {
        panel.SetActive(false);

        ClearChoices(); 

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