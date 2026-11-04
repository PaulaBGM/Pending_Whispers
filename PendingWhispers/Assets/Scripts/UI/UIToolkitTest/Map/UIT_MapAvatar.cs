using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Avatar del mapa en UI Toolkit (sustituye a MapAvatar).
/// Es un VisualElement con position: absolute que se mueve de waypoint en waypoint.
/// Como en UI Toolkit no hay Animator, la animación es una lista de fotogramas:
/// idle cuando está quieto, walk mientras se mueve, y se voltea al ir a la izquierda.
/// </summary>
public class UIT_MapAvatar : MonoBehaviour
{
    [Header("Movimiento")]
    [Tooltip("Píxeles de panel por segundo (con Reference Resolution 1920x1080).")]
    [SerializeField] private float speed = 400f;

    [Header("Animación")]
    [SerializeField] private Sprite idleSprite;
    [SerializeField] private Sprite[] walkFrames;
    [SerializeField] private float framesPerSecond = 8f;
    [Tooltip("Si los sprites miran a la derecha, se voltean al ir a la izquierda.")]
    [SerializeField] private bool flipWhenMovingLeft = true;

    public bool IsMoving { get; private set; }

    private VisualElement avatar;
    private Coroutine routine;
    private Vector2 position;
    private bool facingLeft;

    public void Bind(VisualElement avatarElement)
    {
        avatar = avatarElement;
        if (avatar == null)
            return;

        avatar.pickingMode = PickingMode.Ignore;
        avatar.style.position = Position.Absolute;
        ShowIdle();
    }

    // =========================
    // POSITION
    // =========================
    public void SetPosition(VisualElement waypoint)
    {
        if (avatar == null || waypoint == null)
            return;

        StopMoving();
        position = TargetFor(waypoint);
        ApplyPosition();
        ShowIdle();
    }

    // =========================
    // PATH
    // =========================
    public void FollowPath(List<VisualElement> path, Action onArrived)
    {
        StopMoving();

        if (avatar == null || path == null || path.Count == 0)
        {
            onArrived?.Invoke();
            return;
        }

        routine = StartCoroutine(FollowRoutine(path, onArrived));
    }

    private IEnumerator FollowRoutine(List<VisualElement> path, Action onArrived)
    {
        IsMoving = true;
        float frameTimer = 0f;
        int frame = 0;

        // El primer punto es donde ya está el avatar.
        for (int i = 1; i < path.Count; i++)
        {
            if (path[i] == null)
                continue;

            // Se recalcula cada frame por si cambia el tamaño de la ventana durante el viaje.
            while (true)
            {
                Vector2 target = TargetFor(path[i]);
                Vector2 delta = target - position;

                if (delta.sqrMagnitude <= 1f)
                {
                    position = target;
                    ApplyPosition();
                    break;
                }

                if (flipWhenMovingLeft && Mathf.Abs(delta.x) > 0.5f)
                    SetFacingLeft(delta.x < 0f);

                // Unscaled, como el original: el mapa funciona aunque el juego esté en pausa.
                position = Vector2.MoveTowards(position, target, speed * Time.unscaledDeltaTime);
                ApplyPosition();

                frameTimer += Time.unscaledDeltaTime;
                if (walkFrames != null && walkFrames.Length > 0 && frameTimer >= 1f / framesPerSecond)
                {
                    frameTimer = 0f;
                    frame = (frame + 1) % walkFrames.Length;
                    SetSprite(walkFrames[frame]);
                }

                yield return null;
            }
        }

        routine = null;
        IsMoving = false;
        ShowIdle();
        onArrived?.Invoke();
    }

    private void StopMoving()
    {
        if (routine != null)
            StopCoroutine(routine);

        routine = null;
        IsMoving = false;
    }

    // =========================
    // HELPERS
    // =========================

    /// <summary>Esquina superior izquierda del avatar para que su centro quede sobre el waypoint.</summary>
    private Vector2 TargetFor(VisualElement waypoint)
    {
        VisualElement parent = avatar.parent;
        Vector2 center = parent.WorldToLocal(waypoint.worldBound.center);

        float width = avatar.resolvedStyle.width;
        float height = avatar.resolvedStyle.height;
        if (float.IsNaN(width)) width = 0f;
        if (float.IsNaN(height)) height = 0f;

        return center - new Vector2(width * 0.5f, height * 0.5f);
    }

    private void ApplyPosition()
    {
        avatar.style.left = position.x;
        avatar.style.top = position.y;
    }

    private void SetFacingLeft(bool left)
    {
        if (facingLeft == left)
            return;

        facingLeft = left;
        avatar.style.scale = new Scale(new Vector3(left ? -1f : 1f, 1f, 1f));
    }

    private void ShowIdle()
    {
        if (idleSprite != null)
            SetSprite(idleSprite);
        else if (walkFrames != null && walkFrames.Length > 0)
            SetSprite(walkFrames[0]);
    }

    private void SetSprite(Sprite sprite)
    {
        if (avatar != null && sprite != null)
            avatar.style.backgroundImage = new StyleBackground(sprite);
    }
}
