using UnityEngine;
using TMPro;
using System.Collections;

public class UIFeedbackManager : MonoBehaviour
{
    public static UIFeedbackManager Instance;

    public GameObject panel;
    public TextMeshProUGUI text;

    public float duration = 2f;

    private Coroutine currentRoutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        if (transform.parent != null)
            transform.SetParent(null);

        DontDestroyOnLoad(gameObject);

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
        ShowMessage("Nueva localizaci�n desbloqueada: " + locationName);
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