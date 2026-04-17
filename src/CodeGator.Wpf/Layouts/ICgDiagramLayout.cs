using System.Windows;
using CodeGator.Wpf;

namespace CodeGator.Wpf.Layouts;

/// <summary>
/// This interface represents strategies that position nodes on a diagram graph.
/// </summary>
public interface ICgDiagramLayout
{
    /// <summary>
    /// This method computes a top-left position for each node in the graph.
    /// </summary>
    /// <remarks>
    /// Implementations typically read node sizes from <paramref name="options"/> and may ignore
    /// <paramref name="edges"/> when a layout does not use edge structure.
    /// </remarks>
    /// <param name="nodes">The nodes to position.</param>
    /// <param name="edges">The directed edges between nodes, when the algorithm uses them.</param>
    /// <param name="options">Sizing, spacing, and simulation parameters for the layout pass.</param>
    /// <returns>A map from node id to top-left position in diagram content space.</returns>
    IReadOnlyDictionary<string, Point> Compute(
        IReadOnlyList<CgDiagramNode> nodes,
        IReadOnlyList<CgDiagramEdge> edges,
        CgDiagramLayoutOptions options);
}

