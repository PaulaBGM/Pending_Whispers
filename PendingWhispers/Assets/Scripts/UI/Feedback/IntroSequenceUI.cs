using UnityEngine;
using TMPro;
using System.Collections;

public class IntroSequenceUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private TextMeshProUGUI introText;

    [Header("Texto")]
    [TextArea(4, 8)]
    [SerializeField] private string[] messages;

    [Header("Flags")]
    [SerializeField] private FlagSO introSeenFlag;

    [Header("Settings")]
    [SerializeField] private float fadeDuration = 1f;
    [SerializeField] private float textDuration = 4f;

    private PlayerController player;

    void Start()
    {
        // Si ya vio la intro, no mostrar nada
        if (GameProgress.Instance.HasFlag(introSeenFlag))
        {
            gameObject.SetActive(false);
            return;
        }

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

        // Marcar intro como vista
        GameProgress.Instance.AddFlag(introSeenFlag);

        if (player != null)
            player.canMove = true;
        TutorialPopup.Instance.ShowTutorialOnce("intro", "Basic Controls", "Click to move.\nApproach objects and characters to interact with them.\n\nTAB: Journal\nM: Map\nESC: Pause");
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