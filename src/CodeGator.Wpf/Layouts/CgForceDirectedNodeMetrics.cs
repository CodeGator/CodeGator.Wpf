using System.Windows;

namespace CodeGator.Wpf.Layouts;

/// <summary>
/// This class computes circle and label metrics for force-directed diagram nodes.
/// </summary>
/// <remarks>
/// The default template draws an empty circular head whose diameter matches the node
/// <see cref="CgDiagramNode.Width"/> and places the primary title below the circle.
/// </remarks>
internal static class CgForceDirectedNodeMetrics
{
    /// <summary>
    /// This field reserves space below the circle for the title row.
    /// </summary>
    internal const double TitleBandBelowCircle = 32.0;

    /// <summary>
    /// This method returns the circular node head diameter for the default template.
    /// </summary>
    internal static double GetHeadDiameter(CgDiagramNode n)
    {
        var size = n.Size ?? new Size(n.Width, n.Height);
        var w = size.Width;
        var h = size.Height;
        var maxHead = Math.Max(48.0, h - TitleBandBelowCircle);
        return Math.Min(w, maxHead);
    }

    /// <summary>
    /// This method returns the circle center for hit-testing and connectors.
    /// </summary>
    internal static Point GetCircleCenter(CgDiagramNode n)
    {
        var size = n.Size ?? new Size(n.Width, n.Height);
        var w = size.Width;
        var head = GetHeadDiameter(n);
        return new Point(n.Position.X + w * 0.5, n.Position.Y + head * 0.5);
    }

    /// <summary>
    /// This method returns the circle radius used to clip connector endpoints.
    /// </summary>
    internal static double GetCircleRadius(CgDiagramNode n) => GetHeadDiameter(n) * 0.5;
}
