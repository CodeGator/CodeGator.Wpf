using System.Windows;
using CodeGator.Wpf;

namespace CodeGator.Wpf.Layouts;

/// <summary>
/// This class computes node positions using an iterative force-directed simulation.
/// </summary>
/// <remarks>
/// The iteration count and random seed come from <see cref="CgDiagramLayoutOptions"/>.
/// </remarks>
internal sealed class ForceDirectedLayout : ICgDiagramLayout
{
    /// <summary>
    /// This method simulates attracting and repelling forces to settle nodes, then converts vectors to top-left coordinates.
    /// </summary>
    public IReadOnlyDictionary<string, Point> Compute(IReadOnlyList<CgDiagramNode> nodes, IReadOnlyList<CgDiagramEdge> edges, CgDiagramLayoutOptions options)
    {
        var result = new Dictionary<string, Point>(StringComparer.Ordinal);
        if (nodes.Count == 0) return result;

        var n = nodes.Count;
        var rng = new Random(options.ForceSeed);

        // Start positions on a small jittered grid for stability.
        var pos = new Dictionary<string, Vector>(StringComparer.Ordinal);
        var side = (int)Math.Ceiling(Math.Sqrt(n));
        var step = Math.Max(options.NodeSize.Width, options.NodeSize.Height) * 1.2;
        for (var i = 0; i < n; i++)
        {
            var gx = i % side;
            var gy = i / side;
            pos[nodes[i].Id] = new Vector(
                gx * step + (rng.NextDouble() - 0.5) * 10,
                gy * step + (rng.NextDouble() - 0.5) * 10);
        }

        var ids = nodes.Select(x => x.Id).ToArray();
        var adj = DiagramGraphTopology.BuildOutgoing(nodes, edges);

        // Fruchterman–Reingold-ish
        var area = (step * side) * (step * side);
        var k = Math.Sqrt(area / n);
        var t = k;

        for (var iter = 0; iter < Math.Max(1, options.ForceIterations); iter++)
        {
            var disp = new Dictionary<string, Vector>(StringComparer.Ordinal);
            foreach (var id in ids) disp[id] = new Vector(0, 0);

            // Repulsive
            for (var i = 0; i < ids.Length; i++)
            {
                for (var j = i + 1; j < ids.Length; j++)
                {
                    var v = ids[i];
                    var u = ids[j];
                    var delta = pos[v] - pos[u];
                    var dist = Math.Max(0.01, delta.Length);
                    var force = (k * k) / dist;
                    var dir = delta / dist;
                    disp[v] += dir * force;
                    disp[u] -= dir * force;
                }
            }

            // Attractive (treat as undirected)
            foreach (var from in adj.Keys)
            {
                foreach (var to in adj[from])
                {
                    var delta = pos[from] - pos[to];
                    var dist = Math.Max(0.01, delta.Length);
                    var force = (dist * dist) / k;
                    var dir = delta / dist;
                    disp[from] -= dir * force;
                    disp[to] += dir * force;
                }
            }

            // Limit and apply
            foreach (var id in ids)
            {
                var d = disp[id];
                var dist = Math.Max(0.01, d.Length);
                var limited = d / dist * Math.Min(dist, t);
                pos[id] += limited;
            }

            // Cool down
            t *= 0.95;
        }

        // Convert to top-left positions and normalize.
        foreach (var id in ids)
        {
            var v = pos[id];
            result[id] = new Point(v.X - options.NodeSize.Width * 0.5, v.Y - options.NodeSize.Height * 0.5);
        }

        NormalizeToPositive(result);
        return result;
    }

    /// <summary>
    /// This method translates all positions so the minimum x and y coordinates are non-negative.
    /// </summary>
    static void NormalizeToPositive(Dictionary<string, Point> map)
    {
        if (map.Count == 0) return;
        var minX = map.Values.Min(p => p.X);
        var minY = map.Values.Min(p => p.Y);
        if (minX >= 0 && minY >= 0) return;
        foreach (var k in map.Keys.ToList())
        {
            var p = map[k];
            map[k] = new Point(p.X - minX, p.Y - minY);
        }
    }
}

