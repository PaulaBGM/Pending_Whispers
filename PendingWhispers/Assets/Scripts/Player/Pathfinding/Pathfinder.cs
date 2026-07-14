using System.Collections.Generic;
using UnityEngine;

public class Pathfinder : MonoBehaviour
{
    public static Pathfinder Instance;

    private void Awake()
    {
        Instance = this;
    }

    public List<PathNode> FindPath(PathNode start, PathNode goal)
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

        List<PathNode> path = new();

        if (!cameFrom.ContainsKey(goal))
            return path;

        PathNode node = goal;

        while (node != null)
        {
            path.Add(node);
            node = cameFrom[node];
        }

        path.Reverse();

        return path;
    }
}