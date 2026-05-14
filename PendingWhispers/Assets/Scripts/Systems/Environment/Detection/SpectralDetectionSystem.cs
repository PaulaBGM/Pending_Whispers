using UnityEngine;
using System.Collections;

public class SpectralDetectionSystem : MonoBehaviour
{
    public static SpectralDetectionSystem Instance;

    [Header("Energy")]
    [SerializeField] private float maxEnergy = 100f;
    [SerializeField] private float currentEnergy = 100f;

    [SerializeField] private float drainPerSecond = 15f;
    [SerializeField] private float regenPerSecond = 10f;

    [Header("Overload")]
    [SerializeField] private float overloadCooldown = 4f;

    [Header("State")]
    [SerializeField] private bool detectionActive;

    private bool overloaded;

    public bool DetectionActive => detectionActive;

    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        if (InputController.Instance != null)
        {
            InputController.Instance.OnDetectionPressed += ToggleDetection;
        }
    }

    private void OnDisable()
    {
        if (InputController.Instance != null)
        {
            InputController.Instance.OnDetectionPressed -= ToggleDetection;
        }
    }

    private void Update()
    {
        HandleEnergy();
    }

    private void ToggleDetection()
    {
        if (overloaded)
        {
            UIGameEvents.OnFeedback?.Invoke("Sobrecarga espectral");
            return;
        }

        detectionActive = !detectionActive;

        if (detectionActive)
        {
            UIGameEvents.OnFeedback?.Invoke("Detección espectral activada");
        }
        else
        {
            UIGameEvents.OnFeedback?.Invoke("Detección espectral desactivada");
        }
    }

    private void HandleEnergy()
    {
        if (detectionActive)
        {
            currentEnergy -= drainPerSecond * Time.deltaTime;

            if (currentEnergy <= 0)
            {
                currentEnergy = 0;
                StartCoroutine(OverloadRoutine());
            }
        }
        else
        {
            if (currentEnergy < maxEnergy)
            {
                currentEnergy += regenPerSecond * Time.deltaTime;

                if (currentEnergy > maxEnergy)
                    currentEnergy = maxEnergy;
            }
        }
    }

    private IEnumerator OverloadRoutine()
    {
        overloaded = true;
        detectionActive = false;

        UIGameEvents.OnFeedback?.Invoke("Sobrecarga espectral");

        yield return new WaitForSeconds(overloadCooldown);

        overloaded = false;

        UIGameEvents.OnFeedback?.Invoke("Detección restaurada");
    }

    public float GetNormalizedEnergy()
    {
        return currentEnergy / maxEnergy;
    }
}