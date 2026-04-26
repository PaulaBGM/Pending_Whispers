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
        Instance = this;
        panel.SetActive(false);

        DontDestroyOnLoad(gameObject);
    }

    public void ShowMessage(string message)
    {
        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(ShowRoutine(message));
    }

    IEnumerator ShowRoutine(string message)
    {
        panel.SetActive(true);
        text.text = message;

        yield return new WaitForSeconds(duration);

        panel.SetActive(false);
    }
}