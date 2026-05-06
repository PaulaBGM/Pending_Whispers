using UnityEngine;

public class MainMenuController : MonoBehaviour
{
    private void OnEnable()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.OnSubmitPressed += StartGame;
    }

    private void OnDisable()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.OnSubmitPressed -= StartGame;
    }

    public void StartGame()
    {
        SceneController.Instance.LoadScene("Map");
    }

    public void ExitGame()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}