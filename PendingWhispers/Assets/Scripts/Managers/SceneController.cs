using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Cambia de escena con un fundido a negro: oscurece, carga la escena nueva,
/// espera a que se coloque (jugador, cámara, SpawnPoint...) y vuelve a aclarar.
/// La pantalla negra se crea sola por código: no hace falta montar nada en la escena.
/// Usa tiempo sin escalar para que funcione también con el juego en pausa.
/// </summary>
public class SceneController : BaseSingleton<SceneController>
{
    [Header("Fundido")]
    [SerializeField, Min(0f)] private float fadeOutDuration = 0.35f;
    [SerializeField, Min(0f)] private float fadeInDuration = 0.35f;
    [SerializeField] private Color fadeColor = Color.black;

    /// <summary>Escena desde la que se hizo el ultimo LoadScene (p. ej. para saber desde donde se abrio el mapa).</summary>
    public string PreviousScene { get; private set; }

    /// <summary>True mientras dura el fundido + la carga.</summary>
    public bool IsTransitioning { get; private set; }

    private CanvasGroup fadeGroup;

    public void LoadScene(string scene)
    {
        // Evita dobles cargas (p. ej. dos clics seguidos en una puerta o en el mapa).
        if (IsTransitioning)
        {
            Debug.LogWarning($"[Scene] Ya hay un cambio de escena en curso; se ignora '{scene}'.");
            return;
        }

        Debug.Log("[Scene] Loading: " + scene);
        PreviousScene = SceneManager.GetActiveScene().name;
        StartCoroutine(TransitionRoutine(scene));
    }

    private IEnumerator TransitionRoutine(string scene)
    {
        IsTransitioning = true;
        EnsureFadeOverlay();

        // Mientras está en negro no se puede hacer clic en nada.
        fadeGroup.blocksRaycasts = true;
        yield return Fade(0f, 1f, fadeOutDuration);

        AsyncOperation load = SceneManager.LoadSceneAsync(scene);
        if (load == null)
        {
            Debug.LogError($"[Scene] No se pudo cargar '{scene}'. ¿Está añadida en Build Settings?");
        }
        else
        {
            while (!load.isDone)
                yield return null;

            // Un frame más para que los Start() de la escena nueva coloquen jugador y cámara
            // antes de que se vea nada.
            yield return null;
        }

        yield return Fade(1f, 0f, fadeInDuration);
        fadeGroup.blocksRaycasts = false;
        IsTransitioning = false;
    }

    private IEnumerator Fade(float from, float to, float duration)
    {
        if (duration <= 0f)
        {
            fadeGroup.alpha = to;
            yield break;
        }

        float t = 0f;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            fadeGroup.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / duration));
            yield return null;
        }

        fadeGroup.alpha = to;
    }

    /// <summary>Crea (una sola vez) un Canvas encima de todo con una imagen a pantalla completa.</summary>
    private void EnsureFadeOverlay()
    {
        if (fadeGroup != null)
            return;

        var root = new GameObject("SceneFade");
        root.transform.SetParent(transform, false); // hijo del SceneController: sobrevive al cambio de escena

        var canvas = root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = short.MaxValue; // por encima de cualquier otro Canvas
        root.AddComponent<GraphicRaycaster>(); // para que bloquee los clics durante el fundido

        fadeGroup = root.AddComponent<CanvasGroup>();
        fadeGroup.alpha = 0f;
        fadeGroup.blocksRaycasts = false;
        fadeGroup.interactable = false;

        var imageObject = new GameObject("Black");
        imageObject.transform.SetParent(root.transform, false);

        var image = imageObject.AddComponent<Image>();
        image.color = fadeColor;

        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
