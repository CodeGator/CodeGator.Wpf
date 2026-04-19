namespace CodeGator.Wpf.Layouts;

/// <summary>
/// This class defines string ids for built-in diagram layout algorithms.
/// </summary>
/// <remarks>
/// Pass these values to <see cref="CgDiagramLayouts.Resolve"/> or register additional ids at startup with
/// <see cref="CgDiagramLayouts.Register"/>.
/// Use <see cref="ForceDirected"/> with <see cref="CodeGator.Wpf.CgDiagram"/> for the built-in simulation instead of
/// <see cref="CgDiagramLayouts.Resolve"/>.
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
    /// This field identifies the interactive force-directed layout built into <see cref="CodeGator.Wpf.CgDiagram"/>.
    /// </summary>
    /// <remarks>
    /// This id is not registered with <see cref="CgDiagramLayouts"/>; pass it to <see cref="CgDiagram.LayoutId"/>
    /// or <see cref="CgDiagram.ApplyLayout"/> to run the internal simulation instead of a registered algorithm.
    /// </remarks>
    public const string ForceDirected = "ForceDirected";

    /// <summary>
    /// This property lists every built-in layout id for pickers and tests.
    /// </summary>
    /// <remarks>
    /// Excludes <see cref="ForceDirected"/> because it is not resolved through <see cref="CgDiagramLayouts"/>.
    /// </remarks>
    public static IReadOnlyList<string> All { get; } =
    [
        HierarchicalTopDown,
        HierarchicalLeftToRight,
        Radial,
        Swimlanes,
        CircularRing,
    ];
}
