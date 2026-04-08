using System.Windows;
using CodeGator.Wpf;

namespace CodeGator.Wpf.Layouts;

/// <summary>
/// This interface describes strategies that compute node positions for diagram graphs.
/// </summary>
public interface ICgDiagramLayout
{
    /// <summary>
    /// This method calculates a top-left position in content space for every node in the supplied graph.
    /// </summary>
    IReadOnlyDictionary<string, Point> Compute(
        IReadOnlyList<CgDiagramNode> nodes,
        IReadOnlyList<CgDiagramEdge> edges,
        CgDiagramLayoutOptions options);
}

