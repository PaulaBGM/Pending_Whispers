using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class SpectralScanWave : MonoBehaviour
{
    [SerializeField] private float expansionSpeed = 5f;
    [SerializeField] private float maxRadius = 5f;

    private CircleCollider2D circleCollider;

    private readonly HashSet<SpectralTrace> detectedTraces = new();

    private void Awake()
    {
        circleCollider = GetComponent<CircleCollider2D>();

        circleCollider.isTrigger = true;
        circleCollider.radius = 0f;
    }

    private void Update()
    {
        circleCollider.radius += expansionSpeed * Time.deltaTime;

        transform.localScale =
            Vector3.one * circleCollider.radius * 2f;

        Collider2D[] hits =
            Physics2D.OverlapCircleAll(
                transform.position,
                circleCollider.radius
            );

        foreach (Collider2D hit in hits)
        {
            SpectralTrace trace =
                hit.GetComponent<SpectralTrace>();

            if (trace == null)
                continue;

            if (detectedTraces.Contains(trace))
                continue;

            detectedTraces.Add(trace);

            trace.Reveal();
        }

        if (circleCollider.radius >= maxRadius)
            Destroy(gameObject);
    }
}