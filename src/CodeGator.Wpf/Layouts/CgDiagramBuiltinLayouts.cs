namespace CodeGator.Wpf.Layouts;

/// <summary>
/// This class selects concrete built-in layout implementations for each <see cref="CgDiagramLayoutKind"/> value.
/// </summary>
public static class CgDiagramBuiltinLayouts
{
    /// <summary>
    /// This method resolves the built-in layout implementation that corresponds to the given layout kind.
    /// </summary>
    public static ICgDiagramLayout For(CgDiagramLayoutKind kind) => kind switch
    {
        CgDiagramLayoutKind.HierarchicalTopDown => new HierarchicalTopDownLayout(),
        CgDiagramLayoutKind.HierarchicalLeftToRight => new HierarchicalLeftToRightLayout(),
        CgDiagramLayoutKind.Radial => new RadialLayout(),
        CgDiagramLayoutKind.ForceDirected => new ForceDirectedLayout(),
        CgDiagramLayoutKind.Swimlanes => new SwimlaneLayout(),
        CgDiagramLayoutKind.CircularRing => new CircularRingLayout(),
        _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null)
    };
}

