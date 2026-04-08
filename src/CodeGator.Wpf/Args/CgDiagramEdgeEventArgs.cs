using System.Windows;

namespace CodeGator.Wpf;

/// <summary>
/// This class carries routed-event arguments when the pointer interacts with a diagram edge.
/// </summary>
/// <remarks>
/// The event payload includes the target edge and the pointer position in client coordinates.
/// </remarks>
public sealed class CgDiagramEdgeEventArgs : RoutedEventArgs
{
    /// <summary>
    /// This method initializes event arguments for a routed diagram-edge interaction.
    /// </summary>
    public CgDiagramEdgeEventArgs(RoutedEvent routedEvent, CgDiagramEdge edge, Point clientPoint) : base(routedEvent)
    {
        Edge = edge;
        ClientPoint = clientPoint;
    }

    /// <summary>
    /// This property references the edge that received the routed input event.
    /// </summary>
    public CgDiagramEdge Edge { get; }

    /// <summary>
    /// This property captures the pointer position relative to the diagram control origin.
    /// </summary>
    public Point ClientPoint { get; }
}

