using UnityEngine;

public class SpectralTrace : MonoBehaviour
{
    [SerializeField] private ClueHighlight highlight;
    [SerializeField] private bool revealOnlyOnce = true;

    private bool revealed;

    public void Reveal()
    {
        if (revealOnlyOnce && revealed)
            return;

        revealed = true;

        if (highlight != null)
            highlight.Show();
    }
}