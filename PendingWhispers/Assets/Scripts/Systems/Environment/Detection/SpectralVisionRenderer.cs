using UnityEngine;

public class SpectralVisionRenderer : MonoBehaviour
{
    [SerializeField] private GameObject spectralOverlay;

    [SerializeField] private AudioSource ambienceSource;

    [SerializeField] private AudioClip spectralLoop;

    private void OnEnable()
    {
        SpectralDetectionSystem.OnDetectionChanged +=
            HandleDetection;
    }

    private void OnDisable()
    {
        SpectralDetectionSystem.OnDetectionChanged -=
            HandleDetection;
    }

    void HandleDetection(bool active)
    {
        spectralOverlay.SetActive(active);

        if (active)
        {
            ambienceSource.clip = spectralLoop;
            ambienceSource.Play();
        }
        else
        {
            ambienceSource.Stop();
        }
    }
}