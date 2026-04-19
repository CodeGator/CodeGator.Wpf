using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using CodeGator.Wpf;

namespace Sample1;

/// <summary>
/// This class is the sample application's view model for diagram nodes, edges, zoom, and camera state.
/// </summary>
public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    /// <summary>
    /// This property exposes nodes bound to the <see cref="CgDiagram"/>.
    /// </summary>
    public ObservableCollection<CgDiagramNode> NodesForce { get; } = new();

    /// <summary>
    /// This property exposes edges bound to the <see cref="CgDiagram"/>.
    /// </summary>
    public ObservableCollection<CgDiagramEdge> EdgesForce { get; } = new();

    double _zoomForce = 1.0;

    /// <summary>
    /// This property mirrors zoom for the force-directed diagram.
    /// </summary>
    public double ZoomForce
    {
        get => _zoomForce;
        set
        {
            if (Math.Abs(_zoomForce - value) < 0.0001) return;
            _zoomForce = value;
            OnPropertyChanged();
        }
    }

    double _panXForce;

    /// <summary>
    /// This property mirrors horizontal pan for the force-directed diagram.
    /// </summary>
    public double PanXForce
    {
        get => _panXForce;
        set
        {
            if (Math.Abs(_panXForce - value) < 0.0001) return;
            _panXForce = value;
            OnPropertyChanged();
        }
    }

    double _panYForce;

    /// <summary>
    /// This property mirrors vertical pan for the force-directed diagram.
    /// </summary>
    public double PanYForce
    {
        get => _panYForce;
        set
        {
            if (Math.Abs(_panYForce - value) < 0.0001) return;
            _panYForce = value;
            OnPropertyChanged();
        }
    }

    string _forceLastEvent = "";

    /// <summary>
    /// This property surfaces status for the force-directed diagram (print, load).
    /// </summary>
    public string ForceLastEvent
    {
        get => _forceLastEvent;
        set
        {
            if (_forceLastEvent == value) return;
            _forceLastEvent = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// This method restores zoom and pan for the force-directed diagram.
    /// </summary>
    public void ResetViewForce()
    {
        ZoomForce = 1.0;
        PanXForce = 0;
        PanYForce = 0;
    }

    /// <summary>
    /// This method repopulates the force-directed graph with the built-in demonstration workflow graph.
    /// </summary>
    public void LoadSampleGraphForce()
    {
        NodesForce.Clear();
        EdgesForce.Clear();
        AddSampleGraph(NodesForce, EdgesForce);
    }

    static void AddSampleGraph(ObservableCollection<CgDiagramNode> nodes, ObservableCollection<CgDiagramEdge> edges)
    {
        const double nw = 120;
        const double nh = 152;

        nodes.Add(new CgDiagramNode("n1", "Plain", "Surface, empty circle")
        {
            Position = new Point(40, 60),
            SwimlaneId = "Intake",
            Width = nw,
            Height = nh
        });
        nodes.Add(new CgDiagramNode("n2", "Text", "Surface, text glyph")
        {
            Position = new Point(280, 60),
            SwimlaneId = "Intake",
            Width = nw,
            Height = nh
        });
        nodes.Add(new CgDiagramNode("n3", "Path glyph", "Surface, SVG path glyph")
        {
            Position = new Point(520, 40),
            SwimlaneId = "Processing",
            Width = nw,
            Height = nh
        });
        nodes.Add(new CgDiagramNode("n4", "SVG glyph", "Surface, SVG file glyph")
        {
            Position = new Point(520, 160),
            SwimlaneId = "Processing",
            Width = nw,
            Height = nh
        });
        nodes.Add(new CgDiagramNode("n5", "Full SVG", "SvgFile fills the circle")
        {
            Position = new Point(760, 100),
            SwimlaneId = "Output",
            Width = nw,
            Height = nh
        });

        foreach (var n in nodes)
        {
            ApplyBaselineSampleNodeGraphics(n);
        }

        edges.Add(new CgDiagramEdge("n1", "n2"));
        edges.Add(new CgDiagramEdge("n2", "n3"));
        edges.Add(new CgDiagramEdge("n2", "n4"));
        edges.Add(new CgDiagramEdge("n3", "n5"));
        edges.Add(new CgDiagramEdge("n4", "n5"));
    }

    /// <summary>
    /// This method assigns the default five-node showcase: one distinct diagram node content style per id.
    /// </summary>
    static void ApplyBaselineSampleNodeGraphics(CgDiagramNode n)
    {
        n.CircleGlyphOffset = default;
        switch (n.Id)
        {
            case "n1":
                n.Presentation = CgDiagramNodePresentation.Surface;
                n.SvgPathData = null;
                n.SvgSource = null;
                n.CircleGlyphKind = CgDiagramNodeCircleGlyphKind.None;
                n.CircleGlyph = null;
                break;
            case "n2":
                n.Presentation = CgDiagramNodePresentation.Surface;
                n.SvgPathData = null;
                n.SvgSource = null;
                n.CircleGlyphKind = CgDiagramNodeCircleGlyphKind.Text;
                n.CircleGlyph = "Aa";
                break;
            case "n3":
                n.Presentation = CgDiagramNodePresentation.Surface;
                n.SvgPathData = null;
                n.SvgSource = null;
                n.CircleGlyphKind = CgDiagramNodeCircleGlyphKind.SvgPathData;
                n.CircleGlyph = "M 50,18 L 82,82 L 18,82 Z";
                break;
            case "n4":
                n.Presentation = CgDiagramNodePresentation.Surface;
                n.SvgPathData = null;
                n.SvgSource = null;
                n.CircleGlyphKind = CgDiagramNodeCircleGlyphKind.SvgFile;
                n.CircleGlyph = "Assets/hex.svg";
                break;
            case "n5":
                n.Presentation = CgDiagramNodePresentation.SvgFile;
                n.SvgPathData = null;
                n.SvgSource = "Assets/windows_security_shield.svg";
                n.CircleGlyphKind = CgDiagramNodeCircleGlyphKind.None;
                n.CircleGlyph = null;
                break;
        }
    }

    /// <summary>
    /// This event is raised after a bindable property value changes for WPF bindings.
    /// </summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>
    /// This method notifies listeners that a bindable property value has changed.
    /// </summary>
    void OnPropertyChanged([CallerMemberName] string? propertyName = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
}
