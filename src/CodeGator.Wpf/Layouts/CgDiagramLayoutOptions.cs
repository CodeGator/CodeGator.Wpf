using System.Windows;

namespace CodeGator.Wpf.Layouts;

/// <summary>
/// This class holds sizing, spacing, and simulation parameters consumed by diagram layout algorithms.
/// </summary>
public sealed record CgDiagramLayoutOptions
{
    /// <summary>
    /// This property supplies the default width and height assumed for each node during layout.
    /// </summary>
    public Size NodeSize { get; init; }

    /// <summary>
    /// This property sets the horizontal gap between columns or side-by-side nodes.
    /// </summary>
    public double HorizontalSpacing { get; init; }

    /// <summary>
    /// This property sets the vertical gap between rows or stacked nodes.
    /// </summary>
    public double VerticalSpacing { get; init; }

    /// <summary>
    /// This property limits how many simulation steps run in force-directed layouts.
    /// </summary>
    public int ForceIterations { get; init; }

    /// <summary>
    /// This property seeds the pseudo-random generator so force-directed results stay reproducible.
    /// </summary>
    public int ForceSeed { get; init; }

    /// <summary>
    /// This method creates layout options with explicit spacing and force-directed tuning values.
    /// </summary>
    public CgDiagramLayoutOptions(
        Size nodeSize,
        double horizontalSpacing = 80,
        double verticalSpacing = 60,
        int forceIterations = 250,
        int forceSeed = 1)
    {
        NodeSize = nodeSize;
        HorizontalSpacing = horizontalSpacing;
        VerticalSpacing = verticalSpacing;
        ForceIterations = forceIterations;
        ForceSeed = forceSeed;
    }
}
