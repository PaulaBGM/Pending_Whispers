using System.Collections.Generic;
using UnityEngine;

public class Pathfinder : MonoBehaviour
{
    public static Pathfinder Instance;

    private void Awake()
    {
        Instance = this;
    }

    public List<Vector2> FindPath(PathNode start, PathNode goal)
    {
        Queue<PathNode> frontier = new();

        Dictionary<PathNode, PathNode> cameFrom = new();

        frontier.Enqueue(start);

        cameFrom[start] = null;

        while (frontier.Count > 0)
        {
            PathNode current = frontier.Dequeue();

            if (current == goal)
                break;

            foreach (PathNode next in current.connections)
            {
                if (cameFrom.ContainsKey(next))
                    continue;

                frontier.Enqueue(next);

                cameFrom[next] = current;
            }
        }

        List<Vector2> path = new();

        if (!cameFrom.ContainsKey(goal))
            return path;

        PathNode currentNode = goal;

        while (currentNode != null)
        {
            path.Add(currentNode.transform.position);

            currentNode = cameFrom[currentNode];
        }

        path.Reverse();

        return path;
    }
}