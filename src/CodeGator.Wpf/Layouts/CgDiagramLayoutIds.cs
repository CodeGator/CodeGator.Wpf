namespace CodeGator.Wpf.Layouts;

/// <summary>
/// This class defines stable string identifiers for built-in diagram layout algorithms.
/// </summary>
/// <remarks>
/// Pass these values to <see cref="CgDiagram.LayoutId"/>, <see cref="CgDiagram.ApplyLayout"/>, or
/// <see cref="CgDiagramLayouts.Resolve"/>. Register additional ids at startup with
/// <see cref="CgDiagramLayouts.Register"/>.
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
    /// This field identifies the iterative force-directed simulation layout.
    /// </summary>
    public const string ForceDirected = "ForceDirected";

    /// <summary>
    /// This field identifies the swimlane stacking layout by <see cref="CgDiagramNode.SwimlaneId"/>.
    /// </summary>
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
        ForceDirected,
        Swimlanes,
        CircularRing,
    ];
}
