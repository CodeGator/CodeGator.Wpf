using System.Windows;
using CodeGator.Wpf;

namespace CodeGator.Wpf.Layouts;

/// <summary>
/// This class places nodes at evenly spaced positions around a circular ring.
/// </summary>
internal sealed class CircularRingLayout : ICgDiagramLayout
{
    /// <summary>
    /// This method distributes nodes evenly around a single ring sized from the node count and spacing options.
    /// </summary>
    public IReadOnlyDictionary<string, Point> Compute(IReadOnlyList<CgDiagramNode> nodes, IReadOnlyList<CgDiagramEdge> edges, CgDiagramLayoutOptions options)
    {
        var result = new Dictionary<string, Point>(StringComparer.Ordinal);
        if (nodes.Count == 0) return result;

        var ordered = nodes.OrderBy(n => n.Id, StringComparer.Ordinal).ToList();
        var nCount = ordered.Count;

        var radius = Math.Max(120, (options.NodeSize.Width + options.HorizontalSpacing) * nCount / (2 * Math.PI));
        var cx = radius + options.NodeSize.Width;
        var cy = radius + options.NodeSize.Height;

        for (var i = 0; i < nCount; i++)
        {
            var t = (2 * Math.PI * i) / nCount;
            var x = cx + radius * Math.Cos(t) - options.NodeSize.Width * 0.5;
            var y = cy + radius * Math.Sin(t) - options.NodeSize.Height * 0.5;
            result[ordered[i].Id] = new Point(x, y);
        }

        return result;
    }
}

