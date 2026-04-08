using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace CodeGator.Wpf;

/// <summary>
/// This class represents a diagram vertex with stable identity, labels, and a two-dimensional position.
/// </summary>
/// <remarks>
/// Instances can carry presentation hints, swimlane grouping, SVG path or file sources, and surface sizing, and they notify property changes for selection and hover.
/// </remarks>
public sealed class CgDiagramNode : INotifyPropertyChanged
{
    /// <summary>
    /// This method creates a node with the given identifier, primary label, and optional description text.
    /// </summary>
    public CgDiagramNode(string id, string label, string? description = null)
    {
        Id = id;
        Label = label;
        Description = description;
    }

    /// <summary>
    /// This property exposes the stable identifier referenced by edges and layout dictionaries.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// This property holds the primary display title shown on the node surface.
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// This property carries optional body text rendered beneath the title when presentation is a surface card.
    /// </summary>
    public string? Description { get; }

    /// <summary>
    /// This property assigns the node to a swimlane group for lane-based layouts.
    /// </summary>
    public string? SwimlaneId { get; set; }

    /// <summary>
    /// This property selects how the node visual is composed in the diagram template.
    /// </summary>
    public CgDiagramNodePresentation Presentation { get; set; } = CgDiagramNodePresentation.Surface;

    /// <summary>
    /// This property supplies SVG path mini-language data when <see cref="Presentation"/> is <see cref="CgDiagramNodePresentation.SvgPath"/>.
    /// </summary>
    public string? SvgPathData { get; set; }

    /// <summary>
    /// This property supplies pack, relative, or absolute SVG locations when <see cref="Presentation"/> is <see cref="CgDiagramNodePresentation.SvgFile"/>.
    /// </summary>
    public string? SvgSource { get; set; }

    /// <summary>
    /// This property overrides measured width and height together when layout and hit-testing need explicit bounds.
    /// </summary>
    public Size? Size { get; set; }

    /// <summary>
    /// This property sets the node width used for layout padding and marquee hit-testing when <see cref="Size"/> is null.
    /// </summary>
    public double Width { get; set; } = 168;

    /// <summary>
    /// This property sets the node height used for layout padding and marquee hit-testing when <see cref="Size"/> is null.
    /// </summary>
    /// <remarks>
    /// The default fits a title plus wrapped description for surface nodes without clipping.
    /// </remarks>
    public double Height { get; set; } = 88;

    Point _position;

    /// <summary>
    /// This property stores the node origin in diagram content coordinates.
    /// </summary>
    public Point Position
    {
        get => _position;
        set
        {
            if (_position == value) return;
            _position = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// This event is raised when a bindable property on the node changes for MVVM consumers.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    bool _isSelected;

    /// <summary>
    /// This property tracks whether the node is the active selection for keyboard or click routing.
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
    /// This property tracks transient hover highlighting driven by pointer position over the node chrome.
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
    /// This method raises <see cref="PropertyChanged"/> for the calling property when values change.
    /// </summary>
    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

