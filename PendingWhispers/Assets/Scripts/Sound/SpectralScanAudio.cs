using FMOD.Studio;
using FMODUnity;
using UnityEngine;
using STOP_MODE = FMOD.Studio.STOP_MODE;

public class SpectralScanAudio : MonoBehaviour
{
    [SerializeField] private EventReference scanSound;

    private EventInstance scanInstance;

    public void PlayScanSound()
    {
        scanInstance = RuntimeManager.CreateInstance(scanSound);
        scanInstance.start();
    }

    public void StopScanSound()
    {
        if (scanInstance.isValid())
        {
            scanInstance.stop(STOP_MODE.ALLOWFADEOUT);
            scanInstance.release();
        }
    }
}