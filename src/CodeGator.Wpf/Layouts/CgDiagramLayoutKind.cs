namespace CodeGator.Wpf.Layouts;

/// <summary>
/// This enumeration represents built-in layout strategies for diagram controls.
/// </summary>
/// <remarks>
/// Pass values to <see cref="CodeGator.Wpf.CgDiagram"/> layout APIs.
/// </remarks>
public enum CgDiagramLayoutKind
{
    /// <summary>
    /// This enumeration member lays out breadth-first layers with rows downward.
    /// </summary>
    HierarchicalTopDown,

    /// <summary>
    /// This enumeration member lays out breadth-first layers with columns rightward.
    /// </summary>
    HierarchicalLeftToRight,

    /// <summary>
    /// This enumeration member places nodes on rings by BFS distance from roots.
    /// </summary>
    Radial,

    /// <summary>
    /// This enumeration member uses an iterative force-directed simulation.
    /// </summary>
    ForceDirected,

    /// <summary>
    /// This enumeration member stacks lanes by <see cref="CgDiagramNode.SwimlaneId"/>.
    /// </summary>
    Swimlanes,

    /// <summary>
    /// This enumeration member places all nodes evenly on one circular ring.
    /// </summary>
    CircularRing,
}
