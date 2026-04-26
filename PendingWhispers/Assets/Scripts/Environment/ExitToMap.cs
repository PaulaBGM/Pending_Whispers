using UnityEngine;
using UnityEngine.SceneManagement;

public class ExitToMap : MonoBehaviour
{
    [SerializeField] private string mapSceneName = "Map";

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log("Trigger 2D detectado");

        if (other.CompareTag("Player"))
        {
            Debug.Log("Player detectado");
            SceneManager.LoadScene(mapSceneName);
        }
    }
}