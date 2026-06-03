using UnityEngine;
using System;
using System.Collections;

public class SpectralDetectionSystem : MonoBehaviour
{
    public static SpectralDetectionSystem Instance;

    public static event Action<float> OnEnergyChanged;
    public static event Action<bool> OnCooldownStateChanged;
    public static event Action<float> OnCooldownProgress;

    [Header("Energy")]
    [SerializeField] private float maxEnergy = 100f;
    [SerializeField] private float currentEnergy = 100f;
    [SerializeField] private float scanCost = 25f;
    [SerializeField] private float regenPerSecond = 15f;

    [Header("Cooldown")]
    [SerializeField] private float cooldownTime = 5f;
    [SerializeField] private float overloadCooldown = 4f;

    [Header("Scan")]
    [SerializeField] private SpectralScanWave scanWave;

    private bool overloaded;
    private bool isCoolingDown;
    private float cooldownTimer;

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

    public void LaunchScan()
    {
        if (overloaded || isCoolingDown)
            return;

        if (currentEnergy < scanCost)
            return;

        currentEnergy -= scanCost;
        OnEnergyChanged?.Invoke(EnergyNormalized);

        scanWave.transform.position = transform.position;
        scanWave.gameObject.SetActive(true);

        StartCooldown();

        if (currentEnergy <= 0)
            StartCoroutine(OverloadRoutine());
    }

    private void StartCooldown()
    {
        isCoolingDown = true;
        cooldownTimer = cooldownTime;

        OnCooldownStateChanged?.Invoke(true);

        StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        while (cooldownTimer > 0f)
        {
            cooldownTimer -= Time.deltaTime;

            OnCooldownProgress?.Invoke(1f - (cooldownTimer / cooldownTime));

            yield return null;
        }

        cooldownTimer = 0f;
        isCoolingDown = false;

        OnCooldownStateChanged?.Invoke(false);
        OnCooldownProgress?.Invoke(1f);
    }

    private IEnumerator OverloadRoutine()
    {
        overloaded = true;
        yield return new WaitForSeconds(overloadCooldown);
        overloaded = false;
    }

    private void RegenerateEnergy()
    {
        if (currentEnergy >= maxEnergy)
            return;

        currentEnergy += regenPerSecond * Time.deltaTime;
        currentEnergy = Mathf.Clamp(currentEnergy, 0f, maxEnergy);

        OnEnergyChanged?.Invoke(EnergyNormalized);
    }
}