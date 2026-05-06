using System.Collections.Generic;
using UnityEngine;

public class PathfindingAgent : MonoBehaviour
{
    public float speed = 5f;

    private Queue<Vector2> path = new Queue<Vector2>();
    private Animator animator;
    private Vector2 lastDirection;

    public void Initialize(Animator anim)
    {
        animator = anim;
    }

    public void SetPath(List<Vector2> newPath)
    {
        path.Clear();

        foreach (var p in newPath)
            path.Enqueue(p);
    }

    public bool HasPath()
    {
        return path.Count > 0;
    }

    public void Tick()
    {
        if (path.Count == 0)
        {
            animator.SetBool("isMoving", false);
            return;
        }

        Vector2 current = transform.position;
        Vector2 target = path.Peek();

        Vector2 next = Vector2.MoveTowards(current, target, speed * Time.deltaTime);
        Vector2 dir = (target - current).normalized;

        transform.position = next;

        if (Vector2.Distance(current, target) < 0.05f)
            path.Dequeue();

        if (dir.magnitude > 0.01f)
            lastDirection = dir;

        animator.SetFloat("moveX", lastDirection.x);
        animator.SetFloat("moveY", lastDirection.y);
        animator.SetBool("isMoving", true);
    }
}