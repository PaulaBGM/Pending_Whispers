using System.Collections.Generic;
using UnityEngine;

public class PathNode : MonoBehaviour
{
    public List<PathNode> connections = new();

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;

        Gizmos.DrawSphere(transform.position, 0.15f);

        Gizmos.color = Color.yellow;

        foreach (var node in connections)
        {
            if (node != null)
            {
                Gizmos.DrawLine(
                    transform.position,
                    node.transform.position
                );
            }
        }
    }
}