using UnityEngine;
using System;
using System.Collections;

public enum SpectralTraceType
{
    Footprint,
    BloodEcho,
    EmotionalResidue,
    MemoryFragment,
    Corruption,
    HiddenObject,
    GhostPresence
}

public class SpectralDetectionSystem : MonoBehaviour
{
    public static SpectralDetectionSystem Instance;

    public static event Action<bool> OnDetectionChanged;

    public static event Action<float> OnEnergyChanged;

    [Header("Energy")]
    [SerializeField] private float maxEnergy = 100f;

    [SerializeField] private float currentEnergy = 100f;

    [SerializeField] private float drainPerSecond = 10f;

    [SerializeField] private float regenPerSecond = 15f;

    [Header("Cooldown")]
    [SerializeField] private float overloadCooldown = 4f;

    private bool detectionActive;
    private bool overloaded;

    public bool DetectionActive => detectionActive;

    public float EnergyNormalized =>
        currentEnergy / maxEnergy;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        if (InputController.Instance != null)
        {
            InputController.Instance.OnDetectionPressed +=
                ToggleDetection;
        }
    }

    private void OnDisable()
    {
        if (InputController.Instance != null)
        {
            InputController.Instance.OnDetectionPressed -=
                ToggleDetection;
        }
    }

    private void Update()
    {
        HandleEnergy();
    }

    void ToggleDetection()
    {
        if (overloaded)
            return;

        detectionActive = !detectionActive;

        OnDetectionChanged?.Invoke(detectionActive);
    }

    void HandleEnergy()
    {
        if (detectionActive)
        {
            currentEnergy -=
                drainPerSecond * Time.deltaTime;

            if (currentEnergy <= 0)
            {
                currentEnergy = 0;

                StartCoroutine(OverloadRoutine());
            }
        }
        else
        {
            currentEnergy +=
                regenPerSecond * Time.deltaTime;

            currentEnergy =
                Mathf.Clamp(currentEnergy, 0, maxEnergy);
        }

        OnEnergyChanged?.Invoke(
            currentEnergy / maxEnergy
        );
    }

    IEnumerator OverloadRoutine()
    {
        overloaded = true;

        detectionActive = false;

        OnDetectionChanged?.Invoke(false);

        yield return new WaitForSeconds(overloadCooldown);

        overloaded = false;
    }
}