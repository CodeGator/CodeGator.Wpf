using System.Windows;
using CodeGator.Wpf;

namespace CodeGator.Wpf.Layouts;

/// <summary>
/// This class positions nodes on concentric rings by BFS distance from roots.
/// </summary>
internal sealed class RadialLayout : ICgDiagramLayout
{
    /// <inheritdoc />
    public IReadOnlyDictionary<string, Point> Compute(IReadOnlyList<CgDiagramNode> nodes, IReadOnlyList<CgDiagramEdge> edges, CgDiagramLayoutOptions options)
    {
        var result = new Dictionary<string, Point>(StringComparer.Ordinal);
        if (nodes.Count == 0) return result;

        var layer = DiagramGraphTopology.LayerByBfs(nodes, edges);
        var maxLayer = layer.Values.DefaultIfEmpty(0).Max();

        var center = new Point(0, 0);
        var ringStep = Math.Max(options.NodeSize.Width, options.NodeSize.Height) + Math.Max(options.HorizontalSpacing, options.VerticalSpacing);

        for (var l = 0; l <= maxLayer; l++)
        {
            var ring = nodes.Where(n => layer[n.Id] == l).OrderBy(n => n.Id, StringComparer.Ordinal).ToList();
            if (ring.Count == 0) continue;
            if (l == 0 && ring.Count == 1)
            {
                result[ring[0].Id] = new Point(center.X, center.Y);
                continue;
            }

            var radius = l * ringStep;
            var count = ring.Count;
            for (var i = 0; i < count; i++)
            {
                var t = (2 * Math.PI * i) / count;
                var x = center.X + radius * Math.Cos(t);
                var y = center.Y + radius * Math.Sin(t);
                result[ring[i].Id] = new Point(x, y);
            }
        }

        // Convert centers to top-left positions and normalize.
        foreach (var id in result.Keys.ToList())
        {
            var p = result[id];
            result[id] = new Point(p.X - options.NodeSize.Width * 0.5, p.Y - options.NodeSize.Height * 0.5);
        }

        NormalizeToPositive(result);
        return result;
    }

    /// <summary>
    /// This method shifts positions so the minimum x and y values are non-negative.
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

