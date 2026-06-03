using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using FMODUnity;

public class CameraFlash : MonoBehaviour
{
    public static CameraFlash Instance;

    [Header("UI")]
    [SerializeField] private Image flashImage;
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float maxAlpha = 1f;

    [Header("FMOD")]
    [SerializeField] private EventReference flashEvent;

    private Coroutine flashCoroutine;

    private void Awake()
    {
        Instance = this;

        SetAlpha(0f);
    }

    public void PlayFlash()
    {
        RuntimeManager.PlayOneShot(flashEvent);

        if (flashCoroutine != null)
            StopCoroutine(flashCoroutine);

        flashCoroutine = StartCoroutine(FlashRoutine());
    }

    private IEnumerator FlashRoutine()
    {
        // ⚡ subir instantáneo (flash)
        SetAlpha(maxAlpha);

        float t = 0f;

        // 🌫️ fade out progresivo
        while (t < fadeDuration)
        {
            t += Time.deltaTime;

            float a = Mathf.Lerp(maxAlpha, 0f, t / fadeDuration);
            SetAlpha(a);

            yield return null;
        }

        SetAlpha(0f);
    }

    private void SetAlpha(float a)
    {
        Color c = flashImage.color;
        c.a = a;
        flashImage.color = c;
    }
}