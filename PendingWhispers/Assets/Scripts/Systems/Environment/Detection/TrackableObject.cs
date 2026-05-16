using UnityEngine;

public class TrackableObject : MonoBehaviour
{
    [SerializeField] private SpectralTraceType traceType;

    [Header("Visual")]
    [SerializeField] private GameObject visualObject;
   
    [Header("Behaviour")]
    [SerializeField] private bool hideWhenInactive = true;

    [SerializeField] private bool discovered;

    [Header("Conditions")]
    [SerializeField] private FlagSO requiredFlag;

    private void Awake()
    {
        visualObject.SetActive(false);
    }

    private void OnEnable()
    {
        SpectralDetectionSystem.OnDetectionChanged +=
            HandleDetectionChanged;
    }

    private void OnDisable()
    {
        SpectralDetectionSystem.OnDetectionChanged -=
            HandleDetectionChanged;
    }

    private void HandleDetectionChanged(bool active)
    {
        if (requiredFlag != null)
        {
            if (!GameProgress.Instance.HasFlag(requiredFlag))
            {
                visualObject.SetActive(false);
                return;
            }
        }

        if (active)
        {
            ShowTrack();
        }
        else if (hideWhenInactive && !discovered)
        {
            HideTrack();
        }
    }

    public void ShowTrack()
    {
        visualObject.SetActive(true);
    }

    public void HideTrack()
    {
        visualObject.SetActive(false);
    }

    public void MarkDiscovered()
    {
        discovered = true;
    }
}