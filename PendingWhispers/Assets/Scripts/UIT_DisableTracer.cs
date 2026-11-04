using UnityEngine;

/// <summary>
/// Diagnóstico temporal: ponlo en el GameObject que se desactiva solo (p. ej. Journal_UI).
/// Cuando alguien lo desactive o lo mueva a DontDestroyOnLoad, escribe en la consola
/// la pila de llamadas completa: ahí sale el script y la línea responsable.
/// Bórralo cuando encuentres al culpable.
/// </summary>
[DefaultExecutionOrder(-10000)] // Awake antes que nadie, para ver también lo que pasa al cargar.
public class UIT_DisableTracer : MonoBehaviour
{
    private string lastSceneName;
    private bool isQuitting;

    private void OnApplicationQuit() => isQuitting = true;

    private void Awake()
    {
        lastSceneName = gameObject.scene.name;
        Debug.Log($"[DisableTracer] '{name}' Awake en escena '{lastSceneName}', activo = {gameObject.activeInHierarchy}.", this);
    }

    private void OnDisable()
    {
        // No avisar al salir de Play. (Antes también se filtraba por scene.isLoaded,
        // pero la escena DontDestroyOnLoad no cuenta como "cargada" y se perdía el aviso.)
        if (!Application.isPlaying || isQuitting)
            return;

        Debug.LogError($"[DisableTracer] '{name}' SE HA DESACTIVADO. Pila de llamadas:\n{System.Environment.StackTrace}", this);
    }

    private void Update()
    {
        string sceneName = gameObject.scene.name;
        if (sceneName == lastSceneName)
            return;

        Debug.LogWarning($"[DisableTracer] '{name}' ha pasado de la escena '{lastSceneName}' a '{sceneName}' " +
                         "(DontDestroyOnLoad). Lo ha movido algún componente persistente de su mismo árbol.", this);
        lastSceneName = sceneName;
    }
}