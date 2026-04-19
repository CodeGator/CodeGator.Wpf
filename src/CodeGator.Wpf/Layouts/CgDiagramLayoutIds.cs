namespace CodeGator.Wpf.Layouts;

/// <summary>
/// This class defines string ids for built-in diagram layout algorithms.
/// </summary>
/// <remarks>
/// Pass these values to <see cref="CgDiagramLayouts.Resolve"/> or register additional ids at startup with
/// <see cref="CgDiagramLayouts.Register"/>.
/// Interactive force-directed layout is provided by <see cref="CodeGator.Wpf.CgDiagram"/> instead of a layout id.
/// </remarks>
public static class CgDiagramLayoutIds
{
    /// <summary>
    /// This field identifies the breadth-first layer layout with rows downward.
    /// </summary>
    public const string HierarchicalTopDown = "HierarchicalTopDown";

    /// <summary>
    /// This field identifies the breadth-first layer layout with columns rightward.
    /// </summary>
    public const string HierarchicalLeftToRight = "HierarchicalLeftToRight";

    /// <summary>
    /// This field identifies the ring placement by BFS distance from roots.
    /// </summary>
    public const string Radial = "Radial";

    /// <summary>
    /// This field identifies the swimlane layout keyed by swimlane id.
    /// </summary>
    /// <remarks>
    /// Nodes use <see cref="CgDiagramNode.SwimlaneId"/> to assign lanes.
    /// </remarks>
    public const string Swimlanes = "Swimlanes";

    /// <summary>
    /// This field identifies the single-ring even distribution layout.
    /// </summary>
    public const string CircularRing = "CircularRing";

    /// <summary>
    /// This property lists every built-in layout id for pickers and tests.
    /// </summary>
    public static IReadOnlyList<string> All { get; } =
    [
        HierarchicalTopDown,
        HierarchicalLeftToRight,
        Radial,
        Swimlanes,
        CircularRing,
    ];
}
