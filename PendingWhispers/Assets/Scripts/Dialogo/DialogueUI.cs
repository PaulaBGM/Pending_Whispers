using UnityEngine;
using TMPro;
using UnityEngine.UI;
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

    void Awake()
    {
        Instance = this;
        panel.SetActive(false);
    }

    public void ShowDialogue(string speaker, string text)
    {
        panel.SetActive(true);

        speakerText.text = speaker;
        dialogueText.text = text;

        ClearChoices();
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
    }

    void ClearChoices()
    {
        foreach (Transform child in choicesContainer)
        {
            Destroy(child.gameObject);
        }
    }
}