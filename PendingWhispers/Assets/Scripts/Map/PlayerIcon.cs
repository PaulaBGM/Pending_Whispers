using UnityEngine;
using System.Collections;

public class PlayerIcon : MonoBehaviour
{
    public float moveSpeed = 5f;

    public void MoveTo(Vector3 targetPosition)
    {
        StopAllCoroutines();
        StartCoroutine(MoveRoutine(targetPosition));
    }

    IEnumerator MoveRoutine(Vector3 target)
    {
        while (Vector3.Distance(transform.position, target) > 0.05f)
        {
            transform.position = Vector3.Lerp(transform.position, target, Time.deltaTime * moveSpeed);
            yield return null;
        }

        transform.position = target;
    }
}