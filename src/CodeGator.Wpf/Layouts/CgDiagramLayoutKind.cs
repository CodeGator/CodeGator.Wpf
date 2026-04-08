namespace CodeGator.Wpf.Layouts;

/// <summary>
/// This enumeration names built-in layout strategies for <see cref="CodeGator.Wpf.CgDiagram"/>.
/// </summary>
public enum CgDiagramLayoutKind
{
    /// <summary>
    /// This enumeration member orders nodes in breadth-first layers with rows progressing downward.
    /// </summary>
    HierarchicalTopDown,

    /// <summary>
    /// This enumeration member orders nodes in breadth-first layers with columns progressing rightward.
    /// </summary>
    HierarchicalLeftToRight,

    /// <summary>
    /// This enumeration member places nodes on concentric rings by breadth-first distance from roots.
    /// </summary>
    Radial,

    /// <summary>
    /// This enumeration member positions nodes using an iterative force-directed simulation.
    /// </summary>
    ForceDirected,

    /// <summary>
    /// This enumeration member groups nodes into stacked horizontal bands by <see cref="CgDiagramNode.SwimlaneId"/>.
    /// </summary>
    Swimlanes,

    /// <summary>
    /// This enumeration member distributes all nodes evenly around a single circular ring.
    /// </summary>
    CircularRing,
}
