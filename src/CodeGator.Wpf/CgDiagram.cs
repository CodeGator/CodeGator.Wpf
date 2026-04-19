using System.Collections;
using System.Globalization;
using System.Linq;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using CodeGator.Wpf.Layouts;

namespace CodeGator.Wpf;

/// <summary>
/// This class provides a force-directed interactive diagram control for WPF.
/// </summary>
/// <remarks>
/// Nodes and edges use <see cref="CgDiagramNode"/> and <see cref="CgDiagramEdge"/>; simulation runs on a timer and reacts while nodes are dragged with the primary button,
/// or while the primary button drags empty space to translate the whole graph.
/// The default template draws empty circular nodes with titles below; hovering a node selects it, its edge-adjacent neighbors,
/// and incident connectors (hatched fill).
/// Force-directed physics live in <see cref="CdForceDirectedSimulation"/> instead of a registered <see cref="ICgDiagramLayout"/>.
/// Default node and connector appearance can be customized with dependency properties such as
/// <see cref="NodeChromeFill"/>, <see cref="NodeSelectionFill"/>, <see cref="EdgeSelectedStroke"/>, <see cref="NodeSize"/>,
/// and inherited <see cref="Control.FontFamily"/> / <see cref="Control.FontSize"/>.
/// </remarks>
[TemplatePart(Name = PartEdgesCanvas, Type = typeof(Canvas))]
[TemplatePart(Name = PartNodesItems, Type = typeof(ItemsControl))]
[TemplatePart(Name = PartScrollViewer, Type = typeof(ScrollViewer))]
[TemplatePart(Name = PartContentGrid, Type = typeof(Grid))]
public sealed class CgDiagram : Control
{
    const string PartEdgesCanvas = "PART_EdgesCanvas";
    const string PartNodesItems = "PART_NodesItems";
    const string PartScrollViewer = "PART_ScrollViewer";
    const string PartContentGrid = "PART_ContentGrid";

    Canvas? _edgesCanvas;
    ScrollViewer? _scrollViewer;
    Grid? _contentGrid;

    INotifyCollectionChanged? _nodesNotify;
    INotifyCollectionChanged? _edgesNotify;

    readonly Dictionary<CgDiagramNode, PropertyChangedEventHandler> _nodeHandlers = new();

    CgDiagramNode? _dragNode;
    Point _dragStartMouse;
    Point _dragStartNodePos;

    bool _graphDragActive;
    Point _graphDragStartMouse;
    readonly Dictionary<string, Point> _graphDragStartPositions = new(StringComparer.Ordinal);

    /// <summary>
    /// This field stores the last hover center node to skip redundant selection work.
    /// </summary>
    CgDiagramNode? _lastHoverCenterNode;

    /// <summary>
    /// This field holds the default hatch brush for selected nodes and connectors.
    /// </summary>
    static readonly Brush DefaultDiagramSelectionHatch = CreateDefaultDiagramSelectionHatch();

    CdForceDirectedSimulation? _sim;
    readonly DispatcherTimer _simulationTimer = new() { Interval = TimeSpan.FromMilliseconds(33) };

    bool _staticLayoutMode;

    /// <summary>
    /// This field suppresses per-node edge refresh during batched simulation writes.
    /// </summary>
    /// <remarks>
    /// When true, <see cref="HookNodePositionChanges"/> ignores <see cref="CgDiagramNode.Position"/> so a single
    /// <see cref="RefreshEdges"/> runs after batched simulation writes (avoids N redraws per tick).
    /// </remarks>
    bool _suppressPositionDrivenEdgeRefresh;

    /// <summary>
    /// This constructor registers default style metadata for CgDiagram.
    /// </summary>
    static CgDiagram()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(CgDiagram), new FrameworkPropertyMetadata(typeof(CgDiagram)));
    }

    static Brush CreateDefaultDiagramSelectionHatch()
    {
        return CgDiagramBrushes.CreateDiagonalHatchBrush(
            new SolidColorBrush(Color.FromRgb(0xF3, 0xF3, 0xF3)),
            new SolidColorBrush(Color.FromRgb(0xC4, 0xC4, 0xC4)),
            0.45,
            4.0);
    }

    /// <summary>
    /// This constructor initializes a new instance of the CgDiagram class.
    /// </summary>
    /// <remarks>
    /// Sets focusability, starts the simulation timer on load, and unsubscribes on unload.
    /// </remarks>
    public CgDiagram()
    {
        Focusable = true;
        _simulationTimer.Tick += OnSimulationTick;
        Loaded += OnDiagramLoaded;
        Unloaded += OnDiagramUnloaded;
    }

    void OnDiagramLoaded(object sender, RoutedEventArgs e)
    {
        RefreshSubscriptionsAndEdges();
        ApplyLayout(LayoutId);
        _simulationTimer.Start();
    }

    void OnDiagramUnloaded(object sender, RoutedEventArgs e)
    {
        _simulationTimer.Stop();
        UnsubscribeAll();
    }

    void OnSimulationTick(object? sender, EventArgs e)
    {
        if (!IsLoaded || _edgesCanvas is null)
        {
            return;
        }

        var nodes = (Nodes as IEnumerable)?.OfType<CgDiagramNode>().ToList() ?? new List<CgDiagramNode>();
        var edges = (Edges as IEnumerable)?.OfType<CgDiagramEdge>().ToList() ?? new List<CgDiagramEdge>();
        if (nodes.Count == 0)
        {
            return;
        }

        if (_graphDragActive)
        {
            return;
        }

        if (_staticLayoutMode)
        {
            return;
        }

        if (_sim is null)
        {
            ApplyLayout(LayoutId);
            return;
        }

        var options = new CgDiagramLayoutOptions(NodeSize);
        RunBatchedNodeWrites(() => _sim!.Step(nodes, options, _dragNode?.Id, normalizeDiagram: _dragNode is null));
        RefreshEdges();
    }

    /// <summary>
    /// This method runs an action while batching node position updates for edges.
    /// </summary>
    void RunBatchedNodeWrites(Action action)
    {
        _suppressPositionDrivenEdgeRefresh = true;
        try
        {
            action();
        }
        finally
        {
            _suppressPositionDrivenEdgeRefresh = false;
        }
    }

    /// <summary>
    /// This method re-seeds the simulation from the graph and writes node positions.
    /// </summary>
    /// <remarks>
    /// When <see cref="LayoutId"/> selects a registered <see cref="ICgDiagramLayout"/>, reapplies that layout;
    /// otherwise restarts the built-in force-directed simulation.
    /// </remarks>
    public void ResetSimulation() => ApplyLayout(LayoutId);

    /// <summary>
    /// This method applies <see cref="LayoutId"/> or an explicit layout id to the current graph.
    /// </summary>
    /// <param name="layoutId">Optional override; when null or empty, <see cref="LayoutId"/> is used.</param>
    public void ApplyLayout(string? layoutId = null)
    {
        if (!IsLoaded)
        {
            return;
        }

        var nodes = (Nodes as IEnumerable)?.OfType<CgDiagramNode>().ToList() ?? new List<CgDiagramNode>();
        if (nodes.Count == 0)
        {
            _sim = null;
            _staticLayoutMode = false;
            RefreshEdges();
            return;
        }

        var edges = (Edges as IEnumerable)?.OfType<CgDiagramEdge>().ToList() ?? new List<CgDiagramEdge>();
        var id = string.IsNullOrWhiteSpace(layoutId) ? LayoutId : layoutId!;
        if (IsBuiltInForceDirectedLayout(id))
        {
            _staticLayoutMode = false;
            _sim = new CdForceDirectedSimulation();
            var options = new CgDiagramLayoutOptions(NodeSize);
            RunBatchedNodeWrites(() =>
            {
                _sim.Reset(nodes, edges, options);
                _sim.Settle(nodes, options, 300, pinnedId: null);
            });
            RefreshEdges();
            _scrollViewer?.ScrollToTop();
            _scrollViewer?.ScrollToLeftEnd();
            return;
        }

        _staticLayoutMode = true;
        _sim = null;
        var layout = CgDiagramLayouts.Resolve(id);
        var staticOptions = new CgDiagramLayoutOptions(NodeSize);
        var positions = layout.Compute(nodes, edges, staticOptions);
        RunBatchedNodeWrites(() =>
        {
            foreach (var n in nodes)
            {
                if (positions.TryGetValue(n.Id, out var p))
                {
                    n.Position = p;
                }
            }
        });
        RefreshEdges();
        _scrollViewer?.ScrollToTop();
        _scrollViewer?.ScrollToLeftEnd();
    }

    static bool IsBuiltInForceDirectedLayout(string layoutId) =>
        string.IsNullOrWhiteSpace(layoutId) ||
        string.Equals(layoutId, CgDiagramLayoutIds.ForceDirected, StringComparison.Ordinal);

    /// <summary>
    /// This method finds template parts and refreshes subscriptions and edge visuals.
    /// </summary>
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _edgesCanvas = GetTemplateChild(PartEdgesCanvas) as Canvas;
        _scrollViewer = GetTemplateChild(PartScrollViewer) as ScrollViewer;
        _contentGrid = GetTemplateChild(PartContentGrid) as Grid;
        RefreshSubscriptionsAndEdges();
        if (IsLoaded)
        {
            ApplyLayout(LayoutId);
        }
    }

    /// <summary>
    /// This method prints the diagram through the system print dialog.
    /// </summary>
    /// <remarks>
    /// Zoom and pan are temporarily reset so printing reflects the full graph, then the previous view state is restored.
    /// </remarks>
    /// <param name="jobDescription">Optional print job name shown in the print queue.</param>
    /// <param name="fitToPage">When true, scales output to fit the printable area.</param>
    /// <returns>True when printing starts successfully; otherwise false if the dialog was cancelled or the template is missing.</returns>
    public bool Print(string? jobDescription = null, bool fitToPage = true)
    {
        if (_contentGrid is null)
        {
            return false;
        }

        var dialog = new PrintDialog();
        if (dialog.ShowDialog() != true)
        {
            return false;
        }

        var savedZoom = Zoom;
        var savedPanX = PanX;
        var savedPanY = PanY;
        var savedHOffset = _scrollViewer?.HorizontalOffset ?? 0.0;
        var savedVOffset = _scrollViewer?.VerticalOffset ?? 0.0;

        try
        {
            Zoom = 1.0;
            PanX = 0.0;
            PanY = 0.0;
            _scrollViewer?.ScrollToHorizontalOffset(0.0);
            _scrollViewer?.ScrollToVerticalOffset(0.0);

            Dispatcher.Invoke(() => { }, DispatcherPriority.Render);
            UpdateLayout();
            _contentGrid.UpdateLayout();

            var contentW = Math.Max(1.0, _contentGrid.ActualWidth);
            var contentH = Math.Max(1.0, _contentGrid.ActualHeight);
            var printableW = Math.Max(1.0, dialog.PrintableAreaWidth);
            var printableH = Math.Max(1.0, dialog.PrintableAreaHeight);

            var scale = fitToPage
                ? Math.Min(printableW / contentW, printableH / contentH)
                : 1.0;
            scale = Math.Max(0.01, scale);

            var offsetX = (printableW - contentW * scale) * 0.5;
            var offsetY = (printableH - contentH * scale) * 0.5;

            var dv = new DrawingVisual();
            using (var dc = dv.RenderOpen())
            {
                dc.PushTransform(new TranslateTransform(offsetX, offsetY));
                dc.PushTransform(new ScaleTransform(scale, scale));
                dc.DrawRectangle(new VisualBrush(_contentGrid) { Stretch = Stretch.None }, null, new Rect(0, 0, contentW, contentH));
                dc.Pop();
                dc.Pop();
            }

            dialog.PrintVisual(dv, jobDescription ?? "CgDiagram");
            return true;
        }
        finally
        {
            Zoom = savedZoom;
            PanX = savedPanX;
            PanY = savedPanY;
            _scrollViewer?.ScrollToHorizontalOffset(savedHOffset);
            _scrollViewer?.ScrollToVerticalOffset(savedVOffset);

            Dispatcher.Invoke(() => { }, DispatcherPriority.Render);
            UpdateLayout();
        }
    }

    /// <summary>
    /// This property binds the <see cref="CgDiagramNode"/> items shown on the surface.
    /// </summary>
    public IEnumerable? Nodes
    {
        get => (IEnumerable?)GetValue(NodesProperty);
        set => SetValue(NodesProperty, value);
    }

    /// <summary>
    /// This property exposes <see cref="NodesProperty"/>.
    /// </summary>
    public static readonly DependencyProperty NodesProperty =
        DependencyProperty.Register(
            nameof(Nodes),
            typeof(IEnumerable),
            typeof(CgDiagram),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, (d, _) => ((CgDiagram)d).OnNodesChanged()));

    /// <summary>
    /// This method refreshes hooks and edges when the nodes collection changes.
    /// </summary>
    void OnNodesChanged()
    {
        RefreshSubscriptionsAndEdges();
        ApplyLayout(LayoutId);
    }

    /// <summary>
    /// This property binds the <see cref="CgDiagramEdge"/> items drawn as connectors.
    /// </summary>
    public IEnumerable? Edges
    {
        get => (IEnumerable?)GetValue(EdgesProperty);
        set => SetValue(EdgesProperty, value);
    }

    /// <summary>
    /// This property exposes <see cref="EdgesProperty"/>.
    /// </summary>
    public static readonly DependencyProperty EdgesProperty =
        DependencyProperty.Register(
            nameof(Edges),
            typeof(IEnumerable),
            typeof(CgDiagram),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, (d, _) => ((CgDiagram)d).OnEdgesChanged()));

    /// <summary>
    /// This property selects the active layout algorithm or built-in force-directed mode.
    /// </summary>
    public string LayoutId
    {
        get => (string)GetValue(LayoutIdProperty);
        set => SetValue(LayoutIdProperty, value);
    }

    /// <summary>
    /// This property exposes <see cref="LayoutIdProperty"/>.
    /// </summary>
    public static readonly DependencyProperty LayoutIdProperty =
        DependencyProperty.Register(
            nameof(LayoutId),
            typeof(string),
            typeof(CgDiagram),
            new FrameworkPropertyMetadata(
                CgDiagramLayoutIds.ForceDirected,
                FrameworkPropertyMetadataOptions.BindsTwoWayByDefault,
                (d, _) => ((CgDiagram)d).OnLayoutIdChanged()));

    /// <summary>
    /// This method reapplies layout when <see cref="LayoutId"/> changes at runtime.
    /// </summary>
    void OnLayoutIdChanged()
    {
        if (IsLoaded)
        {
            ApplyLayout();
        }
    }

    /// <summary>
    /// This method refreshes hooks and edges when the edges collection changes.
    /// </summary>
    void OnEdgesChanged()
    {
        RefreshSubscriptionsAndEdges();
        ApplyLayout(LayoutId);
    }

    /// <summary>
    /// This property holds the data template for each node in the items control.
    /// </summary>
    public DataTemplate? NodeTemplate
    {
        get => (DataTemplate?)GetValue(NodeTemplateProperty);
        set => SetValue(NodeTemplateProperty, value);
    }

    /// <summary>
    /// This property exposes <see cref="NodeTemplateProperty"/>.
    /// </summary>
    public static readonly DependencyProperty NodeTemplateProperty =
        DependencyProperty.Register(nameof(NodeTemplate), typeof(DataTemplate), typeof(CgDiagram), new PropertyMetadata(null));

    /// <summary>
    /// This property enables or disables the background grid behind content.
    /// </summary>
    public bool ShowGrid
    {
        get => (bool)GetValue(ShowGridProperty);
        set => SetValue(ShowGridProperty, value);
    }

    /// <summary>
    /// This property exposes <see cref="ShowGridProperty"/>.
    /// </summary>
    public static readonly DependencyProperty ShowGridProperty =
        DependencyProperty.Register(nameof(ShowGrid), typeof(bool), typeof(CgDiagram), new PropertyMetadata(false));

    /// <summary>
    /// This property sets the brush for background grid lines.
    /// </summary>
    public Brush? GridBrush
    {
        get => (Brush?)GetValue(GridBrushProperty);
        set => SetValue(GridBrushProperty, value);
    }

    /// <summary>
    /// This property exposes <see cref="GridBrushProperty"/>.
    /// </summary>
    public static readonly DependencyProperty GridBrushProperty =
        DependencyProperty.Register(nameof(GridBrush), typeof(Brush), typeof(CgDiagram), new PropertyMetadata(null));

    /// <summary>
    /// This property controls the opacity applied to grid line rendering.
    /// </summary>
    public double GridOpacity
    {
        get => (double)GetValue(GridOpacityProperty);
        set => SetValue(GridOpacityProperty, value);
    }

    /// <summary>
    /// This property exposes <see cref="GridOpacityProperty"/>.
    /// </summary>
    public static readonly DependencyProperty GridOpacityProperty =
        DependencyProperty.Register(nameof(GridOpacity), typeof(double), typeof(CgDiagram), new PropertyMetadata(0.35));

    /// <summary>
    /// This property gets or sets zoom applied through layout transforms.
    /// </summary>
    public double Zoom
    {
        get => (double)GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
    }

    /// <summary>
    /// This property exposes <see cref="ZoomProperty"/>.
    /// </summary>
    public static readonly DependencyProperty ZoomProperty =
        DependencyProperty.Register(
            nameof(Zoom),
            typeof(double),
            typeof(CgDiagram),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, (d, _) => ((CgDiagram)d).CoerceAndRefresh()));

    /// <summary>
    /// This property defines the lower bound applied when coercing <see cref="Zoom"/>.
    /// </summary>
    public double MinZoom
    {
        get => (double)GetValue(MinZoomProperty);
        set => SetValue(MinZoomProperty, value);
    }

    /// <summary>
    /// This property exposes <see cref="MinZoomProperty"/>.
    /// </summary>
    public static readonly DependencyProperty MinZoomProperty =
        DependencyProperty.Register(nameof(MinZoom), typeof(double), typeof(CgDiagram), new PropertyMetadata(0.25, (d, _) => ((CgDiagram)d).CoerceAndRefresh()));

    /// <summary>
    /// This property defines the upper bound applied when coercing <see cref="Zoom"/>.
    /// </summary>
    public double MaxZoom
    {
        get => (double)GetValue(MaxZoomProperty);
        set => SetValue(MaxZoomProperty, value);
    }

    /// <summary>
    /// This property exposes <see cref="MaxZoomProperty"/>.
    /// </summary>
    public static readonly DependencyProperty MaxZoomProperty =
        DependencyProperty.Register(nameof(MaxZoom), typeof(double), typeof(CgDiagram), new PropertyMetadata(6.0, (d, _) => ((CgDiagram)d).CoerceAndRefresh()));

    /// <summary>
    /// This property offsets the diagram content horizontally in control space.
    /// </summary>
    public double PanX
    {
        get => (double)GetValue(PanXProperty);
        set => SetValue(PanXProperty, value);
    }

    /// <summary>
    /// This property exposes <see cref="PanXProperty"/>.
    /// </summary>
    public static readonly DependencyProperty PanXProperty =
        DependencyProperty.Register(nameof(PanX), typeof(double), typeof(CgDiagram), new PropertyMetadata(0.0));

    /// <summary>
    /// This property offsets the diagram content vertically in control space.
    /// </summary>
    public double PanY
    {
        get => (double)GetValue(PanYProperty);
        set => SetValue(PanYProperty, value);
    }

    /// <summary>
    /// This property exposes <see cref="PanYProperty"/>.
    /// </summary>
    public static readonly DependencyProperty PanYProperty =
        DependencyProperty.Register(nameof(PanY), typeof(double), typeof(CgDiagram), new PropertyMetadata(0.0));

    /// <summary>
    /// This property sets the default stroke brush for connector lines.
    /// </summary>
    public Brush EdgeStroke
    {
        get => (Brush)GetValue(EdgeStrokeProperty);
        set => SetValue(EdgeStrokeProperty, value);
    }

    /// <summary>
    /// This property exposes <see cref="EdgeStrokeProperty"/>.
    /// </summary>
    public static readonly DependencyProperty EdgeStrokeProperty =
        DependencyProperty.Register(
            nameof(EdgeStroke),
            typeof(Brush),
            typeof(CgDiagram),
            new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0x6E, 0x6E, 0x76)), (d, _) => ((CgDiagram)d).RefreshEdges()));

    /// <summary>
    /// This property sets the stroke thickness of connector lines in pixels.
    /// </summary>
    public double EdgeThickness
    {
        get => (double)GetValue(EdgeThicknessProperty);
        set => SetValue(EdgeThicknessProperty, value);
    }

    /// <summary>
    /// This property exposes <see cref="EdgeThicknessProperty"/>.
    /// </summary>
    public static readonly DependencyProperty EdgeThicknessProperty =
        DependencyProperty.Register(nameof(EdgeThickness), typeof(double), typeof(CgDiagram), new PropertyMetadata(1.0, (d, _) => ((CgDiagram)d).RefreshEdges()));

    /// <summary>
    /// This property sets the fill brush for force-directed node circles.
    /// </summary>
    public Brush NodeChromeFill
    {
        get => (Brush)GetValue(NodeChromeFillProperty);
        set => SetValue(NodeChromeFillProperty, value);
    }

    /// <summary>
    /// This property exposes <see cref="NodeChromeFillProperty"/>.
    /// </summary>
    public static readonly DependencyProperty NodeChromeFillProperty =
        DependencyProperty.Register(
            nameof(NodeChromeFill),
            typeof(Brush),
            typeof(CgDiagram),
            new FrameworkPropertyMetadata(Brushes.White, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>
    /// This property sets the outline brush for force-directed node circles.
    /// </summary>
    public Brush NodeChromeStroke
    {
        get => (Brush)GetValue(NodeChromeStrokeProperty);
        set => SetValue(NodeChromeStrokeProperty, value);
    }

    /// <summary>
    /// This property exposes <see cref="NodeChromeStrokeProperty"/>.
    /// </summary>
    public static readonly DependencyProperty NodeChromeStrokeProperty =
        DependencyProperty.Register(
            nameof(NodeChromeStroke),
            typeof(Brush),
            typeof(CgDiagram),
            new FrameworkPropertyMetadata(new SolidColorBrush(Color.FromRgb(0x6E, 0x6E, 0x76)), FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>
    /// This property sets the outline thickness for force-directed node circles.
    /// </summary>
    public double NodeChromeStrokeThickness
    {
        get => (double)GetValue(NodeChromeStrokeThicknessProperty);
        set => SetValue(NodeChromeStrokeThicknessProperty, value);
    }

    /// <summary>
    /// This property exposes <see cref="NodeChromeStrokeThicknessProperty"/>.
    /// </summary>
    public static readonly DependencyProperty NodeChromeStrokeThicknessProperty =
        DependencyProperty.Register(
            nameof(NodeChromeStrokeThickness),
            typeof(double),
            typeof(CgDiagram),
            new FrameworkPropertyMetadata(1.0, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>
    /// This property sets the fill brush when a node is selected (default hatch).
    /// </summary>
    public Brush NodeSelectionFill
    {
        get => (Brush)GetValue(NodeSelectionFillProperty);
        set => SetValue(NodeSelectionFillProperty, value);
    }

    /// <summary>
    /// This property exposes <see cref="NodeSelectionFillProperty"/>.
    /// </summary>
    public static readonly DependencyProperty NodeSelectionFillProperty =
        DependencyProperty.Register(
            nameof(NodeSelectionFill),
            typeof(Brush),
            typeof(CgDiagram),
            new FrameworkPropertyMetadata(DefaultDiagramSelectionHatch, FrameworkPropertyMetadataOptions.AffectsRender));

    /// <summary>
    /// This property sets the stroke for selected connectors and arrowheads.
    /// </summary>
    public Brush EdgeSelectedStroke
    {
        get => (Brush)GetValue(EdgeSelectedStrokeProperty);
        set => SetValue(EdgeSelectedStrokeProperty, value);
    }

    /// <summary>
    /// This property exposes <see cref="EdgeSelectedStrokeProperty"/>.
    /// </summary>
    public static readonly DependencyProperty EdgeSelectedStrokeProperty =
        DependencyProperty.Register(
            nameof(EdgeSelectedStroke),
            typeof(Brush),
            typeof(CgDiagram),
            new PropertyMetadata(DefaultDiagramSelectionHatch, (d, _) => ((CgDiagram)d).RefreshEdges()));

    /// <summary>
    /// This property adds to <see cref="EdgeThickness"/> when a connector is selected.
    /// </summary>
    public double EdgeSelectionThicknessBoost
    {
        get => (double)GetValue(EdgeSelectionThicknessBoostProperty);
        set => SetValue(EdgeSelectionThicknessBoostProperty, value);
    }

    /// <summary>
    /// This property exposes <see cref="EdgeSelectionThicknessBoostProperty"/>.
    /// </summary>
    public static readonly DependencyProperty EdgeSelectionThicknessBoostProperty =
        DependencyProperty.Register(
            nameof(EdgeSelectionThicknessBoost),
            typeof(double),
            typeof(CgDiagram),
            new PropertyMetadata(0.35, (d, _) => ((CgDiagram)d).RefreshEdges()));

    /// <summary>
    /// This property sets a template anchor point for node adorners and overlays.
    /// </summary>
    public Point NodeAnchor
    {
        get => (Point)GetValue(NodeAnchorProperty);
        set => SetValue(NodeAnchorProperty, value);
    }

    /// <summary>
    /// This property exposes <see cref="NodeAnchorProperty"/>.
    /// </summary>
    public static readonly DependencyProperty NodeAnchorProperty =
        DependencyProperty.Register(nameof(NodeAnchor), typeof(Point), typeof(CgDiagram), new PropertyMetadata(new Point(80, 22), (d, _) => ((CgDiagram)d).RefreshEdges()));

    /// <summary>
    /// This property holds default node size when nodes do not supply explicit bounds.
    /// </summary>
    public Size NodeSize
    {
        get => (Size)GetValue(NodeSizeProperty);
        set => SetValue(NodeSizeProperty, value);
    }

    /// <summary>
    /// This property exposes <see cref="NodeSizeProperty"/>.
    /// </summary>
    public static readonly DependencyProperty NodeSizeProperty =
        DependencyProperty.Register(nameof(NodeSize), typeof(Size), typeof(CgDiagram), new PropertyMetadata(new Size(110, 140), (d, _) =>
        {
            var diagram = (CgDiagram)d;
            diagram.ApplyDiagramNodeSizeDefaults();
            diagram.RefreshEdges();
        }));

    /// <summary>
    /// This property pads measured bounds before computing scrollable width and height.
    /// </summary>
    public Thickness ContentPadding
    {
        get => (Thickness)GetValue(ContentPaddingProperty);
        set => SetValue(ContentPaddingProperty, value);
    }

    /// <summary>
    /// This property exposes <see cref="ContentPaddingProperty"/>.
    /// </summary>
    public static readonly DependencyProperty ContentPaddingProperty =
        DependencyProperty.Register(nameof(ContentPadding), typeof(Thickness), typeof(CgDiagram), new PropertyMetadata(new Thickness(40), (d, _) => ((CgDiagram)d).RefreshEdges()));

    /// <summary>
    /// This property gets the scrollable content width including padding.
    /// </summary>
    public double ContentWidth
    {
        get => (double)GetValue(ContentWidthProperty);
        private set => SetValue(ContentWidthPropertyKey, value);
    }

    /// <summary>
    /// This field holds the key for the read-only <see cref="ContentWidth"/> property.
    /// </summary>
    static readonly DependencyPropertyKey ContentWidthPropertyKey =
        DependencyProperty.RegisterReadOnly(nameof(ContentWidth), typeof(double), typeof(CgDiagram), new PropertyMetadata(0.0));

    /// <summary>
    /// This field identifies the <see cref="ContentWidthProperty"/>.
    /// </summary>
    public static readonly DependencyProperty ContentWidthProperty = ContentWidthPropertyKey.DependencyProperty;

    /// <summary>
    /// This property gets the scrollable content height including padding.
    /// </summary>
    public double ContentHeight
    {
        get => (double)GetValue(ContentHeightProperty);
        private set => SetValue(ContentHeightPropertyKey, value);
    }

    /// <summary>
    /// This field holds the key for the read-only <see cref="ContentHeight"/> property.
    /// </summary>
    static readonly DependencyPropertyKey ContentHeightPropertyKey =
        DependencyProperty.RegisterReadOnly(nameof(ContentHeight), typeof(double), typeof(CgDiagram), new PropertyMetadata(0.0));

    /// <summary>
    /// This field identifies the <see cref="ContentHeightProperty"/>.
    /// </summary>
    public static readonly DependencyProperty ContentHeightProperty = ContentHeightPropertyKey.DependencyProperty;

    /// <summary>
    /// This method clamps zoom to min and max values and refreshes edge visuals.
    /// </summary>
    void CoerceAndRefresh()
    {
        if (double.IsNaN(Zoom) || Zoom <= 0)
        {
            Zoom = 1.0;
        }
        Zoom = Math.Clamp(Zoom, Math.Max(0.01, MinZoom), Math.Max(MinZoom, MaxZoom));
        RefreshEdges();
    }

    /// <summary>
    /// This method handles preview wheel input to adjust zoom before the scroll viewer.
    /// </summary>
    /// <param name="e">The mouse wheel event arguments.</param>
    protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
    {
        // ScrollViewer handles wheel by default; use Preview to keep zoom working.
        base.OnPreviewMouseWheel(e);
        var delta = e.Delta > 0 ? 1.10 : 1 / 1.10;
        Zoom = Math.Clamp(Zoom * delta, MinZoom, MaxZoom);
        e.Handled = true;
    }

    /// <summary>
    /// This method adjusts zoom from bubbled mouse wheel input.
    /// </summary>
    /// <param name="e">The mouse wheel event arguments.</param>
    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        var delta = e.Delta > 0 ? 1.10 : 1 / 1.10;
        Zoom = Math.Clamp(Zoom * delta, MinZoom, MaxZoom);
        e.Handled = true;
    }

    /// <summary>
    /// This method handles preview mouse down before children mark events handled.
    /// </summary>
    /// <param name="e">The mouse button event arguments.</param>
    protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
    {
        // ScrollViewer can mark mouse input as handled; grab it early.
        base.OnPreviewMouseDown(e);
        HandleMouseDown(e);
    }

    /// <summary>
    /// This method handles mouse down that reaches the control after preview routing.
    /// </summary>
    /// <param name="e">The mouse button event arguments.</param>
    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        HandleMouseDown(e);
    }

    /// <summary>
    /// This method starts a node drag when the primary button hits a node.
    /// </summary>
    /// <param name="e">The mouse button event arguments.</param>
    void HandleMouseDown(MouseButtonEventArgs e)
    {
        if (e.Handled)
        {
            return;
        }

        if (IsOriginalSourceOnDiagramScrollChrome(e.OriginalSource as DependencyObject))
        {
            return;
        }

        Focus();

        if (e.ChangedButton != MouseButton.Left)
        {
            return;
        }

        if (TryGetNodeFromOriginalSource(e.OriginalSource as DependencyObject, out var node))
        {
            _dragNode = node;
            _dragStartMouse = e.GetPosition(this);
            _dragStartNodePos = node.Position;
            CaptureMouse();
            e.Handled = true;
            return;
        }

        var nodes = (Nodes as IEnumerable)?.OfType<CgDiagramNode>().ToList() ?? new List<CgDiagramNode>();
        if (nodes.Count == 0)
        {
            return;
        }

        _graphDragActive = true;
        _graphDragStartMouse = e.GetPosition(this);
        _graphDragStartPositions.Clear();
        foreach (var n in nodes)
        {
            _graphDragStartPositions[n.Id] = n.Position;
        }

        CaptureMouse();
        e.Handled = true;
    }

    /// <summary>
    /// This method forwards move input to node dragging.
    /// </summary>
    /// <param name="e">The mouse event arguments.</param>
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        HandleMouseMove(e);
    }

    /// <summary>
    /// This method updates drag state for the current pointer move.
    /// </summary>
    /// <param name="e">The mouse event arguments.</param>
    void HandleMouseMove(MouseEventArgs e)
    {
        if (e.Handled)
        {
            return;
        }

        var p = e.GetPosition(this);

        if (_graphDragActive && IsMouseCaptured)
        {
            var dx = (p.X - _graphDragStartMouse.X) / Math.Max(0.0001, Zoom);
            var dy = (p.Y - _graphDragStartMouse.Y) / Math.Max(0.0001, Zoom);
            var nodes = (Nodes as IEnumerable)?.OfType<CgDiagramNode>().ToList() ?? new List<CgDiagramNode>();
            RunBatchedNodeWrites(() =>
            {
                foreach (var n in nodes)
                {
                    if (_graphDragStartPositions.TryGetValue(n.Id, out var start))
                    {
                        n.Position = new Point(start.X + dx, start.Y + dy);
                    }
                }
            });
            RefreshEdges();
            e.Handled = true;
            return;
        }

        if (_dragNode is not null && IsMouseCaptured)
        {
            var dx = (p.X - _dragStartMouse.X) / Math.Max(0.0001, Zoom);
            var dy = (p.Y - _dragStartMouse.Y) / Math.Max(0.0001, Zoom);
            _dragNode.Position = new Point(_dragStartNodePos.X + dx, _dragStartNodePos.Y + dy);
            RefreshEdges();
            e.Handled = true;
        }
    }

    /// <summary>
    /// This method completes pointer operations started during preview mouse down.
    /// </summary>
    /// <param name="e">The mouse button event arguments.</param>
    protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseUp(e);
        HandleMouseUp(e);
    }

    /// <summary>
    /// This method completes pointer operations when mouse up bubbles normally.
    /// </summary>
    /// <param name="e">The mouse button event arguments.</param>
    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        HandleMouseUp(e);
    }

    /// <summary>
    /// This method ends an active node drag when the primary button is released.
    /// </summary>
    /// <param name="e">The mouse button event arguments.</param>
    void HandleMouseUp(MouseButtonEventArgs e)
    {
        if (e.Handled)
        {
            return;
        }

        if (e.ChangedButton == MouseButton.Left && _graphDragActive)
        {
            _graphDragActive = false;
            _graphDragStartPositions.Clear();
            if (IsMouseCaptured)
            {
                ReleaseMouseCapture();
            }

            var nodes = (Nodes as IEnumerable)?.OfType<CgDiagramNode>().ToList() ?? new List<CgDiagramNode>();
            _sim?.SyncCentersFromNodes(nodes);

            e.Handled = true;
            return;
        }

        if (e.ChangedButton == MouseButton.Left && _dragNode is not null)
        {
            _dragNode = null;
            if (IsMouseCaptured)
            {
                ReleaseMouseCapture();
            }

            e.Handled = true;
        }
    }

    /// <summary>
    /// This method returns true when the hit target is the diagram scroll bar chrome.
    /// </summary>
    /// <remarks>
    /// True when the original source is under a <see cref="ScrollBar"/> in <c>PART_ScrollViewer</c>.
    /// </remarks>
    bool IsOriginalSourceOnDiagramScrollChrome(DependencyObject? original)
    {
        if (_scrollViewer is null || original is null)
        {
            return false;
        }

        for (var d = original; d is not null; d = VisualTreeHelper.GetParent(d))
        {
            if (d is not ScrollBar sb)
            {
                continue;
            }

            for (var x = (DependencyObject?)sb; x is not null; x = VisualTreeHelper.GetParent(x))
            {
                if (ReferenceEquals(x, _scrollViewer))
                {
                    return true;
                }
            }

            return false;
        }

        return false;
    }

    /// <summary>
    /// This method finds a <see cref="CgDiagramNode"/> from a hit-test target upward.
    /// </summary>
    /// <param name="original">The original source of the input event.</param>
    /// <param name="node">The node found in the visual tree, if any.</param>
    /// <returns>True when a node data context is found; otherwise false.</returns>
    static bool TryGetNodeFromOriginalSource(DependencyObject? original, out CgDiagramNode node)
    {
        node = null!;
        for (var d = original; d is not null; d = VisualTreeHelper.GetParent(d))
        {
            if (d is FrameworkElement fe && fe.DataContext is CgDiagramNode n)
            {
                node = n;
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// This method updates hover state when the pointer enters the control.
    /// </summary>
    /// <param name="e">The mouse event arguments.</param>
    protected override void OnMouseEnter(MouseEventArgs e)
    {
        base.OnMouseEnter(e);
        UpdateHoverSelection(e);
    }

    /// <summary>
    /// This method clears hover-driven selection when the pointer leaves the control.
    /// </summary>
    /// <param name="e">The mouse event arguments.</param>
    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        ClearHoverSelection();
    }

    /// <summary>
    /// This method updates hover selection on preview move and forwards drags.
    /// </summary>
    /// <param name="e">The mouse event arguments.</param>
    protected override void OnPreviewMouseMove(MouseEventArgs e)
    {
        base.OnPreviewMouseMove(e);
        UpdateHoverSelection(e);
        HandleMouseMove(e);
    }

    /// <summary>
    /// This method selects the pointer node, neighbors, and incident edges.
    /// </summary>
    /// <remarks>
    /// Applies hatched styling for the default template.
    /// </remarks>
    /// <param name="e">The mouse event arguments.</param>
    void UpdateHoverSelection(MouseEventArgs e)
    {
        if (Nodes is null || Edges is null)
        {
            return;
        }

        if (_dragNode is not null || _graphDragActive)
        {
            return;
        }

        var center = TryGetNodeFromOriginalSource(e.OriginalSource as DependencyObject, out var node) ? node : null;
        if (ReferenceEquals(center, _lastHoverCenterNode))
        {
            return;
        }

        ClearAllGraphSelection();
        _lastHoverCenterNode = center;
        if (center is null)
        {
            RefreshEdges();
            return;
        }

        var highlightIds = new HashSet<string>(StringComparer.Ordinal) { center.Id };
        foreach (var obj in Edges)
        {
            if (obj is not CgDiagramEdge edge)
            {
                continue;
            }

            if (edge.FromId == center.Id)
            {
                highlightIds.Add(edge.ToId);
            }

            if (edge.ToId == center.Id)
            {
                highlightIds.Add(edge.FromId);
            }
        }

        foreach (var obj in Nodes)
        {
            if (obj is CgDiagramNode n && highlightIds.Contains(n.Id))
            {
                n.IsSelected = true;
            }
        }

        foreach (var obj in Edges)
        {
            if (obj is not CgDiagramEdge edge)
            {
                continue;
            }

            edge.IsSelected = edge.FromId == center.Id || edge.ToId == center.Id;
        }

        RefreshEdges();
    }

    /// <summary>
    /// This method clears selection on all nodes and edges in the graph.
    /// </summary>
    void ClearAllGraphSelection()
    {
        if (Nodes is not null)
        {
            foreach (var obj in Nodes)
            {
                if (obj is CgDiagramNode n)
                {
                    n.IsSelected = false;
                }
            }
        }

        if (Edges is not null)
        {
            foreach (var obj in Edges)
            {
                if (obj is CgDiagramEdge edge)
                {
                    edge.IsSelected = false;
                }
            }
        }
    }

    /// <summary>
    /// This method clears hover-driven selection when the pointer exits the diagram.
    /// </summary>
    void ClearHoverSelection()
    {
        _lastHoverCenterNode = null;
        ClearAllGraphSelection();
        RefreshEdges();
    }

    /// <summary>
    /// This method hooks collections and properties and refreshes edge visuals.
    /// </summary>
    void RefreshSubscriptionsAndEdges()
    {
        if (!IsLoaded)
        {
            return;
        }

        HookCollections();
        ApplyDiagramNodeSizeDefaults();
        HookNodePositionChanges();
        RefreshEdges();
    }

    void ApplyDiagramNodeSizeDefaults()
    {
        if (Nodes is null)
        {
            return;
        }

        var s = NodeSize;
        if (s.Width <= 0 || s.Height <= 0)
        {
            return;
        }

        const double labelTopMargin = 8.0;
        const double labelBottomPad = 4.0;

        foreach (var obj in Nodes)
        {
            if (obj is not CgDiagramNode n || n.Size is not null)
            {
                continue;
            }

            n.Width = s.Width;
            var headRowHeight = n.Width;
            var labelHeight = MeasureNodeLabelHeight(n.Label, n.Width);
            n.Height = Math.Max(s.Height, headRowHeight + labelTopMargin + labelHeight + labelBottomPad);
        }
    }

    double MeasureNodeLabelHeight(string? label, double maxWidth)
    {
        if (string.IsNullOrWhiteSpace(label) || maxWidth <= 1.0)
        {
            return 0.0;
        }

        double pixelsPerDip = 1.0;
        try
        {
            pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
        }
        catch (InvalidOperationException)
        {
        }

        var family = FontFamily ?? SystemFonts.MessageFontFamily;
        var typeface = new Typeface(family, FontStyle, FontWeight, FontStretch);
        var numberSubstitution = new NumberSubstitution(
            NumberCultureSource.Text,
            CultureInfo.CurrentCulture,
            NumberSubstitutionMethod.AsCulture);
        var hinted = CgDiagramLabelWrapFormatting.InsertLineBreakHints(label);
        var formattedText = new FormattedText(
            hinted,
            CultureInfo.CurrentCulture,
            FlowDirection,
            typeface,
            FontSize,
            Brushes.Black,
            numberSubstitution,
            TextFormattingMode.Ideal,
            pixelsPerDip);
        formattedText.MaxTextWidth = maxWidth;
        formattedText.Trimming = TextTrimming.None;
        var lineSlack = FontSize * 0.35;
        return Math.Ceiling(formattedText.Height + lineSlack);
    }

    /// <summary>
    /// This method subscribes to collection changes on nodes and edges.
    /// </summary>
    void HookCollections()
    {
        if (_nodesNotify is not null)
        {
            _nodesNotify.CollectionChanged -= OnNodesCollectionChanged;
            _nodesNotify = null;
        }
        if (_edgesNotify is not null)
        {
            _edgesNotify.CollectionChanged -= OnEdgesCollectionChanged;
            _edgesNotify = null;
        }

        _nodesNotify = Nodes as INotifyCollectionChanged;
        if (_nodesNotify is not null)
        {
            _nodesNotify.CollectionChanged += OnNodesCollectionChanged;
        }

        _edgesNotify = Edges as INotifyCollectionChanged;
        if (_edgesNotify is not null)
        {
            _edgesNotify.CollectionChanged += OnEdgesCollectionChanged;
        }
    }

    /// <summary>
    /// This method refreshes node hooks and redraws connectors when nodes change.
    /// </summary>
    /// <param name="sender">The collection that raised the event.</param>
    /// <param name="e">Details about the collection change.</param>
    void OnNodesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _lastHoverCenterNode = null;
        ApplyDiagramNodeSizeDefaults();
        HookNodePositionChanges();
        RefreshEdges();
    }

    /// <summary>
    /// This method redraws connectors when the edges collection changes.
    /// </summary>
    /// <param name="sender">The collection that raised the event.</param>
    /// <param name="e">Details about the collection change.</param>
    void OnEdgesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        _lastHoverCenterNode = null;
        RefreshEdges();
    }

    /// <summary>
    /// This method subscribes to node property changes that affect connector geometry.
    /// </summary>
    void HookNodePositionChanges()
    {
        foreach (var kvp in _nodeHandlers)
        {
            kvp.Key.PropertyChanged -= kvp.Value;
        }
        _nodeHandlers.Clear();

        if (Nodes is null)
        {
            return;
        }

        foreach (var obj in Nodes)
        {
            if (obj is CgDiagramNode n)
            {
                PropertyChangedEventHandler h = (_, args) =>
                {
                    if (args.PropertyName is null || args.PropertyName == nameof(CgDiagramNode.Position))
                    {
                        if (_suppressPositionDrivenEdgeRefresh && args.PropertyName == nameof(CgDiagramNode.Position))
                        {
                            return;
                        }

                        RefreshEdges();
                    }
                };
                n.PropertyChanged += h;
                _nodeHandlers[n] = h;
            }
        }
    }

    /// <summary>
    /// This method rebuilds connectors and scroll extent from the model.
    /// </summary>
    void RefreshEdges()
    {
        if (_edgesCanvas is null)
        {
            return;
        }

        _edgesCanvas.Children.Clear();
        _edgesCanvas.IsHitTestVisible = false;

        var nodes = Nodes;
        var edges = Edges;
        if (nodes is null || edges is null)
        {
            ContentWidth = 0;
            ContentHeight = 0;
            return;
        }

        var map = new Dictionary<string, (Point Center, double Radius)>(StringComparer.Ordinal);
        var anyNodes = false;
        var minX = double.PositiveInfinity;
        var minY = double.PositiveInfinity;
        var maxX = double.NegativeInfinity;
        var maxY = double.NegativeInfinity;

        foreach (var obj in nodes)
        {
            if (obj is CgDiagramNode n)
            {
                anyNodes = true;
                minX = Math.Min(minX, n.Position.X);
                minY = Math.Min(minY, n.Position.Y);
                var size = n.Size ?? new Size(n.Width, n.Height);
                maxX = Math.Max(maxX, n.Position.X + size.Width);
                maxY = Math.Max(maxY, n.Position.Y + size.Height);

                map[n.Id] = (CgForceDirectedNodeMetrics.GetCircleCenter(n), CgForceDirectedNodeMetrics.GetCircleRadius(n));
            }
        }

        if (anyNodes)
        {
            var pad = ContentPadding;
            // Scroll extent must cover absolute node coordinates from (0,0) of PART_ContentGrid.
            // Using only (maxX - minX) underestimates width when minX > 0 (hierarchical layouts), so the
            // ScrollViewer viewport matches the clipped area and scrollbars appear to do nothing.
            var leftBound = Math.Min(0, minX);
            var topBound = Math.Min(0, minY);
            var w = maxX - leftBound + pad.Left + pad.Right;
            var h = maxY - topBound + pad.Top + pad.Bottom;
            ContentWidth = Math.Max(0, w);
            ContentHeight = Math.Max(0, h);
        }
        else
        {
            ContentWidth = 0;
            ContentHeight = 0;
        }

        foreach (var obj in edges)
        {
            if (obj is not CgDiagramEdge e)
            {
                continue;
            }

            if (!map.TryGetValue(e.FromId, out var from) || !map.TryGetValue(e.ToId, out var to))
            {
                continue;
            }

            if (!TryClipConnectorToCircles(from.Center, from.Radius, to.Center, to.Radius, out var start, out var end))
            {
                continue;
            }

            var strokeBrush = e.IsSelected ? EdgeSelectedStroke : EdgeStroke;

            const double arrowLength = 10.0;
            const double arrowHalfWidth = 4.5;
            var dx = end.X - start.X;
            var dy = end.Y - start.Y;
            var segLen = Math.Sqrt(dx * dx + dy * dy);
            if (segLen < 0.5)
            {
                continue;
            }

            var vx = dx / segLen;
            var vy = dy / segLen;
            var lineEnd = new Point(end.X - vx * arrowLength, end.Y - vy * arrowLength);
            if ((lineEnd.X - start.X) * vx + (lineEnd.Y - start.Y) * vy < 0)
            {
                lineEnd = end;
            }

            var visualLine = new Line
            {
                X1 = start.X,
                Y1 = start.Y,
                X2 = lineEnd.X,
                Y2 = lineEnd.Y,
                Stroke = strokeBrush,
                StrokeThickness = e.IsSelected ? EdgeThickness + EdgeSelectionThicknessBoost : EdgeThickness,
                SnapsToDevicePixels = true
            };

            var arrow = CreateArrowHeadPath(end, new Vector(vx, vy), arrowLength, arrowHalfWidth, strokeBrush);

            _edgesCanvas.Children.Add(visualLine);
            if (arrow is not null)
            {
                _edgesCanvas.Children.Add(arrow);
            }
        }
    }

    /// <summary>
    /// This method clips a segment between circle centers to the two circle outlines.
    /// </summary>
    static bool TryClipConnectorToCircles(Point c0, double r0, Point c1, double r1, out Point start, out Point end)
    {
        var dx = c1.X - c0.X;
        var dy = c1.Y - c0.Y;
        var len = Math.Sqrt(dx * dx + dy * dy);
        if (len < 1e-9)
        {
            start = c0;
            end = c1;
            return false;
        }

        var ux = dx / len;
        var uy = dy / len;
        start = new Point(c0.X + ux * r0, c0.Y + uy * r0);
        end = new Point(c1.X - ux * r1, c1.Y - uy * r1);
        var clippedLen = Math.Sqrt((end.X - start.X) * (end.X - start.X) + (end.Y - start.Y) * (end.Y - start.Y));
        return clippedLen >= 1.0;
    }

    /// <summary>
    /// This method builds a filled arrowhead path at a connector tip.
    /// </summary>
    static Path? CreateArrowHeadPath(Point tip, Vector direction, double length, double halfWidth, Brush fill)
    {
        if (direction.LengthSquared < 1e-12)
        {
            return null;
        }

        direction.Normalize();
        var baseCenter = new Point(tip.X - direction.X * length, tip.Y - direction.Y * length);
        var perp = new Vector(-direction.Y, direction.X);
        var p1 = baseCenter + perp * halfWidth;
        var p2 = baseCenter - perp * halfWidth;

        var fig = new PathFigure { StartPoint = tip, IsClosed = true };
        fig.Segments.Add(new LineSegment(p1, true));
        fig.Segments.Add(new LineSegment(p2, true));

        var geo = new PathGeometry();
        geo.Figures.Add(fig);

        return new Path
        {
            Data = geo,
            Fill = fill,
            SnapsToDevicePixels = true
        };
    }

    /// <summary>
    /// This method unsubscribes listeners when the control unloads.
    /// </summary>
    void UnsubscribeAll()
    {
        if (_nodesNotify is not null)
        {
            _nodesNotify.CollectionChanged -= OnNodesCollectionChanged;
            _nodesNotify = null;
        }
        if (_edgesNotify is not null)
        {
            _edgesNotify.CollectionChanged -= OnEdgesCollectionChanged;
            _edgesNotify = null;
        }
        foreach (var kvp in _nodeHandlers)
        {
            kvp.Key.PropertyChanged -= kvp.Value;
        }
        _nodeHandlers.Clear();
    }
}

