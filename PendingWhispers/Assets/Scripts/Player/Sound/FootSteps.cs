using FMODUnity;
using FMOD.Studio;
using UnityEngine;

public class FootstepAudio : MonoBehaviour
{
    [Header("FMOD")]
    [SerializeField] private EventReference footstepEvent;

    [Header("Ground")]
    [SerializeField] private LayerMask groundLayer;
    private const string GROUND_PARAMETER = "GroundTypes";
    private enum SurfaceType
    {
        Wood = 0,
        Carpet = 1,
        Concrete = 2
    }

    // 👇 ESTE MÉTODO LO LLAMA LA ANIMACIÓN
    public void PlayFootstep()
    {
        SurfaceType surface = GetSurface();

        EventInstance instance = RuntimeManager.CreateInstance(footstepEvent);

        instance.setParameterByName(GROUND_PARAMETER, (float)surface);        
        instance.set3DAttributes(RuntimeUtils.To3DAttributes(transform.position));

        instance.start();
        instance.release();
    }

    SurfaceType GetSurface()
    {
        Collider2D[] hits = Physics2D.OverlapPointAll(transform.position);

        foreach (Collider2D col in hits)
        {
            if (col.CompareTag("Carpet"))
                return SurfaceType.Carpet;
        }

        foreach (Collider2D col in hits)
        {
            if (col.CompareTag("Wood"))
                return SurfaceType.Wood;

            if (col.CompareTag("Concrete"))
                return SurfaceType.Concrete;
        }

        return SurfaceType.Concrete;
    }
}