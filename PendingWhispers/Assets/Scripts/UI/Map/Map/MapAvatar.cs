using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class MapAvatar : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Velocidad en píxeles de referencia por segundo (Canvas Scaler a 1920x1080). Igual en cualquier resolución.")]
    [SerializeField] private float speed = 400f;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    private RectTransform rectTransform;
    private Coroutine routine;

    public bool IsMoving { get; private set; }

    public event Action OnDestinationReached;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    // =========================================================
    // POSITION
    // =========================================================

    public void SetPosition(MapWaypoint waypoint)
    {
        if (waypoint == null)
        {
            Debug.LogError("[MapAvatar] Waypoint NULL.",this);
            return;
        }

        rectTransform.position = waypoint.RectTransform.position;

        StopAnimation();
    }

    // =========================================================
    // PATH
    // =========================================================

    public void FollowPath(List<MapWaypoint> path)
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        if (path == null || path.Count == 0)
        {
            IsMoving = false;
            StopAnimation();
            return;
        }

        if (path.Count == 1)
        {
            SetPosition(path[0]);

            IsMoving = false;
            OnDestinationReached?.Invoke();

            return;
        }

        routine = StartCoroutine(FollowRoutine(path));
    }

    // =========================================================
    // MOVEMENT
    // =========================================================

    private IEnumerator FollowRoutine(List<MapWaypoint> path)
    {
        IsMoving = true;

        for (int i = 1; i < path.Count; i++)
        {
            MapWaypoint waypoint = path[i];

            if (waypoint == null)
                continue;

            // Se mueve en el espacio local del padre (unidades del Canvas, que el CanvasScaler
            // ajusta a la resolución de referencia 1920x1080). Antes se movía en
            // rectTransform.position, que en un Canvas Overlay son píxeles de PANTALLA:
            // la velocidad cambiaba según la resolución (más rápido en ventanas pequeñas,
            // más lento en 4K).
            Transform parent = rectTransform.parent;
            Vector3 target = parent != null
                ? parent.InverseTransformPoint(waypoint.RectTransform.position)
                : waypoint.RectTransform.position;

            while ((rectTransform.localPosition - target).sqrMagnitude > 0.01f)
            {
                Vector3 current = rectTransform.localPosition;
                Vector2 direction = target - current;

                if (direction.sqrMagnitude > 0.001f)
                    direction.Normalize();

                UpdateAnimator(direction);

                // Unscaled: el mapa funciona aunque el juego esté en pausa.
                rectTransform.localPosition = Vector3.MoveTowards(
                    current,
                    target,
                    speed * Time.unscaledDeltaTime);

                yield return null;

                // Por si el waypoint se mueve (p. ej. cambio de tamaño de ventana durante el viaje).
                if (parent != null)
                    target = parent.InverseTransformPoint(waypoint.RectTransform.position);
            }

            rectTransform.localPosition = target;
        }

        IsMoving = false;

        StopAnimation();

        routine = null;

        OnDestinationReached?.Invoke();
    }

    // =========================================================
    // ANIMATION
    // =========================================================

    private void UpdateAnimator(Vector2 direction)
    {
        if (animator == null)
            return;

        float deltaTime = Time.unscaledDeltaTime;

        animator.SetFloat(
            "moveX",
            direction.x,
            0.1f,
            deltaTime
        );

        animator.SetFloat(
            "moveY",
            direction.y,
            0.1f,
            deltaTime
        );

        animator.SetBool(
            "isMoving",
            true
        );
    }
    private void StopAnimation()
    {
        if (animator == null)
            return;

        animator.SetBool(
            "isMoving",
            false
        );
    }
}