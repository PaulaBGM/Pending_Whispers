using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerIcon : MonoBehaviour
{
    [SerializeField] private float speed = 4f;

    private readonly Queue<PathNode> path = new();

    public bool IsMoving { get; private set; }

    public event Action OnDestinationReached;

    public void FollowPath(List<PathNode> newPath)
    {
        StopAllCoroutines();

        path.Clear();

        for (int i = 1; i < newPath.Count; i++)
            path.Enqueue(newPath[i]);

        StartCoroutine(FollowRoutine());
    }

    private IEnumerator FollowRoutine()
    {
        IsMoving = true;

        while (path.Count > 0)
        {
            Vector2 target = path.Peek().Position;

            while (((Vector2)transform.position - target).sqrMagnitude > 0.0001f)
            {
                transform.position = Vector2.MoveTowards(
                    transform.position,
                    target,
                    speed * Time.deltaTime);

                yield return null;
            }

            transform.position = target;

            path.Dequeue();
        }

        IsMoving = false;

        OnDestinationReached?.Invoke();
    }
}