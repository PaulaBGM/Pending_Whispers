using System;
using UnityEngine;

public class SpectralTrace : MonoBehaviour
{
    [SerializeField] private ClueHighlight highlight;
    [SerializeField] private bool revealOnlyOnce = true;

    private bool revealed;

    /// <summary>True cuando la onda espectral de Lyra ya ha alcanzado esta traza en esta visita a la escena.</summary>
    public bool IsRevealed => revealed;

    /// <summary>Se lanza solo la primera vez que la traza se revela.</summary>
    public event Action OnRevealed;

    public void Reveal()
    {
        if (revealOnlyOnce && revealed)
            return;

        bool firstReveal = !revealed;
        revealed = true;

        if (highlight != null)
            highlight.Show();

        if (firstReveal)
            OnRevealed?.Invoke();
    }
}