using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class MapAvatar : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float speed = 10000f;

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

            Vector3 target =
                waypoint.RectTransform.position;

            while (
                (rectTransform.position - target).sqrMagnitude
                > 0.01f
            )
            {
                Vector3 current =
                    rectTransform.position;

                Vector2 direction =
                    (target - current);

                if (direction.sqrMagnitude > 0.001f)
                    direction.Normalize();

                // -----------------------------
                // ANIMATION
                // -----------------------------

                UpdateAnimator(direction);

                // -----------------------------
                // MOVEMENT
                // -----------------------------

                rectTransform.position =
                    Vector3.MoveTowards(
                        current,
                        target,
                        speed * Time.unscaledDeltaTime
                    );

                yield return null;
            }

            rectTransform.position = target;
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