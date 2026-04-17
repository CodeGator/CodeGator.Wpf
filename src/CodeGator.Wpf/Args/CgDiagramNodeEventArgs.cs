using System.Windows;

namespace CodeGator.Wpf.Args;

/// <summary>
/// This class carries routed-event data for pointer input on a diagram node.
/// </summary>
/// <remarks>
/// The event payload includes the target node and the pointer position in client coordinates.
/// </remarks>
public sealed class CgDiagramNodeEventArgs : RoutedEventArgs
{
    /// <summary>
    /// This constructor initializes a new instance of the CgDiagramNodeEventArgs class.
    /// </summary>
    /// <param name="routedEvent">The routed event identifier for this occurrence.</param>
    /// <param name="node">The diagram node associated with the interaction.</param>
    /// <param name="clientPoint">The pointer position in the diagram control's coordinates.</param>
    public CgDiagramNodeEventArgs(RoutedEvent routedEvent, CgDiagramNode node, Point clientPoint) : base(routedEvent)
    {
        Node = node;
        ClientPoint = clientPoint;
    }

    /// <summary>
    /// This property references the node that received the routed input event.
    /// </summary>
    public CgDiagramNode Node { get; }

    /// <summary>
    /// This property holds the pointer position relative to the diagram control origin.
    /// </summary>
    public Point ClientPoint { get; }
}

