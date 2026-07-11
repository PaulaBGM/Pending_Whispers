using UnityEngine;
using TMPro;
using System.Collections;

public class UIFeedbackManager : BaseSingleton<UIFeedbackManager>
{

    public GameObject panel;
    public TextMeshProUGUI text;

    public float duration = 2f;

    private Coroutine currentRoutine;

    protected override void Awake()
    {
        if (transform.parent != null)
            transform.SetParent(null);

        base.Awake();

        if (Instance != this)
            return;

        if (panel != null)
            panel.SetActive(false);
    }

    void OnEnable()
    {
        UIGameEvents.OnFeedback += ShowMessage;
        UIGameEvents.OnLocationUnlocked += OnLocationUnlocked;
    }

    void OnDisable()
    {
        UIGameEvents.OnFeedback -= ShowMessage;
        UIGameEvents.OnLocationUnlocked -= OnLocationUnlocked;
    }

    void OnLocationUnlocked(string locationName)
    {
        ShowMessage("Nueva localizacion desbloqueada: " + locationName);
    }

    public void ShowMessage(string message)
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(ShowRoutine(message));
    }

    IEnumerator ShowRoutine(string message)
    {
        if (panel == null || text == null)
            yield break;

        panel.SetActive(true);
        text.text = message;

        yield return new WaitForSeconds(duration);

        if (panel != null)
            panel.SetActive(false);
    }
}