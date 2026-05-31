using UnityEngine;
using System;
using System.Collections;

public class SpectralDetectionSystem : MonoBehaviour
{
    public static SpectralDetectionSystem Instance;

    public static event Action<float> OnEnergyChanged;

    [Header("Energy")]
    [SerializeField] private float maxEnergy = 100f;
    [SerializeField] private float currentEnergy = 100f;

    [SerializeField] private float scanCost = 25f;

    [SerializeField] private float regenPerSecond = 15f;

    [Header("Cooldown")]
    [SerializeField] private float overloadCooldown = 4f;

    [Header("Scan")]
    [SerializeField] private SpectralScanWave scanWavePrefab;

    private bool overloaded;

    public float EnergyNormalized => currentEnergy / maxEnergy;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    private void OnEnable()
    {
        if (InputController.Instance != null)
            InputController.Instance.OnDetectionPressed += LaunchScan;
    }

    private void OnDisable()
    {
        if (InputController.Instance != null)
            InputController.Instance.OnDetectionPressed -= LaunchScan;
    }

    private void Update()
    {
        RegenerateEnergy();
    }

    private void LaunchScan()
    {
        if (overloaded)
            return;

        if (currentEnergy < scanCost)
            return;

        currentEnergy -= scanCost;

        OnEnergyChanged?.Invoke(EnergyNormalized);

        Instantiate(scanWavePrefab, transform.position, Quaternion.identity);

        if (currentEnergy <= 0)
            StartCoroutine(OverloadRoutine());
    }

    private void RegenerateEnergy()
    {
        if (currentEnergy >= maxEnergy)
            return;

        currentEnergy += regenPerSecond * Time.deltaTime;
        currentEnergy = Mathf.Clamp(currentEnergy, 0f, maxEnergy);

        OnEnergyChanged?.Invoke(EnergyNormalized);
    }

    private IEnumerator OverloadRoutine()
    {
        overloaded = true;

        yield return new WaitForSeconds(overloadCooldown);

        overloaded = false;
    }
}