using UnityEngine;
using UnityEngine.AI;

public class NavmeshMovement : MonoBehaviour
{
    NavMeshAgent agent;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();

        agent.updatePosition = true;
        agent.updateRotation = false;
        agent.updateUpAxis = false;

        agent.speed = 3.5f;
        agent.acceleration = 8f;
        agent.stoppingDistance = 0f;

        if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 2f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position);
        }

        Debug.Log("READY isOnNavMesh: " + agent.isOnNavMesh);
    }

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Debug.Log("🖱 CLICK");

            Vector2 worldPoint = Camera.main.ScreenToWorldPoint(Input.mousePosition);

            RaycastHit2D hit = Physics2D.Raycast(worldPoint, Vector2.zero);

            if (hit.collider != null)
            {
                Debug.Log("✔ HIT 2D: " + hit.collider.name);
                Debug.Log("✔ POINT: " + hit.point);

                agent.SetDestination(hit.point);
            }
            else
            {
                Debug.Log("❌ NO HIT 2D");
            }
        }

        Debug.Log("velocity: " + agent.velocity);
    }
}