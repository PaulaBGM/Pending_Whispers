using System;
using UnityEngine;

public class DetectionVisionController : MonoBehaviour
{
    public static event Action<bool> OnDetectionVisionChanged;

    private bool isActive = false;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            ToggleVision();
        }
    }

    private void ToggleVision()
    {
        isActive = !isActive;
        OnDetectionVisionChanged?.Invoke(isActive);
    }
}