using UnityEngine;
using TMPro;
using System.Collections;

public class IntroSequenceUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI introText;

    [Header("Texto Intro")]
    [TextArea(3, 8)]
    [SerializeField] private string[] messages;

    [Header("Flags")]
    [SerializeField] private FlagSO introSeenFlag;

    [Header("Animación")]
    [SerializeField] private float fadeDuration = 1f;

    [SerializeField] private float textDuration = 3f;

    [SerializeField] private bool allowSkip = true;

    private PlayerController player;

    private bool isRunning;

    void Start()
    {
        gameObject.SetActive(true);

        canvasGroup.alpha = 0;

        // Si ya vio la intro, no mostrarla otra vez
        if (introSeenFlag != null &&
            GameProgress.Instance.HasFlag(introSeenFlag))
        {
            gameObject.SetActive(false);
            return;
        }

        player = FindFirstObjectByType<PlayerController>();

        if (player != null)
            player.canMove = false;

        StartCoroutine(IntroRoutine());
    }

    void Update()
    {
        if (!isRunning) return;

        if (allowSkip && Input.GetMouseButtonDown(0))
        {
            StopAllCoroutines();
            EndIntro();
        }
    }

    IEnumerator IntroRoutine()
    {
        isRunning = true;

        foreach (var msg in messages)
        {
            // Texto actual
            introText.text = msg;

            // Fade In
            yield return StartCoroutine(Fade(0, 1));

            // Espera
            yield return new WaitForSeconds(textDuration);

            // Fade Out
            yield return StartCoroutine(Fade(1, 0));

            yield return new WaitForSeconds(0.2f);
        }

        EndIntro();
    }

    IEnumerator Fade(float from, float to)
    {
        float timer = 0f;

        while (timer < fadeDuration)
        {
            timer += Time.deltaTime;

            canvasGroup.alpha = Mathf.Lerp(
                from,
                to,
                timer / fadeDuration
            );

            yield return null;
        }

        canvasGroup.alpha = to;
    }

    void EndIntro()
    {
        isRunning = false;

        // Guardar flag
        if (introSeenFlag != null)
        {
            GameProgress.Instance.AddFlag(introSeenFlag);
        }

        // Devolver control al jugador
        if (player != null)
            player.canMove = true;

        gameObject.SetActive(false);
    }
}