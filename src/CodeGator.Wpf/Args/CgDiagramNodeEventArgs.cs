using System.Windows;

namespace CodeGator.Wpf.Args;

/// <summary>
/// This class carries routed-event arguments when the pointer interacts with a diagram node.
/// </summary>
/// <remarks>
/// The event payload includes the target node and the pointer position in client coordinates.
/// </remarks>
public sealed class CgDiagramNodeEventArgs : RoutedEventArgs
{
    /// <summary>
    /// This method initializes event arguments for a routed diagram-node interaction.
    /// </summary>
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
    /// This property captures the pointer position relative to the diagram control origin.
    /// </summary>
    public Point ClientPoint { get; }
}

