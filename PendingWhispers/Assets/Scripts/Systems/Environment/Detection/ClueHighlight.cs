using UnityEngine;
using System.Collections;

public class ClueHighlight : MonoBehaviour
{
    [SerializeField] private SpriteRenderer highlightRenderer;
    [SerializeField] private float duration = 3f;

    private Coroutine currentRoutine;

    private void Awake()
    {
        if (highlightRenderer != null)
            highlightRenderer.enabled = false;
    }

    public void Show()
    {
        if (highlightRenderer == null)
            return;

        if (currentRoutine != null)
            StopCoroutine(currentRoutine);

        currentRoutine = StartCoroutine(ShowRoutine());
    }

    private IEnumerator ShowRoutine()
    {
        highlightRenderer.enabled = true;

        yield return new WaitForSeconds(duration);

        highlightRenderer.enabled = false;
    }
}