using System.Windows;
using CodeGator.Wpf;

namespace CodeGator.Wpf.Layouts;

/// <summary>
/// This class lays out nodes in breadth-first layers, advancing horizontally with ordered columns.
/// </summary>
internal sealed class HierarchicalLeftToRightLayout : ICgDiagramLayout
{
    /// <summary>
    /// This method calculates a top-left position in content space for every node using horizontal hierarchical layers.
    /// </summary>
    public IReadOnlyDictionary<string, Point> Compute(IReadOnlyList<CgDiagramNode> nodes, IReadOnlyList<CgDiagramEdge> edges, CgDiagramLayoutOptions options)
    {
        var result = new Dictionary<string, Point>(StringComparer.Ordinal);
        if (nodes.Count == 0) return result;

        var layer = DiagramGraphTopology.LayerByBfs(nodes, edges);
        var groups = nodes.GroupBy(n => layer[n.Id]).OrderBy(g => g.Key).ToList();

        var xStep = options.NodeSize.Width + options.HorizontalSpacing;
        var yStep = options.NodeSize.Height + options.VerticalSpacing;

        var x = 0.0;
        foreach (var g in groups)
        {
            var col = g.OrderBy(n => n.Id, StringComparer.Ordinal).ToList();
            var totalH = (col.Count - 1) * yStep;
            var y = -totalH * 0.5;
            foreach (var n in col)
            {
                result[n.Id] = new Point(x, y);
                y += yStep;
            }
            x += xStep;
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

