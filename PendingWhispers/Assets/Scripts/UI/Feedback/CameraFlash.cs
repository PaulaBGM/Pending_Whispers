using FMODUnity;
using Inventory;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class CameraFlash : BaseSingleton<CameraFlash>
{
    [Header("UI")]
    [SerializeField] private Image flashImage;
    [SerializeField] private float fadeDuration = 0.3f;
    [SerializeField] private float maxAlpha = 1f;
    [Header("FMOD")]
    [SerializeField] private EventReference flashEvent;
    protected override bool PersistAcrossScenes => false;
    private Coroutine flashCoroutine;

    protected override void Awake()
    {
        base.Awake();
        if (Instance != this)
            return;
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
        SetAlpha(maxAlpha);
        float t = 0f;
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