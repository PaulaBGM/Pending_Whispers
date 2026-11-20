using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Punto de aparición del jugador al llegar a esta escena por una SceneDoor.
/// Se pone en un objeto vacío donde debe aparecer el jugador (p. ej. al pie de la escalera)
/// y se le da un id único dentro de la escena ("FromBasement", "FromHouse"...).
///
/// Si se llega a la escena por otro camino (mapa, cargar partida), no hace nada
/// y el jugador aparece donde esté colocado en la escena, como hasta ahora.
/// </summary>
public class SpawnPoint : MonoBehaviour
{
    [Tooltip("Id que usa la SceneDoor de la otra escena para mandar aquí al jugador.")]
    [SerializeField] private string id;

    // Destino pendiente: lo rellena SceneDoor justo antes de cambiar de escena.
    private static string pendingScene;
    private static string pendingId;

    public static void SetPending(string sceneName, string spawnId)
    {
        pendingScene = sceneName;
        pendingId = spawnId;
    }

    private void Start()
    {
        if (string.IsNullOrEmpty(pendingId) || pendingScene != gameObject.scene.name)
            return;

        if (pendingId != id)
            return;

        // Se consume para que no se vuelva a aplicar al entrar a esta escena por otro camino.
        pendingScene = null;
        pendingId = null;

        var player = FindFirstObjectByType<PlayerController_Actions>();
        if (player == null)
        {
            Debug.LogWarning($"[SpawnPoint] No hay jugador en la escena para colocarlo en '{id}'.", this);
            return;
        }

        Vector3 target = transform.position;
        target.z = player.transform.position.z;

        // Con NavMeshAgent hay que usar Warp: si solo se cambia transform.position,
        // el agente devuelve al jugador a donde estaba.
        if (player.TryGetComponent(out NavMeshAgent agent) && agent.enabled)
        {
            if (NavMesh.SamplePosition(target, out NavMeshHit hit, 2f, NavMesh.AllAreas))
                target = hit.position;

            agent.Warp(target);
            if (agent.isOnNavMesh)
                agent.ResetPath();
        }
        else
        {
            player.transform.position = target;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = new Color(0.3f, 1f, 0.5f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, 0.3f);
    }
}
