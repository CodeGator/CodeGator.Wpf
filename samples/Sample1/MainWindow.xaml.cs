using System.Windows;
using CodeGator.Wpf;

namespace Sample1;

/// <summary>
/// This class is the sample application's main window, wiring diagram controls to the view model.
/// </summary>
public partial class MainWindow : Window
{
    readonly MainWindowViewModel _vm = new();

    /// <summary>
    /// This method initializes the window, assigns the view model, loads sample data, and starts the diagram simulation.
    /// </summary>
    public MainWindow()
    {
        InitializeComponent();
        DataContext = _vm;
        _vm.LoadSampleGraphForce();
        Diagram.ResetSimulation();
    }

    /// <summary>
    /// This method resets zoom and pan for the force-directed diagram.
    /// </summary>
    void ResetViewForce_Click(object sender, RoutedEventArgs e) => _vm.ResetViewForce();

    /// <summary>
    /// This method prints the force-directed diagram.
    /// </summary>
    void PrintForce_Click(object sender, RoutedEventArgs e)
    {
        var ok = Diagram.Print("Sample1 - CgDiagram", fitToPage: true);
        _vm.ForceLastEvent = ok ? "Printed force-directed diagram (fit to page)." : "Print cancelled.";
    }

    /// <summary>
    /// This method reloads the force-directed sample graph and restarts simulation.
    /// </summary>
    void LoadSampleGraphForce_Click(object sender, RoutedEventArgs e)
    {
        _vm.LoadSampleGraphForce();
        Diagram.ResetSimulation();
        _vm.ForceLastEvent = "Loaded sample graph (CgDiagram).";
    }
}
