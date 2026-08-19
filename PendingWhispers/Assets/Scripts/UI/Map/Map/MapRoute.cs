using System.Collections.Generic;

public static class MapRoute
{
    public static List<MapWaypoint> FindPath(
        MapWaypoint start,
        MapWaypoint goal)
    {
        var path = new List<MapWaypoint>();

        if (start == null || goal == null)
            return path;

        var frontier = new Queue<MapWaypoint>();
        var cameFrom = new Dictionary<MapWaypoint, MapWaypoint>();

        frontier.Enqueue(start);
        cameFrom[start] = null;

        while (frontier.Count > 0)
        {
            MapWaypoint current = frontier.Dequeue();

            if (current == goal)
                break;

            foreach (MapWaypoint next in current.Connections)
            {
                if (next == null)
                    continue;

                if (cameFrom.ContainsKey(next))
                    continue;

                frontier.Enqueue(next);
                cameFrom[next] = current;
            }
        }

        if (!cameFrom.ContainsKey(goal))
            return path;

        MapWaypoint currentNode = goal;

        while (currentNode != null)
        {
            path.Add(currentNode);
            currentNode = cameFrom[currentNode];
        }

        path.Reverse();

        return path;
    }
}