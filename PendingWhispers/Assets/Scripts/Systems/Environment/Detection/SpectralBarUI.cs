using UnityEngine;
using UnityEngine.UI;

public class SpectralBarUI : MonoBehaviour
{
    [SerializeField] private Image fillBar;

    private void OnEnable()
    {
        SpectralDetectionSystem.OnEnergyChanged += UpdateBar;
    }

    private void OnDisable()
    {
        SpectralDetectionSystem.OnEnergyChanged -= UpdateBar;
    }

    private void UpdateBar(float value)
    {
        fillBar.fillAmount = value;
    }
}