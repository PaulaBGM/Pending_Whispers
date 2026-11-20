using FMODUnity;
using UnityEngine;

/// <summary>
/// Puerta/escalera que lleva a otra escena sin pasar por el mapa (p. ej. casa -> sótano).
/// Se interactúa como con cualquier objeto: clic, el jugador se acerca y se usa.
///
/// Puede estar bloqueada por una flag: hasta que el jugador la tenga
/// (p. ej. al resolver el puzzle), solo muestra el mensaje de bloqueo.
/// En la escena de destino tiene que haber un SpawnPoint con el id indicado.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SceneDoor : MonoBehaviour, IInteractable, IHighlightable
{
    [Header("Destino")]
    [Tooltip("Nombre exacto de la escena (y añadida en Build Settings).")]
    [SerializeField] private string targetScene;
    [Tooltip("Id del SpawnPoint de la escena de destino donde aparece el jugador.")]
    [SerializeField] private string targetSpawnId;

    [Header("Bloqueo")]
    [Tooltip("Flag necesaria para poder pasar. Vacío = siempre abierta.")]
    [SerializeField] private FlagSO requiredFlag;
    [Tooltip("Mensaje si se intenta pasar sin la flag.")]
    [SerializeField, TextArea] private string lockedMessage = "It's locked.";

    [Header("Highlight")]
    [Tooltip("Sprite que se tiñe al pasar el ratón. Si se deja vacío se usa el primero de los hijos.")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color highlightColor = Color.cyan;

    [Header("FMOD")]
    [Tooltip("Opcional. Sonido al resaltar.")]
    [SerializeField] private EventReference highlightEvent;

    private Color originalColor;
    private bool isHighlighted;

    public bool IsUnlocked =>
        requiredFlag == null || (GameProgress.Instance != null && GameProgress.Instance.HasFlag(requiredFlag));

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
            originalColor = spriteRenderer.color;
    }

    public void Interact(PlayerController_Actions player)
    {
        if (!IsUnlocked)
        {
            if (!string.IsNullOrEmpty(lockedMessage))
                UIFeedbackManager.Instance?.ShowMessage(lockedMessage);
            return;
        }

        if (string.IsNullOrEmpty(targetScene))
        {
            Debug.LogError($"[SceneDoor] '{name}' no tiene escena de destino.", this);
            return;
        }

        SpawnPoint.SetPending(targetScene, targetSpawnId);
        SceneController.Instance.LoadScene(targetScene);
    }

    public Transform GetTransform() => transform;

    public void SetHighlight(bool value)
    {
        if (spriteRenderer == null || value == isHighlighted)
            return;

        spriteRenderer.color = value ? highlightColor : originalColor;
        isHighlighted = value;

        if (value && !highlightEvent.IsNull)
            RuntimeManager.PlayOneShot(highlightEvent, transform.position);
    }

    private void OnDisable()
    {
        SetHighlight(false);
    }
}
