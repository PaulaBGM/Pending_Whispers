using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class SpectralScanWave : MonoBehaviour
{
    [Header("Scan")]
    [SerializeField] private float maxRadius = 5f;
    [SerializeField] private float duration = 5f;

    [Header("Particles")]
    [SerializeField] private ParticleSystem ps;

    [Header("Particle Size")]
    [SerializeField] private float startParticleSize = 0.1f;
    [SerializeField] private float maxParticleSize = 0.5f;

    private CircleCollider2D circleCollider;

    private readonly HashSet<SpectralTrace> detected = new();

    private float timer;
    private float currentRadius;

    private ParticleSystem.MainModule mainModule;

    private void Awake()
    {
        circleCollider = GetComponent<CircleCollider2D>();
        circleCollider.isTrigger = true;

        if (ps == null)
            ps = GetComponentInChildren<ParticleSystem>();

        if (ps != null)
            mainModule = ps.main;
    }

    private void OnEnable()
    {
        ResetScan();

        timer = 0f;

        if (ps != null)
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ps.Play(true);
        }
    }

    private void Update()
    {
        timer += Time.deltaTime;

        float t = Mathf.Clamp01(timer / duration);

        currentRadius = t * maxRadius;

        circleCollider.radius = currentRadius;

        UpdateParticles(t);

        Detect();

        if (t >= 1f)
        {
            gameObject.SetActive(false);
        }
    }

    private void UpdateParticles(float t)
    {
        if (ps == null)
            return;

        mainModule.startSize = Mathf.Lerp(startParticleSize, maxParticleSize, t);
    }

    private void Detect()
    {
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, currentRadius);

        foreach (var hit in hits)
        {
            SpectralTrace trace = hit.GetComponent<SpectralTrace>();

            if (trace == null || detected.Contains(trace))
                continue;

            detected.Add(trace);
            trace.Reveal();
        }
    }

    private void ResetScan()
    {
        detected.Clear();
        currentRadius = 0f;
        circleCollider.radius = 0f;
    }
}