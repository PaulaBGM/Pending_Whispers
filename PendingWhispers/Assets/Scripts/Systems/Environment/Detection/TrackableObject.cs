using UnityEngine;

public class TrackableObject : MonoBehaviour
{
    [Header("Visuals")]
    [SerializeField] private GameObject visualObject;

    [Header("Evidence")]
    [SerializeField] private FlagSO evidenceFlag;

    [SerializeField] private bool autoHide = true;

    private bool discovered = false;

    void Start()
    {
        HideTrack();
    }

    void Update()
    {
        if (SpectralDetectionSystem.Instance == null)
            return;

        if (SpectralDetectionSystem.Instance.DetectionActive)
        {
            ShowTrack();
        }
        else if (autoHide && !discovered)
        {
            HideTrack();
        }
    }

    public void ShowTrack()
    {
        visualObject.SetActive(true);

        //SpectralDetectionSystem.Instance.RegisterObject(this);
    }

    public void HideTrack()
    {
        if (!discovered)
            visualObject.SetActive(false);
    }

    public void CollectEvidence()
    {
        if (discovered) return;

        discovered = true;

        GameProgress.Instance.AddFlag(evidenceFlag);

        UIFeedbackManager.Instance.ShowMessage("Pista encontrada");
    }
}