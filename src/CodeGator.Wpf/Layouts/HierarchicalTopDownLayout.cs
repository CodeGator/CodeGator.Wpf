using System.Windows;
using CodeGator.Wpf;

namespace CodeGator.Wpf.Layouts;

/// <summary>
/// This class lays out breadth-first layers with ordered rows downward.
/// </summary>
internal sealed class HierarchicalTopDownLayout : ICgDiagramLayout
{
    /// <inheritdoc />
    public IReadOnlyDictionary<string, Point> Compute(IReadOnlyList<CgDiagramNode> nodes, IReadOnlyList<CgDiagramEdge> edges, CgDiagramLayoutOptions options)
    {
        var result = new Dictionary<string, Point>(StringComparer.Ordinal);
        if (nodes.Count == 0) return result;

        var layer = DiagramGraphTopology.LayerByBfs(nodes, edges);
        var groups = nodes.GroupBy(n => layer[n.Id]).OrderBy(g => g.Key).ToList();

        var xStep = options.NodeSize.Width + options.HorizontalSpacing;
        var yStep = options.NodeSize.Height + options.VerticalSpacing;

        var y = 0.0;
        foreach (var g in groups)
        {
            var row = g.OrderBy(n => n.Id, StringComparer.Ordinal).ToList();
            var totalW = (row.Count - 1) * xStep;
            var x = -totalW * 0.5;
            foreach (var n in row)
            {
                result[n.Id] = new Point(x, y);
                x += xStep;
            }
            y += yStep;
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

