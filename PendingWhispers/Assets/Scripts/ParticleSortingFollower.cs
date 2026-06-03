using System.Collections;
using UnityEngine;

public class ParticleSortingFollower : MonoBehaviour
{
    [SerializeField] private SpriteRenderer playerRenderer;
    [SerializeField] private ParticleSystemRenderer psRenderer;

    [SerializeField] private int orderOffset = -1;

    private void OnEnable()
    {
        StartCoroutine(Init());
    }

    private IEnumerator Init()
    {
        yield return null;
        yield return new WaitForEndOfFrame();

        if (psRenderer == null)
            psRenderer = GetComponentInChildren<ParticleSystemRenderer>();

        if (playerRenderer == null)
            playerRenderer = GetComponentInParent<SpriteRenderer>();

        while (gameObject.activeInHierarchy)
        {
            if (playerRenderer != null && psRenderer != null)
            {
                psRenderer.sortingOrder = playerRenderer.sortingOrder + orderOffset;
            }

            yield return null;
        }
    }
}