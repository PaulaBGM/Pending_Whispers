using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : BaseSingleton<SceneController>
{
    public void LoadScene(string scene)
    {
        Debug.Log("[Scene] Loading: " + scene);
        SceneManager.LoadScene(scene);
    }
}