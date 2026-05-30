using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : BaseSingleton<SceneController>
{
    public static new SceneController Instance { get; private set; }

    private new void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadScene(string scene)
    {
        Debug.Log("[Scene] Loading: " + scene);
        SceneManager.LoadScene(scene);
    }
}