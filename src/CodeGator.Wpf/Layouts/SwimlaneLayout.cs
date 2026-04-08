using System.Windows;
using CodeGator.Wpf;

namespace CodeGator.Wpf.Layouts;

/// <summary>
/// This class arranges nodes in stacked horizontal lanes grouped by <see cref="CgDiagramNode.SwimlaneId"/>.
/// </summary>
internal sealed class SwimlaneLayout : ICgDiagramLayout
{
    /// <summary>
    /// This method lays out nodes in rows separated by swimlane identifier while preserving order within each lane.
    /// </summary>
    public IReadOnlyDictionary<string, Point> Compute(IReadOnlyList<CgDiagramNode> nodes, IReadOnlyList<CgDiagramEdge> edges, CgDiagramLayoutOptions options)
    {
        var result = new Dictionary<string, Point>(StringComparer.Ordinal);
        if (nodes.Count == 0) return result;

        var lanes = nodes
            .GroupBy(n => string.IsNullOrWhiteSpace(n.SwimlaneId) ? "Default" : n.SwimlaneId!)
            .OrderBy(g => g.Key, StringComparer.Ordinal)
            .ToList();

        var xStep = options.NodeSize.Width + options.HorizontalSpacing;
        var yLaneStep = options.NodeSize.Height + options.VerticalSpacing * 2;

        var y = 0.0;
        foreach (var lane in lanes)
        {
            var inLane = lane.OrderBy(n => n.Id, StringComparer.Ordinal).ToList();
            var x = 0.0;
            foreach (var n in inLane)
            {
                result[n.Id] = new Point(x, y);
                x += xStep;
            }
            y += yLaneStep;
        }

        return result;
    }
}

