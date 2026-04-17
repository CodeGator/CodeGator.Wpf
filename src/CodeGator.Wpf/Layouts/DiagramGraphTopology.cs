using CodeGator.Wpf;

namespace CodeGator.Wpf.Layouts;

/// <summary>
/// This class provides graph topology helpers for diagram layout algorithms.
/// </summary>
internal static class DiagramGraphTopology
{
    /// <summary>
    /// This method builds outgoing adjacency lists for each node id in the graph.
    /// </summary>
    public static Dictionary<string, List<string>> BuildOutgoing(IReadOnlyList<CgDiagramNode> nodes, IReadOnlyList<CgDiagramEdge> edges)
    {
        var map = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        foreach (var n in nodes)
        {
            map[n.Id] = new List<string>();
        }

        foreach (var e in edges)
        {
            if (map.TryGetValue(e.FromId, out var list) && map.ContainsKey(e.ToId))
            {
                list.Add(e.ToId);
            }
        }
        return map;
    }

    /// <summary>
    /// This method counts incoming edges per node for root and cycle detection.
    /// </summary>
    public static Dictionary<string, int> BuildInDegree(IReadOnlyList<CgDiagramNode> nodes, IReadOnlyList<CgDiagramEdge> edges)
    {
        var indeg = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var n in nodes) indeg[n.Id] = 0;
        foreach (var e in edges)
        {
            if (indeg.ContainsKey(e.ToId) && indeg.ContainsKey(e.FromId))
            {
                indeg[e.ToId] = indeg[e.ToId] + 1;
            }
        }
        return indeg;
    }

    /// <summary>
    /// This method returns ids with no incoming edges, ordered for stable starts.
    /// </summary>
    public static List<string> Roots(IReadOnlyList<CgDiagramNode> nodes, IReadOnlyList<CgDiagramEdge> edges)
    {
        var indeg = BuildInDegree(nodes, edges);
        return indeg.Where(kvp => kvp.Value == 0).Select(kvp => kvp.Key).OrderBy(x => x, StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// This method assigns each node a layer using a Kahn-style breadth-first pass.
    /// </summary>
    /// <remarks>
    /// When the graph is entirely cyclic, every node remains on layer zero in a stable order.
    /// </remarks>
    public static Dictionary<string, int> LayerByBfs(IReadOnlyList<CgDiagramNode> nodes, IReadOnlyList<CgDiagramEdge> edges)
    {
        var outgoing = BuildOutgoing(nodes, edges);
        var indeg = BuildInDegree(nodes, edges);
        var layer = nodes.ToDictionary(n => n.Id, _ => 0, StringComparer.Ordinal);

        var q = new Queue<string>(indeg.Where(kvp => kvp.Value == 0).Select(kvp => kvp.Key).OrderBy(x => x, StringComparer.Ordinal));
        if (q.Count == 0)
        {
            // For cycles, fall back to stable order.
            for (var i = 0; i < nodes.Count; i++)
            {
                layer[nodes[i].Id] = 0;
            }
            return layer;
        }

        var seen = new HashSet<string>(StringComparer.Ordinal);
        while (q.Count > 0)
        {
            var id = q.Dequeue();
            if (!seen.Add(id)) continue;
            var baseL = layer[id];
            foreach (var to in outgoing[id])
            {
                layer[to] = Math.Max(layer[to], baseL + 1);
                indeg[to]--;
                if (indeg[to] == 0)
                {
                    q.Enqueue(to);
                }
            }
        }

        // Any disconnected/cyclic nodes not visited: keep at layer 0 but stable.
        return layer;
    }
}

