using System.Collections.Generic;

/// <summary>
/// Misma búsqueda en anchura que MapRoute, pero sobre nombres de elementos del UXML
/// en vez de componentes MapWaypoint.
/// </summary>
public static class UIT_MapRoute
{
    public static List<string> FindPath(
        IReadOnlyDictionary<string, HashSet<string>> graph,
        string start,
        string goal)
    {
        var path = new List<string>();

        if (graph == null || string.IsNullOrEmpty(start) || string.IsNullOrEmpty(goal))
            return path;

        if (start == goal)
        {
            path.Add(start);
            return path;
        }

        var frontier = new Queue<string>();
        var cameFrom = new Dictionary<string, string>();

        frontier.Enqueue(start);
        cameFrom[start] = null;

        while (frontier.Count > 0)
        {
            string current = frontier.Dequeue();

            if (current == goal)
                break;

            if (!graph.TryGetValue(current, out var connections))
                continue;

            foreach (string next in connections)
            {
                if (string.IsNullOrEmpty(next) || cameFrom.ContainsKey(next))
                    continue;

                frontier.Enqueue(next);
                cameFrom[next] = current;
            }
        }

        if (!cameFrom.ContainsKey(goal))
            return path;

        for (string node = goal; node != null; node = cameFrom[node])
            path.Add(node);

        path.Reverse();
        return path;
    }
}
