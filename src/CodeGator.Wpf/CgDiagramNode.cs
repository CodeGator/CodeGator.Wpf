using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;

namespace CodeGator.Wpf;

/// <summary>
/// This class represents a diagram vertex with identity, labels, and 2D position.
/// </summary>
/// <remarks>
/// Instances can carry presentation hints, swimlane grouping, SVG path or file sources, and surface sizing, and they notify property changes for selection and hover.
/// </remarks>
public sealed class CgDiagramNode : INotifyPropertyChanged
{
    /// <summary>
    /// This constructor initializes a new instance of the CgDiagramNode class.
    /// </summary>
    /// <param name="id">The stable identifier referenced by edges and layout results.</param>
    /// <param name="label">The primary title shown on the node surface.</param>
    /// <param name="description">Optional body text for surface presentation.</param>
    public CgDiagramNode(string id, string label, string? description = null)
    {
        Id = id;
        Label = label;
        Description = description;
    }

    /// <summary>
    /// This property holds the stable id referenced by edges and layout dictionaries.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// This property holds the primary display title shown on the node surface.
    /// </summary>
    public string Label { get; }

    /// <summary>
    /// This property holds optional body text under the title for surface presentation.
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
    /// This property holds SVG path data when <see cref="Presentation"/> is SvgPath.
    /// </summary>
    /// <remarks>
    /// Used when <see cref="Presentation"/> is <see cref="CgDiagramNodePresentation.SvgPath"/>.
    /// </remarks>
    public string? SvgPathData { get; set; }

    /// <summary>
    /// This property holds SVG source paths or URIs when presentation is SvgFile.
    /// </summary>
    /// <remarks>
    /// Used when <see cref="Presentation"/> is <see cref="CgDiagramNodePresentation.SvgFile"/>.
    /// </remarks>
    public string? SvgSource { get; set; }

    CgDiagramNodeCircleGlyphKind _circleGlyphKind = CgDiagramNodeCircleGlyphKind.None;
    string? _circleGlyph;
    Point _circleGlyphOffset;

    /// <summary>
    /// This property selects optional content inside the force-directed node circle.
    /// </summary>
    public CgDiagramNodeCircleGlyphKind CircleGlyphKind
    {
        get => _circleGlyphKind;
        set
        {
            if (_circleGlyphKind == value) return;
            _circleGlyphKind = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// This property holds text, path data, SVG source, or bitmap URI for <see cref="CircleGlyphKind"/>.
    /// </summary>
    public string? CircleGlyph
    {
        get => _circleGlyph;
        set
        {
            if (_circleGlyph == value) return;
            _circleGlyph = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// This property applies an extra translation in device-independent pixels to circle-glyph content.
    /// </summary>
    /// <remarks>
    /// Positive X moves right, positive Y moves down. Applied after template layout and automatic glyph
    /// centering so callers can fine-tune text, path, SVG file, or bitmap glyphs inside the node circle.
    /// </remarks>
    public Point CircleGlyphOffset
    {
        get => _circleGlyphOffset;
        set
        {
            if (_circleGlyphOffset == value) return;
            _circleGlyphOffset = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// This property sets explicit width and height for layout and hit-testing.
    /// </summary>
    public Size? Size { get; set; }

    double _width = 110;
    double _height = 140;

    /// <summary>
    /// This property sets width for layout and marquee when <see cref="Size"/> is null.
    /// </summary>
    public double Width
    {
        get => _width;
        set
        {
            if (_width == value) return;
            _width = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// This property sets height for layout and marquee if <see cref="Size"/> is null.
    /// </summary>
    /// <remarks>
    /// The default fits a title plus wrapped description for surface nodes without clipping.
    /// </remarks>
    public double Height
    {
        get => _height;
        set
        {
            if (_height == value) return;
            _height = value;
            OnPropertyChanged();
        }
    }

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
    /// This event fires when a bindable property on the node changes.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    bool _isSelected;

    /// <summary>
    /// This property indicates whether the node is selected for keyboard and clicks.
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
    /// This property indicates hover highlight while the pointer is over the node.
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
    /// This method raises <see cref="PropertyChanged"/> when a property value changes.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}

