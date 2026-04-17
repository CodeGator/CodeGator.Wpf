using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CodeGator.Wpf;

/// <summary>
/// This class represents a directed link between two diagram nodes by endpoint ids.
/// </summary>
public sealed class CgDiagramEdge : INotifyPropertyChanged
{
    /// <summary>
    /// This constructor initializes a new instance of the CgDiagramEdge class.
    /// </summary>
    /// <param name="fromId">The source node id for the directed relationship.</param>
    /// <param name="toId">The target node id for the directed relationship.</param>
    /// <param name="label">Optional text rendered along the connector.</param>
    public CgDiagramEdge(string fromId, string toId, string? label = null)
    {
        FromId = fromId;
        ToId = toId;
        Label = label;
    }

    /// <summary>
    /// This property identifies the source node id for the directed relationship.
    /// </summary>
    public string FromId { get; }

    /// <summary>
    /// This property identifies the target node id for the directed relationship.
    /// </summary>
    public string ToId { get; }

    /// <summary>
    /// This property optionally describes the connector text rendered along the edge.
    /// </summary>
    public string? Label { get; }

    bool _isSelected;

    /// <summary>
    /// This property indicates whether the edge is selected on the diagram surface.
    /// </summary>
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            OnPropertyChanged();
        }
    }

    bool _isHovered;

    /// <summary>
    /// This property indicates hover when the pointer is over the edge hit target.
    /// </summary>
    public bool IsHovered
    {
        get => _isHovered;
        set
        {
            if (_isHovered == value) return;
            _isHovered = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// This event fires when a bindable property on the edge changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// This method raises <see cref="PropertyChanged"/> when a property value changes.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

