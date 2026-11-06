using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : BaseSingleton<SceneController>
{
    /// <summary>Escena desde la que se hizo el ultimo LoadScene (p. ej. para saber desde donde se abrio el mapa).</summary>
    public string PreviousScene { get; private set; }

    public void LoadScene(string scene)
    {
        Debug.Log("[Scene] Loading: " + scene);
        PreviousScene = SceneManager.GetActiveScene().name;
        SceneManager.LoadScene(scene);
    }
}