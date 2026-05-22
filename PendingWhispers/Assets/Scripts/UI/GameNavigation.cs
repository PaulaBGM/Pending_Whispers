using UnityEngine;

public class GameNavigation : MonoBehaviour
{
    public static GameNavigation Instance { get; private set; }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void OpenMap()
    {
        SceneController.Instance.LoadScene("Map");
    }

    public void OpenGame()
    {
        SceneController.Instance.LoadScene("Game");
    }
}