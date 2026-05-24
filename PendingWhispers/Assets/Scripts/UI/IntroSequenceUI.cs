using UnityEngine;
using TMPro;
using System.Collections;

public class IntroSequenceUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI introText;

    [Header("Settings")]
    [TextArea(4, 8)]
    [SerializeField] private string[] messages;

    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private float textDuration = 4f;

    private PlayerController player;

    void Start()
    {
        player = FindFirstObjectByType<PlayerController>();

        if (player != null)
            player.canMove = false;

        StartCoroutine(IntroRoutine());
    }

    IEnumerator IntroRoutine()
    {
        yield return Fade(0, 1);

        foreach (var msg in messages)
        {
            introText.text = msg;

            yield return new WaitForSeconds(textDuration);
        }

        yield return Fade(1, 0);

        if (player != null)
            player.canMove = true;

        gameObject.SetActive(false);
    }

    IEnumerator Fade(float from, float to)
    {
        float t = 0;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;

            canvasGroup.alpha = Mathf.Lerp(from, to, t / fadeDuration);

            yield return null;
        }

        canvasGroup.alpha = to;
    }
}