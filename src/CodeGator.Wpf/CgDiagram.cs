using CodeGator.Wpf.Args;
using System.Collections;
using System.Linq;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;
using CodeGator.Wpf.Layouts;

namespace CodeGator.Wpf;

/// <summary>
/// This class is a WPF control that provides an interactive diagram surface for nodes and edges.
/// </summary>
/// <remarks>
/// The control supports zoom, pan, selection, and routed events for pointer interaction with nodes and edges.
/// </remarks>
[TemplatePart(Name = PartEdgesCanvas, Type = typeof(Canvas))]
[TemplatePart(Name = PartNodesItems, Type = typeof(ItemsControl))]
[TemplatePart(Name = PartOverlayCanvas, Type = typeof(Canvas))]
[TemplatePart(Name = PartScrollViewer, Type = typeof(ScrollViewer))]
[TemplatePart(Name = PartContentGrid, Type = typeof(Grid))]
public sealed class CgDiagram : Control
{
    const string PartEdgesCanvas = "PART_EdgesCanvas";
    const string PartNodesItems = "PART_NodesItems";
    const string PartOverlayCanvas = "PART_OverlayCanvas";
    const string PartScrollViewer = "PART_ScrollViewer";
    const string PartContentGrid = "PART_ContentGrid";

    Canvas? _edgesCanvas;
    Canvas? _overlayCanvas;
    ScrollViewer? _scrollViewer;
    Grid? _contentGrid;
    Rectangle? _marqueeVisual;

    INotifyCollectionChanged? _nodesNotify;
    INotifyCollectionChanged? _edgesNotify;

    readonly Dictionary<CgDiagramNode, PropertyChangedEventHandler> _nodeHandlers = new();

    CgDiagramNode? _dragNode;
    CgDiagramNode? _mouseDownNode;
    Point _dragStartMouse;
    Point _dragStartNodePos;
    Point _mouseDownMouse;
    bool _movedDuringMouseDown;

    bool _marqueeSelecting;
    Point _marqueeStartContent;
    Point _marqueeCurrentContent;

    bool _panning;
    Point _panStartMouse;
    double _panStartX;
    double _panStartY;

    const double ClickMoveThreshold = 3.0;

    /// <summary>
    /// This static constructor registers the default style key metadata used when WPF resolves the control template.
    /// </summary>
    static CgDiagram()
    {
        DefaultStyleKeyProperty.OverrideMetadata(typeof(CgDiagram), new FrameworkPropertyMetadata(typeof(CgDiagram)));
    }

    /// <summary>
    /// This constructor creates the diagram control, enables focus, and subscribes to loaded and unloaded events.
    /// </summary>
    public CgDiagram()
    {
        Focusable = true;
        Loaded += (_, _) => RefreshSubscriptionsAndEdges();
        Unloaded += (_, _) => UnsubscribeAll();
    }

    /// <summary>
    /// This event exposes the registered bubbling identifier for <see cref="NodeClick"/>.
    /// </summary>
    public static readonly RoutedEvent NodeClickEvent =
        EventManager.RegisterRoutedEvent(nameof(NodeClick), RoutingStrategy.Bubble, typeof(EventHandler<CgDiagramNodeEventArgs>), typeof(CgDiagram));

    /// <summary>
    /// This event bubbles after the user completes a primary-button press on a node without dragging.
    /// </summary>
    /// <remarks>
    /// Handlers receive <see cref="CgDiagramNodeEventArgs"/> with the node and pointer position.
    /// </remarks>
    public event EventHandler<CgDiagramNodeEventArgs> NodeClick
    {
        add => AddHandler(NodeClickEvent, value);
        remove => RemoveHandler(NodeClickEvent, value);
    }

    /// <summary>
    /// This event exposes the registered bubbling identifier for <see cref="NodeRightClick"/>.
    /// </summary>
    public static readonly RoutedEvent NodeRightClickEvent =
        EventManager.RegisterRoutedEvent(nameof(NodeRightClick), RoutingStrategy.Bubble, typeof(EventHandler<CgDiagramNodeEventArgs>), typeof(CgDiagram));

    /// <summary>
    /// This event bubbles when the user secondary-clicks a diagram node while that gesture is not starting a pan.
    /// </summary>
    /// <remarks>
    /// Handlers receive <see cref="CgDiagramNodeEventArgs"/> with the node and pointer position.
    /// </remarks>
    public event EventHandler<CgDiagramNodeEventArgs> NodeRightClick
    {
        add => AddHandler(NodeRightClickEvent, value);
        remove => RemoveHandler(NodeRightClickEvent, value);
    }

    /// <summary>
    /// This event exposes the registered bubbling identifier for <see cref="EdgeClick"/>.
    /// </summary>
    public static readonly RoutedEvent EdgeClickEvent =
        EventManager.RegisterRoutedEvent(nameof(EdgeClick), RoutingStrategy.Bubble, typeof(EventHandler<CgDiagramEdgeEventArgs>), typeof(CgDiagram));

    /// <summary>
    /// This event bubbles after the user primary-clicks a connector hit target.
    /// </summary>
    /// <remarks>
    /// Handlers receive <see cref="CgDiagramEdgeEventArgs"/> with the edge and pointer position.
    /// </remarks>
    public event EventHandler<CgDiagramEdgeEventArgs> EdgeClick
    {
        add => AddHandler(EdgeClickEvent, value);
        remove => RemoveHandler(EdgeClickEvent, value);
    }

    /// <summary>
    /// This event exposes the registered bubbling identifier for <see cref="EdgeRightClick"/>.
    /// </summary>
    public static readonly RoutedEvent EdgeRightClickEvent =
        EventManager.RegisterRoutedEvent(nameof(EdgeRightClick), RoutingStrategy.Bubble, typeof(EventHandler<CgDiagramEdgeEventArgs>), typeof(CgDiagram));

    /// <summary>
    /// This event bubbles when the user secondary-clicks a connector hit target.
    /// </summary>
    /// <remarks>
    /// Handlers receive <see cref="CgDiagramEdgeEventArgs"/> with the edge and pointer position.
    /// </remarks>
    public event EventHandler<CgDiagramEdgeEventArgs> EdgeRightClick
    {
        add => AddHandler(EdgeRightClickEvent, value);
        remove => RemoveHandler(EdgeRightClickEvent, value);
    }

    /// <summary>
    /// This method locates named template parts and triggers a full subscription and edge refresh pass.
    /// </summary>
    public override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        _edgesCanvas = GetTemplateChild(PartEdgesCanvas) as Canvas;
        _overlayCanvas = GetTemplateChild(PartOverlayCanvas) as Canvas;
        _scrollViewer = GetTemplateChild(PartScrollViewer) as ScrollViewer;
        _contentGrid = GetTemplateChild(PartContentGrid) as Grid;
        RefreshSubscriptionsAndEdges();
    }

    /// <summary>
    /// This method opens the print dialog and renders the entire diagram content to the selected printer.
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
            // Reset view so we print the entire content deterministically.
            Zoom = 1.0;
            PanX = 0.0;
            PanY = 0.0;
            _scrollViewer?.ScrollToHorizontalOffset(0.0);
            _scrollViewer?.ScrollToVerticalOffset(0.0);

            // Ensure layout is up to date with the temporary view.
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
    /// This property gets or sets the layout kind used when callers invoke <see cref="ApplyLayout(CgDiagramLayoutKind?, ICgDiagramLayout?)"/> without overriding it.
    /// </summary>
    public CgDiagramLayoutKind Layout
    {
        get => (CgDiagramLayoutKind)GetValue(LayoutProperty);
        set => SetValue(LayoutProperty, value);
    }

    /// <summary>
    /// This property identifies the <see cref="Layout"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty LayoutProperty =
        DependencyProperty.Register(
            nameof(Layout),
            typeof(CgDiagramLayoutKind),
            typeof(CgDiagram),
            new PropertyMetadata(CgDiagramLayoutKind.HierarchicalTopDown));

    /// <summary>
    /// This method arranges nodes using a built-in or custom layout and updates connector geometry afterward.
    /// </summary>
    public void ApplyLayout(CgDiagramLayoutKind? kind = null, ICgDiagramLayout? layout = null)
    {
        var nodes = (Nodes as IEnumerable)?.OfType<CgDiagramNode>().ToList() ?? new List<CgDiagramNode>();
        var edges = (Edges as IEnumerable)?.OfType<CgDiagramEdge>().ToList() ?? new List<CgDiagramEdge>();
        if (nodes.Count == 0)
        {
            return;
        }

        var k = kind ?? Layout;
        var algo = layout ?? CgDiagramBuiltinLayouts.For(k);
        var options = new CgDiagramLayoutOptions(NodeSize);
        var positions = algo.Compute(nodes, edges, options);
        foreach (var n in nodes)
        {
            if (positions.TryGetValue(n.Id, out var p))
            {
                n.Position = p;
            }
        }

        RefreshEdges();
    }

    /// <summary>
    /// This property exposes the bound collection of <see cref="CgDiagramNode"/> instances rendered on the surface.
    /// </summary>
    public IEnumerable? Nodes
    {
        get => (IEnumerable?)GetValue(NodesProperty);
        set => SetValue(NodesProperty, value);
    }

    /// <summary>
    /// This property identifies the <see cref="Nodes"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty NodesProperty =
        DependencyProperty.Register(
            nameof(Nodes),
            typeof(IEnumerable),
            typeof(CgDiagram),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, (d, _) => ((CgDiagram)d).OnNodesChanged()));

    /// <summary>
    /// This method reacts to the nodes collection dependency property changing by refreshing hooks and edges.
    /// </summary>
    void OnNodesChanged()
    {
        RefreshSubscriptionsAndEdges();
    }

    /// <summary>
    /// This property exposes the bound collection of <see cref="CgDiagramEdge"/> instances connecting nodes.
    /// </summary>
    public IEnumerable? Edges
    {
        get => (IEnumerable?)GetValue(EdgesProperty);
        set => SetValue(EdgesProperty, value);
    }

    /// <summary>
    /// This property identifies the <see cref="Edges"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty EdgesProperty =
        DependencyProperty.Register(
            nameof(Edges),
            typeof(IEnumerable),
            typeof(CgDiagram),
            new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender, (d, _) => ((CgDiagram)d).OnEdgesChanged()));

    /// <summary>
    /// This method reacts to the edges collection dependency property changing by refreshing hooks and edges.
    /// </summary>
    void OnEdgesChanged()
    {
        RefreshSubscriptionsAndEdges();
    }

    /// <summary>
    /// This property supplies the data template used for each item in the nodes items control.
    /// </summary>
    public DataTemplate? NodeTemplate
    {
        get => (DataTemplate?)GetValue(NodeTemplateProperty);
        set => SetValue(NodeTemplateProperty, value);
    }

    /// <summary>
    /// This property identifies the <see cref="NodeTemplate"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty NodeTemplateProperty =
        DependencyProperty.Register(nameof(NodeTemplate), typeof(DataTemplate), typeof(CgDiagram), new PropertyMetadata(null));

    /// <summary>
    /// This property toggles whether a background grid renders behind diagram content.
    /// </summary>
    public bool ShowGrid
    {
        get => (bool)GetValue(ShowGridProperty);
        set => SetValue(ShowGridProperty, value);
    }

    /// <summary>
    /// This property identifies the <see cref="ShowGrid"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ShowGridProperty =
        DependencyProperty.Register(nameof(ShowGrid), typeof(bool), typeof(CgDiagram), new PropertyMetadata(false));

    /// <summary>
    /// This property defines the stroke brush applied to the background grid lines.
    /// </summary>
    public Brush? GridBrush
    {
        get => (Brush?)GetValue(GridBrushProperty);
        set => SetValue(GridBrushProperty, value);
    }

    /// <summary>
    /// This property identifies the <see cref="GridBrush"/> dependency property.
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
    /// This property identifies the <see cref="GridOpacity"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty GridOpacityProperty =
        DependencyProperty.Register(nameof(GridOpacity), typeof(double), typeof(CgDiagram), new PropertyMetadata(0.35));

    /// <summary>
    /// This property gets or sets the diagram zoom factor applied through layout transforms.
    /// </summary>
    public double Zoom
    {
        get => (double)GetValue(ZoomProperty);
        set => SetValue(ZoomProperty, value);
    }

    /// <summary>
    /// This property identifies the <see cref="Zoom"/> dependency property.
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
    /// This property identifies the <see cref="MinZoom"/> dependency property.
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
    /// This property identifies the <see cref="MaxZoom"/> dependency property.
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
    /// This property identifies the <see cref="PanX"/> dependency property.
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
    /// This property identifies the <see cref="PanY"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty PanYProperty =
        DependencyProperty.Register(nameof(PanY), typeof(double), typeof(CgDiagram), new PropertyMetadata(0.0));

    /// <summary>
    /// This property defines the brush used to stroke connector lines in their default state.
    /// </summary>
    public Brush EdgeStroke
    {
        get => (Brush)GetValue(EdgeStrokeProperty);
        set => SetValue(EdgeStrokeProperty, value);
    }

    /// <summary>
    /// This property identifies the <see cref="EdgeStroke"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty EdgeStrokeProperty =
        DependencyProperty.Register(nameof(EdgeStroke), typeof(Brush), typeof(CgDiagram), new PropertyMetadata(new SolidColorBrush(Color.FromArgb(0x99, 0xFF, 0xFF, 0xFF)), (d, _) => ((CgDiagram)d).RefreshEdges()));

    /// <summary>
    /// This property controls the pixel thickness of visible connector strokes.
    /// </summary>
    public double EdgeThickness
    {
        get => (double)GetValue(EdgeThicknessProperty);
        set => SetValue(EdgeThicknessProperty, value);
    }

    /// <summary>
    /// This property identifies the <see cref="EdgeThickness"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty EdgeThicknessProperty =
        DependencyProperty.Register(nameof(EdgeThickness), typeof(double), typeof(CgDiagram), new PropertyMetadata(1.25, (d, _) => ((CgDiagram)d).RefreshEdges()));

    /// <summary>
    /// This property defines a node-local reference point available to templates for aligning adorners and overlays.
    /// </summary>
    public Point NodeAnchor
    {
        get => (Point)GetValue(NodeAnchorProperty);
        set => SetValue(NodeAnchorProperty, value);
    }

    /// <summary>
    /// This property identifies the <see cref="NodeAnchor"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty NodeAnchorProperty =
        DependencyProperty.Register(nameof(NodeAnchor), typeof(Point), typeof(CgDiagram), new PropertyMetadata(new Point(80, 22), (d, _) => ((CgDiagram)d).RefreshEdges()));

    /// <summary>
    /// This property supplies the default width and height assumptions used for connectors and layout padding when nodes omit explicit measurements.
    /// </summary>
    public Size NodeSize
    {
        get => (Size)GetValue(NodeSizeProperty);
        set => SetValue(NodeSizeProperty, value);
    }

    /// <summary>
    /// This property identifies the <see cref="NodeSize"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty NodeSizeProperty =
        DependencyProperty.Register(nameof(NodeSize), typeof(Size), typeof(CgDiagram), new PropertyMetadata(new Size(160, 56), (d, _) => ((CgDiagram)d).RefreshEdges()));

    /// <summary>
    /// This property pads the measured diagram bounds when computing scrollable <see cref="ContentWidth"/> and <see cref="ContentHeight"/>.
    /// </summary>
    public Thickness ContentPadding
    {
        get => (Thickness)GetValue(ContentPaddingProperty);
        set => SetValue(ContentPaddingProperty, value);
    }

    /// <summary>
    /// This property identifies the <see cref="ContentPadding"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ContentPaddingProperty =
        DependencyProperty.Register(nameof(ContentPadding), typeof(Thickness), typeof(CgDiagram), new PropertyMetadata(new Thickness(40), (d, _) => ((CgDiagram)d).RefreshEdges()));

    /// <summary>
    /// This property exposes the computed scrollable width of diagram content including padding.
    /// </summary>
    public double ContentWidth
    {
        get => (double)GetValue(ContentWidthProperty);
        private set => SetValue(ContentWidthPropertyKey, value);
    }

    /// <summary>
    /// This property holds the key registered for the read-only <see cref="ContentWidth"/> dependency property.
    /// </summary>
    static readonly DependencyPropertyKey ContentWidthPropertyKey =
        DependencyProperty.RegisterReadOnly(nameof(ContentWidth), typeof(double), typeof(CgDiagram), new PropertyMetadata(0.0));

    /// <summary>
    /// This property identifies the public <see cref="ContentWidth"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ContentWidthProperty = ContentWidthPropertyKey.DependencyProperty;

    /// <summary>
    /// This property exposes the computed scrollable height of diagram content including padding.
    /// </summary>
    public double ContentHeight
    {
        get => (double)GetValue(ContentHeightProperty);
        private set => SetValue(ContentHeightPropertyKey, value);
    }

    /// <summary>
    /// This property holds the key registered for the read-only <see cref="ContentHeight"/> dependency property.
    /// </summary>
    static readonly DependencyPropertyKey ContentHeightPropertyKey =
        DependencyProperty.RegisterReadOnly(nameof(ContentHeight), typeof(double), typeof(CgDiagram), new PropertyMetadata(0.0));

    /// <summary>
    /// This property identifies the public <see cref="ContentHeight"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty ContentHeightProperty = ContentHeightPropertyKey.DependencyProperty;

    /// <summary>
    /// This method clamps zoom to configured bounds and rebuilds edge visuals when zoom-related properties change.
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
    /// This method adjusts zoom from a preview wheel event before the scroll viewer consumes it.
    /// </summary>
    protected override void OnPreviewMouseWheel(MouseWheelEventArgs e)
    {
        // ScrollViewer handles wheel by default; use Preview to keep zoom working.
        base.OnPreviewMouseWheel(e);
        var delta = e.Delta > 0 ? 1.10 : 1 / 1.10;
        Zoom = Math.Clamp(Zoom * delta, MinZoom, MaxZoom);
        e.Handled = true;
    }

    /// <summary>
    /// This method adjusts zoom when the mouse wheel bubbles without being handled earlier.
    /// </summary>
    protected override void OnMouseWheel(MouseWheelEventArgs e)
    {
        base.OnMouseWheel(e);
        var delta = e.Delta > 0 ? 1.10 : 1 / 1.10;
        Zoom = Math.Clamp(Zoom * delta, MinZoom, MaxZoom);
        e.Handled = true;
    }

    /// <summary>
    /// This method forwards early mouse down input to shared hit testing before child controls mark it handled.
    /// </summary>
    protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
    {
        // ScrollViewer can mark mouse input as handled; grab it early.
        base.OnPreviewMouseDown(e);
        HandleMouseDown(e);
    }

    /// <summary>
    /// This method forwards mouse down input after preview handling when interaction should still reach the diagram.
    /// </summary>
    protected override void OnMouseDown(MouseButtonEventArgs e)
    {
        base.OnMouseDown(e);
        HandleMouseDown(e);
    }

    /// <summary>
    /// This method interprets mouse down actions to begin panning, dragging, marquee selection, or context clicks.
    /// </summary>
    void HandleMouseDown(MouseButtonEventArgs e)
    {
        if (e.Handled)
        {
            return;
        }

        Focus();

        if (e.ChangedButton == MouseButton.Right)
        {
            if (TryGetNodeFromOriginalSource(e.OriginalSource as DependencyObject, out var node))
            {
                SelectNode(node);
                RaiseEvent(new CgDiagramNodeEventArgs(NodeRightClickEvent, node, e.GetPosition(this)));
                e.Handled = true;
                return;
            }

            _panning = true;
            _panStartMouse = e.GetPosition(this);
            _panStartX = PanX;
            _panStartY = PanY;
            CaptureMouse();
            e.Handled = true;
            return;
        }

        if (e.ChangedButton == MouseButton.Left)
        {
            if (TryGetNodeFromOriginalSource(e.OriginalSource as DependencyObject, out var node))
            {
                _mouseDownNode = node;
                _mouseDownMouse = e.GetPosition(this);
                _movedDuringMouseDown = false;

                _dragNode = node;
                _dragStartMouse = e.GetPosition(this);
                _dragStartNodePos = node.Position;
                CaptureMouse();
                e.Handled = true;
            }
            else
            {
                // Start marquee selection when clicking empty space.
                _mouseDownNode = null;
                _movedDuringMouseDown = false;
                _marqueeSelecting = true;
                var c = ToContentPoint(e.GetPosition(this));
                _marqueeStartContent = c;
                _marqueeCurrentContent = c;
                EnsureMarqueeVisual();
                UpdateMarqueeVisual();
                CaptureMouse();
                e.Handled = true;
            }
        }
    }

    /// <summary>
    /// This method routes move notifications to the shared drag, pan, and marquee update logic.
    /// </summary>
    protected override void OnMouseMove(MouseEventArgs e)
    {
        base.OnMouseMove(e);
        HandleMouseMove(e);
    }

    /// <summary>
    /// This method updates marquee rectangles, detects click versus drag, pans the surface, or moves the active node.
    /// </summary>
    void HandleMouseMove(MouseEventArgs e)
    {
        if (e.Handled)
        {
            return;
        }

        var p = e.GetPosition(this);

        if (_marqueeSelecting && IsMouseCaptured)
        {
            _marqueeCurrentContent = ToContentPoint(p);
            UpdateMarqueeVisual();
            e.Handled = true;
            return;
        }

        if (_mouseDownNode is not null && !_movedDuringMouseDown)
        {
            if (Math.Abs(p.X - _mouseDownMouse.X) > ClickMoveThreshold || Math.Abs(p.Y - _mouseDownMouse.Y) > ClickMoveThreshold)
            {
                _movedDuringMouseDown = true;
            }
        }

        if (_panning && IsMouseCaptured)
        {
            PanX = _panStartX + (p.X - _panStartMouse.X);
            PanY = _panStartY + (p.Y - _panStartMouse.Y);
            e.Handled = true;
            return;
        }

        if (_dragNode is not null && IsMouseCaptured)
        {
            var dx = (p.X - _dragStartMouse.X) / Math.Max(0.0001, Zoom);
            var dy = (p.Y - _dragStartMouse.Y) / Math.Max(0.0001, Zoom);
            _dragNode.Position = new Point(_dragStartNodePos.X + dx, _dragStartNodePos.Y + dy);
            e.Handled = true;
        }
    }

    /// <summary>
    /// This method completes pointer operations initiated during preview mouse down handling.
    /// </summary>
    protected override void OnPreviewMouseUp(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseUp(e);
        HandleMouseUp(e);
    }

    /// <summary>
    /// This method completes pointer operations when mouse up arrives without preview interception.
    /// </summary>
    protected override void OnMouseUp(MouseButtonEventArgs e)
    {
        base.OnMouseUp(e);
        HandleMouseUp(e);
    }

    /// <summary>
    /// This method finalizes marquee selection, panning, or node clicks after mouse capture ends.
    /// </summary>
    void HandleMouseUp(MouseButtonEventArgs e)
    {
        if (e.Handled)
        {
            return;
        }

        if (e.ChangedButton == MouseButton.Left && _marqueeSelecting)
        {
            _marqueeSelecting = false;
            if (IsMouseCaptured) ReleaseMouseCapture();

            HideMarqueeVisual();
            ApplyMarqueeSelection();
            e.Handled = true;
            return;
        }

        if (e.ChangedButton == MouseButton.Right && _panning)
        {
            _panning = false;
            if (IsMouseCaptured) ReleaseMouseCapture();
            e.Handled = true;
            return;
        }

        if (e.ChangedButton == MouseButton.Left && _dragNode is not null)
        {
            var upNode = _mouseDownNode;
            var treatAsClick = upNode is not null && !_movedDuringMouseDown;

            _dragNode = null;
            _mouseDownNode = null;
            if (IsMouseCaptured) ReleaseMouseCapture();

            if (treatAsClick && upNode is not null)
            {
                SelectNode(upNode);
                RaiseEvent(new CgDiagramNodeEventArgs(NodeClickEvent, upNode, e.GetPosition(this)));
            }

            e.Handled = true;
        }
    }

    /// <summary>
    /// This method walks the visual tree from a hit target to find a data context bound to <see cref="CgDiagramNode"/>.
    /// </summary>
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
    /// This method updates hover highlighting when the cursor enters the diagram's bounds.
    /// </summary>
    protected override void OnMouseEnter(MouseEventArgs e)
    {
        base.OnMouseEnter(e);
        UpdateHoverNode(e);
    }

    /// <summary>
    /// This method clears hover state when the cursor leaves the diagram's bounds.
    /// </summary>
    protected override void OnMouseLeave(MouseEventArgs e)
    {
        base.OnMouseLeave(e);
        ClearHoverNodes();
    }

    /// <summary>
    /// This method refreshes hover targets and continues drag or marquee handling during preview move events.
    /// </summary>
    protected override void OnPreviewMouseMove(MouseEventArgs e)
    {
        base.OnPreviewMouseMove(e);
        UpdateHoverNode(e);
        HandleMouseMove(e);
    }

    /// <summary>
    /// This method sets <see cref="CgDiagramNode.IsHovered"/> on nodes according to the element currently under the pointer.
    /// </summary>
    void UpdateHoverNode(MouseEventArgs e)
    {
        if (Nodes is null) return;
        var overNode = TryGetNodeFromOriginalSource(e.OriginalSource as DependencyObject, out var node) ? node : null;
        foreach (var obj in Nodes)
        {
            if (obj is CgDiagramNode n)
            {
                n.IsHovered = overNode is not null && ReferenceEquals(n, overNode);
            }
        }
    }

    /// <summary>
    /// This method clears hover flags on every node when the pointer is no longer over a node visual.
    /// </summary>
    void ClearHoverNodes()
    {
        if (Nodes is null) return;
        foreach (var obj in Nodes)
        {
            if (obj is CgDiagramNode n)
            {
                n.IsHovered = false;
            }
        }
    }

    /// <summary>
    /// This method maps a point from control coordinates into the scrollable content grid's coordinate space.
    /// </summary>
    Point ToContentPoint(Point controlPoint)
    {
        // Use WPF's visual transforms to map from the control into content coordinates.
        // This is robust across ScrollViewer offsets, LayoutTransform zoom, and RenderTransform pan.
        if (_contentGrid is null)
        {
            return controlPoint;
        }

        try
        {
            var toContent = TransformToVisual(_contentGrid);
            return toContent.Transform(controlPoint);
        }
        catch
        {
            return controlPoint;
        }
    }

    /// <summary>
    /// This method creates the marquee rectangle on the overlay canvas when it is missing.
    /// </summary>
    void EnsureMarqueeVisual()
    {
        if (_overlayCanvas is null)
        {
            return;
        }

        if (_marqueeVisual is not null)
        {
            return;
        }

        _marqueeVisual = new Rectangle
        {
            Stroke = Brushes.DeepSkyBlue,
            StrokeThickness = 1.0,
            Fill = new SolidColorBrush(Color.FromArgb(0x33, 0x00, 0xBF, 0xFF)),
            Visibility = Visibility.Collapsed,
            SnapsToDevicePixels = true
        };
        _overlayCanvas.Children.Add(_marqueeVisual);
    }

    /// <summary>
    /// This method positions and sizes the marquee rectangle to match the current drag selection in overlay space.
    /// </summary>
    void UpdateMarqueeVisual()
    {
        if (_marqueeVisual is null || _overlayCanvas is null)
        {
            return;
        }

        var x0 = Math.Min(_marqueeStartContent.X, _marqueeCurrentContent.X);
        var y0 = Math.Min(_marqueeStartContent.Y, _marqueeCurrentContent.Y);
        var x1 = Math.Max(_marqueeStartContent.X, _marqueeCurrentContent.X);
        var y1 = Math.Max(_marqueeStartContent.Y, _marqueeCurrentContent.Y);

        // Convert content rect back to control-space for drawing.
        if (_contentGrid is null)
        {
            return;
        }

        Point p0, p1;
        try
        {
            var toControl = _contentGrid.TransformToVisual(this);
            p0 = toControl.Transform(new Point(x0, y0));
            p1 = toControl.Transform(new Point(x1, y1));
        }
        catch
        {
            return;
        }

        var left = Math.Min(p0.X, p1.X);
        var top = Math.Min(p0.Y, p1.Y);
        var width = Math.Abs(p1.X - p0.X);
        var height = Math.Abs(p1.Y - p0.Y);

        Canvas.SetLeft(_marqueeVisual, left);
        Canvas.SetTop(_marqueeVisual, top);
        _marqueeVisual.Width = Math.Max(0.0, width);
        _marqueeVisual.Height = Math.Max(0.0, height);
        _marqueeVisual.Visibility = Visibility.Visible;
    }

    /// <summary>
    /// This method hides the marquee overlay after a selection gesture completes.
    /// </summary>
    void HideMarqueeVisual()
    {
        if (_marqueeVisual is not null)
        {
            _marqueeVisual.Visibility = Visibility.Collapsed;
        }
    }

    /// <summary>
    /// This method selects nodes intersecting the marquee rectangle and clears edge selection.
    /// </summary>
    /// <remarks>
    /// A very small marquee clears all node and edge selection instead of performing a box hit test.
    /// </remarks>
    void ApplyMarqueeSelection()
    {
        if (Nodes is null)
        {
            return;
        }

        var x0 = Math.Min(_marqueeStartContent.X, _marqueeCurrentContent.X);
        var y0 = Math.Min(_marqueeStartContent.Y, _marqueeCurrentContent.Y);
        var x1 = Math.Max(_marqueeStartContent.X, _marqueeCurrentContent.X);
        var y1 = Math.Max(_marqueeStartContent.Y, _marqueeCurrentContent.Y);

        var selW = x1 - x0;
        var selH = y1 - y0;
        if (selW < 2 && selH < 2)
        {
            // Treat tiny drag as "clear selection".
            foreach (var obj in Nodes)
            {
                if (obj is CgDiagramNode n) n.IsSelected = false;
            }
            if (Edges is not null)
            {
                foreach (var obj in Edges)
                {
                    if (obj is CgDiagramEdge e) e.IsSelected = false;
                }
            }
            RefreshEdges();
            return;
        }

        var selectedAny = false;
        foreach (var obj in Nodes)
        {
            if (obj is not CgDiagramNode n)
            {
                continue;
            }

            var nx0 = n.Position.X;
            var ny0 = n.Position.Y;
            var size = n.Size ?? new Size(n.Width, n.Height);
            var nx1 = nx0 + size.Width;
            var ny1 = ny0 + size.Height;

            var intersects =
                nx0 <= x1 && nx1 >= x0 &&
                ny0 <= y1 && ny1 >= y0;

            n.IsSelected = intersects;
            selectedAny |= intersects;
        }

        if (Edges is not null)
        {
            foreach (var obj in Edges)
            {
                if (obj is CgDiagramEdge e)
                {
                    e.IsSelected = false;
                }
            }
        }

        if (selectedAny)
        {
            RefreshEdges();
        }
        else
        {
            RefreshEdges();
        }
    }

    /// <summary>
    /// This method reconnects collection and property listeners after the control loads or collections change.
    /// </summary>
    void RefreshSubscriptionsAndEdges()
    {
        if (!IsLoaded)
        {
            return;
        }

        HookCollections();
        HookNodePositionChanges();
        RefreshEdges();
    }

    /// <summary>
    /// This method selects exactly one node and clears selection on all other nodes and edges.
    /// </summary>
    void SelectNode(CgDiagramNode node)
    {
        if (Nodes is not null)
        {
            foreach (var obj in Nodes)
            {
                if (obj is CgDiagramNode n)
                {
                    n.IsSelected = ReferenceEquals(n, node);
                }
            }
        }

        if (Edges is not null)
        {
            foreach (var obj in Edges)
            {
                if (obj is CgDiagramEdge e)
                {
                    e.IsSelected = false;
                }
            }
        }

        RefreshEdges();
    }

    /// <summary>
    /// This method selects exactly one edge and clears selection on every node and other edges.
    /// </summary>
    void SelectEdge(CgDiagramEdge edge)
    {
        if (Edges is not null)
        {
            foreach (var obj in Edges)
            {
                if (obj is CgDiagramEdge e)
                {
                    e.IsSelected = ReferenceEquals(e, edge);
                }
            }
        }

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

        RefreshEdges();
    }

    /// <summary>
    /// This method attaches collection changed handlers to the current nodes and edges enumerations.
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
    /// This method refreshes node property hooks and redraws connectors whenever the nodes collection mutates.
    /// </summary>
    void OnNodesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        HookNodePositionChanges();
        RefreshEdges();
    }

    /// <summary>
    /// This method redraws connectors whenever the edges collection mutates.
    /// </summary>
    void OnEdgesCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        RefreshEdges();
    }

    /// <summary>
    /// This method rebinds property changed handlers so connector geometry follows individual node moves.
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
                        RefreshEdges();
                    }
                };
                n.PropertyChanged += h;
                _nodeHandlers[n] = h;
            }
        }
    }

    /// <summary>
    /// This method rebuilds all connector visuals, hit targets, and content extent measurements from current model state.
    /// </summary>
    void RefreshEdges()
    {
        if (_edgesCanvas is null)
        {
            return;
        }

        _edgesCanvas.Children.Clear();
        _edgesCanvas.IsHitTestVisible = true;

        var nodes = Nodes;
        var edges = Edges;
        if (nodes is null || edges is null)
        {
            ContentWidth = 0;
            ContentHeight = 0;
            return;
        }

        var map = new Dictionary<string, Point>(StringComparer.Ordinal);
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

                // Anchor edges at node center by default.
                map[n.Id] = new Point(n.Position.X + size.Width * 0.5, n.Position.Y + size.Height * 0.5);
            }
        }

        if (anyNodes)
        {
            var pad = ContentPadding;
            var w = (maxX - minX) + pad.Left + pad.Right;
            var h = (maxY - minY) + pad.Top + pad.Bottom;
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

            if (!map.TryGetValue(e.FromId, out var p0) || !map.TryGetValue(e.ToId, out var p1))
            {
                continue;
            }

            var visualLine = new Line
            {
                X1 = p0.X,
                Y1 = p0.Y,
                X2 = p1.X,
                Y2 = p1.Y,
                Stroke = e.IsSelected ? Brushes.DeepSkyBlue
                    : e.IsHovered ? Brushes.White
                    : EdgeStroke,
                StrokeThickness = EdgeThickness,
                SnapsToDevicePixels = true
            };

            var hitThickness = Math.Max(8.0, EdgeThickness + 6.0);
            var hitLine = new Line
            {
                X1 = p0.X,
                Y1 = p0.Y,
                X2 = p1.X,
                Y2 = p1.Y,
                Stroke = Brushes.Transparent,
                StrokeThickness = hitThickness,
                SnapsToDevicePixels = true,
                Cursor = Cursors.Hand,
                Tag = e
            };

            hitLine.MouseEnter += (_, _) =>
            {
                e.IsHovered = true;
                RefreshEdges();
            };
            hitLine.MouseLeave += (_, _) =>
            {
                e.IsHovered = false;
                RefreshEdges();
            };
            hitLine.PreviewMouseLeftButtonDown += (_, args) =>
            {
                if (args.Handled) return;
                SelectEdge(e);
                RaiseEvent(new CgDiagramEdgeEventArgs(EdgeClickEvent, e, args.GetPosition(this)));
                args.Handled = true;
            };
            hitLine.PreviewMouseRightButtonDown += (_, args) =>
            {
                if (args.Handled) return;
                SelectEdge(e);
                RaiseEvent(new CgDiagramEdgeEventArgs(EdgeRightClickEvent, e, args.GetPosition(this)));
                args.Handled = true;
            };

            _edgesCanvas.Children.Add(visualLine);
            _edgesCanvas.Children.Add(hitLine);
        }
    }

    /// <summary>
    /// This method removes collection and property listeners when the control unloads to avoid leaks.
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

