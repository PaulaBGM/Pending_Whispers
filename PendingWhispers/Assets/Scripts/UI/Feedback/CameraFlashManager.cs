using UnityEngine;

public class CameraFlashManager : MonoBehaviour
{
    public static CameraFlashManager Instance;

    [Header("Flash")]
    [SerializeField] private CameraFlash cameraFlash;

    private void Awake()
    {
        Instance = this;
    }

    public void PlayEvidenceFeedback()
    {
        if (cameraFlash != null)
            cameraFlash.PlayFlash();
    }
}