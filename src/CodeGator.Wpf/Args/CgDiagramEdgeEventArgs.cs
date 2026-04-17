using System.Windows;

namespace CodeGator.Wpf;

/// <summary>
/// This class carries routed-event data for pointer input on a diagram edge.
/// </summary>
/// <remarks>
/// The event payload includes the target edge and the pointer position in client coordinates.
/// </remarks>
public sealed class CgDiagramEdgeEventArgs : RoutedEventArgs
{
    /// <summary>
    /// This constructor initializes a new instance of the CgDiagramEdgeEventArgs class.
    /// </summary>
    /// <param name="routedEvent">The routed event identifier for this occurrence.</param>
    /// <param name="edge">The diagram edge associated with the interaction.</param>
    /// <param name="clientPoint">The pointer position in the diagram control's coordinates.</param>
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
    /// This property holds the pointer position relative to the diagram control origin.
    /// </summary>
    public Point ClientPoint { get; }
}

