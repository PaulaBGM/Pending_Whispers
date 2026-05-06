using UnityEngine;

public class DetectionVisionController : MonoBehaviour
{
    public static event System.Action<bool> OnDetectionVisionChanged;

    private bool isActive = false;

    void OnEnable()
    {
        if (InputController.Instance != null)
            InputController.Instance.OnDetectionPressed += ToggleVision;
    }

    void OnDisable()
    {
        if (InputController.Instance != null)
            InputController.Instance.OnDetectionPressed -= ToggleVision;
    }

    private void ToggleVision()
    {
        isActive = !isActive;
        OnDetectionVisionChanged?.Invoke(isActive);
    }
}