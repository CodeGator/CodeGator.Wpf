using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using CodeGator.Wpf;
using CodeGator.Wpf.Layouts;

namespace Sample1;

/// <summary>
/// This class is the sample application's view model for diagram nodes, edges, zoom, and layout selection.
/// </summary>
public sealed class MainWindowViewModel : INotifyPropertyChanged
{
    /// <summary>
    /// This property exposes the sample diagram nodes bound to the <see cref="CgDiagram"/> control.
    /// </summary>
    public ObservableCollection<CgDiagramNode> Nodes { get; } = new();

    /// <summary>
    /// This property exposes the sample edges that connect nodes in the demonstration graph.
    /// </summary>
    public ObservableCollection<CgDiagramEdge> Edges { get; } = new();

    double _zoom = 1.0;

    /// <summary>
    /// This property mirrors the diagram zoom factor for binder diagnostics in the sample UI.
    /// </summary>
    public double Zoom
    {
        get => _zoom;
        set
        {
            if (Math.Abs(_zoom - value) < 0.0001) return;
            _zoom = value;
            OnPropertyChanged();
        }
    }

    double _panX;

    /// <summary>
    /// This property mirrors horizontal pan translation applied to the diagram surface.
    /// </summary>
    public double PanX
    {
        get => _panX;
        set
        {
            if (Math.Abs(_panX - value) < 0.0001) return;
            _panX = value;
            OnPropertyChanged();
        }
    }

    double _panY;

    /// <summary>
    /// This property mirrors vertical pan translation applied to the diagram surface.
    /// </summary>
    public double PanY
    {
        get => _panY;
        set
        {
            if (Math.Abs(_panY - value) < 0.0001) return;
            _panY = value;
            OnPropertyChanged();
        }
    }

    bool _showGrid = true;

    /// <summary>
    /// This property toggles diagram grid visibility in the sample chrome bindings.
    /// </summary>
    public bool ShowGrid
    {
        get => _showGrid;
        set
        {
            if (_showGrid == value) return;
            _showGrid = value;
            OnPropertyChanged();
        }
    }

    string _lastEvent = "Click or right-click a node/connector…";

    /// <summary>
    /// This property surfaces the latest user interaction or command message for the status area.
    /// </summary>
    public string LastEvent
    {
        get => _lastEvent;
        set
        {
            if (_lastEvent == value) return;
            _lastEvent = value;
            OnPropertyChanged();
        }
    }

    CgDiagramLayoutKind _layout = CgDiagramLayoutKind.HierarchicalTopDown;

    /// <summary>
    /// This property stores the layout algorithm selected in the sample combo box binding.
    /// </summary>
    public CgDiagramLayoutKind Layout
    {
        get => _layout;
        set
        {
            if (_layout == value) return;
            _layout = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// This property supplies enum values for populating the sample layout picker.
    /// </summary>
    public Array AvailableLayouts { get; } = Enum.GetValues(typeof(CgDiagramLayoutKind));

    /// <summary>
    /// This method restores zoom to one and resets pan offsets to the diagram origin.
    /// </summary>
    public void ResetView()
    {
        Zoom = 1.0;
        PanX = 0;
        PanY = 0;
    }

    /// <summary>
    /// This method repopulates nodes and edges with the built-in demonstration workflow graph.
    /// </summary>
    public void LoadSampleGraph()
    {
        Nodes.Clear();
        Edges.Clear();

        Nodes.Add(new CgDiagramNode("n1", "Start", "Entry point") { Position = new Point(40, 60), SwimlaneId = "Intake" });
        Nodes.Add(new CgDiagramNode("n2", "Validate", "Check inputs")
        {
            Position = new Point(280, 60),
            SwimlaneId = "Intake",
            Presentation = CgDiagramNodePresentation.SvgPath,
            SvgPathData = "M 50,0 L 100,50 L 50,100 L 0,50 Z", // diamond
            Width = 176,
            Height = 120
        });
        Nodes.Add(new CgDiagramNode("n3", "Compute", "Run core algorithm")
        {
            Position = new Point(520, 40),
            SwimlaneId = "Processing",
            Presentation = CgDiagramNodePresentation.SvgFile,
            SvgSource = "Assets/hex.svg",
            Width = 200,
            Height = 120
        });
        Nodes.Add(new CgDiagramNode("n4", "Persist", "Save results")
        {
            Position = new Point(520, 160),
            SwimlaneId = "Processing",
            Presentation = CgDiagramNodePresentation.SvgPath,
            SvgPathData = "M 10,0 H 90 A 10,10 0 0 1 100,10 V 90 A 10,10 0 0 1 90,100 H 10 A 10,10 0 0 1 0,90 V 10 A 10,10 0 0 1 10,0 Z",
            Width = 200,
            Height = 96
        });
        Nodes.Add(new CgDiagramNode("n5", "Done", "Return response") { Position = new Point(760, 100), SwimlaneId = "Output" });

        Edges.Add(new CgDiagramEdge("n1", "n2"));
        Edges.Add(new CgDiagramEdge("n2", "n3"));
        Edges.Add(new CgDiagramEdge("n2", "n4"));
        Edges.Add(new CgDiagramEdge("n3", "n5"));
        Edges.Add(new CgDiagramEdge("n4", "n5"));
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

