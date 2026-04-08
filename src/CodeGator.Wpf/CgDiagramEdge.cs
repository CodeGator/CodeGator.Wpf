using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace CodeGator.Wpf;

/// <summary>
/// This class represents a directed link between two diagram nodes, identified by their endpoint ids.
/// </summary>
public sealed class CgDiagramEdge : INotifyPropertyChanged
{
    /// <summary>
    /// This method creates an edge from one node id to another with an optional relationship label.
    /// </summary>
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
    /// This property tracks whether the edge is the active selection in the diagram surface.
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
    /// This property tracks transient hover highlighting when the pointer moves over the edge hit target.
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
    /// This event is raised when a bindable property on the edge changes for MVVM consumers.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// This method raises <see cref="PropertyChanged"/> for the calling property when values change.
    /// </summary>
    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

