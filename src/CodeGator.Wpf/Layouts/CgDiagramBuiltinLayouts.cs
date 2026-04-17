namespace CodeGator.Wpf.Layouts;

/// <summary>
/// This class picks a built-in <see cref="ICgDiagramLayout"/> for each layout kind.
/// </summary>
public static class CgDiagramBuiltinLayouts
{
    /// <summary>
    /// This method returns the built-in layout implementation for the given kind.
    /// </summary>
    /// <param name="kind">The layout strategy to resolve.</param>
    /// <returns>The layout algorithm instance for <paramref name="kind"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="kind"/> is not a defined <see cref="CgDiagramLayoutKind"/> value.
    /// </exception>
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

