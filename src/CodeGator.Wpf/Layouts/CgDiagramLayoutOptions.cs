using System.Windows;

namespace CodeGator.Wpf.Layouts;

/// <summary>
/// This class holds sizing, spacing, and parameters for diagram layout algorithms.
/// </summary>
public sealed record CgDiagramLayoutOptions
{
    /// <summary>
    /// This property holds default node width and height for layout algorithms.
    /// </summary>
    public Size NodeSize { get; init; }

    /// <summary>
    /// This property sets horizontal spacing between adjacent nodes or columns.
    /// </summary>
    public double HorizontalSpacing { get; init; }

    /// <summary>
    /// This property sets the vertical gap between rows or stacked nodes.
    /// </summary>
    public double VerticalSpacing { get; init; }

    /// <summary>
    /// This property caps iteration count for force-directed layout simulations.
    /// </summary>
    public int ForceIterations { get; init; }

    /// <summary>
    /// This property seeds the random generator for reproducible force layouts.
    /// </summary>
    public int ForceSeed { get; init; }

    /// <summary>
    /// This constructor initializes a new instance of the CgDiagramLayoutOptions class.
    /// </summary>
    /// <param name="nodeSize">The default width and height assumed for each node.</param>
    /// <param name="horizontalSpacing">The gap between columns or side-by-side nodes.</param>
    /// <param name="verticalSpacing">The gap between rows or stacked nodes.</param>
    /// <param name="forceIterations">The maximum force-directed simulation steps.</param>
    /// <param name="forceSeed">The seed for the pseudo-random generator.</param>
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
