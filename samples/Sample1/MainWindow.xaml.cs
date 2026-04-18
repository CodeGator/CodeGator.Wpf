using System.Windows;
using CodeGator.Wpf;
using CodeGator.Wpf.Args;
using CodeGator.Wpf.Layouts;

namespace Sample1;

/// <summary>
/// This class is the sample application's main window, wiring the diagram control to the view model and user commands.
/// </summary>
public partial class MainWindow : Window
{
    readonly MainWindowViewModel _vm = new();

    /// <summary>
    /// This method initializes the window, assigns the view model, loads sample data, and applies the initial layout.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        DataContext = _vm;
        _vm.LoadSampleGraph();
        Diagram.ApplyLayout(_vm.LayoutId);
    }

    /// <summary>
    /// This method handles reset view commands by invoking the view model camera defaults.
    /// </summary>
    void ResetView_Click(object sender, RoutedEventArgs e) => _vm.ResetView();

    /// <summary>
    /// This method prompts for a print destination and records the outcome in the view model status text.
    /// </summary>
    void Print_Click(object sender, RoutedEventArgs e)
    {
        if (Diagram.Print("Sample1 - CgDiagram", fitToPage: true))
        {
            _vm.LastEvent = "Printed diagram (fit to page).";
        }
        else
        {
            _vm.LastEvent = "Print cancelled.";
        }
    }

    /// <summary>
    /// This method reloads the sample graph and reapplies the currently selected layout algorithm.
    /// </summary>
    void LoadSampleGraph_Click(object sender, RoutedEventArgs e)
    {
        _vm.LoadSampleGraph();
        Diagram.ApplyLayout(_vm.LayoutId);
        _vm.LastEvent = "Loaded sample graph.";
    }

    /// <summary>
    /// This method applies the newly chosen layout id and updates the last-event status message.
    /// </summary>
    void Layout_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        Diagram.ApplyLayout(_vm.LayoutId);
        _vm.LastEvent = $"Applied layout: {_vm.LayoutId}";
    }

    /// <summary>
    /// This method reflects a left-click on a diagram node in the view model status line.
    /// </summary>
    void Diagram_NodeClick(object sender, CgDiagramNodeEventArgs e) =>
        _vm.LastEvent = $"Node click: {e.Node.Id} ({e.Node.Label})";

    /// <summary>
    /// This method reflects a right-click on a diagram node in the view model status line.
    /// </summary>
    void Diagram_NodeRightClick(object sender, CgDiagramNodeEventArgs e) =>
        _vm.LastEvent = $"Node right-click: {e.Node.Id} ({e.Node.Label})";

    /// <summary>
    /// This method reflects a left-click on a diagram edge in the view model status line.
    /// </summary>
    void Diagram_EdgeClick(object sender, CgDiagramEdgeEventArgs e) =>
        _vm.LastEvent = $"Edge click: {e.Edge.FromId} → {e.Edge.ToId}";

    /// <summary>
    /// This method reflects a right-click on a diagram edge in the view model status line.
    /// </summary>
    void Diagram_EdgeRightClick(object sender, CgDiagramEdgeEventArgs e) =>
        _vm.LastEvent = $"Edge right-click: {e.Edge.FromId} → {e.Edge.ToId}";
}