using System.Windows;
using CodeGator.Wpf;

namespace CodeGator.Wpf.UnitTests;

/// <summary>
/// This class contains automated tests that verify construction and behavior of <see cref="CgDiagramNode"/>.
/// </summary>
public sealed class CgDiagramNodeTests
{
    /// <summary>
    /// This method verifies that constructor arguments populate id, label, and description fields.
    /// </summary>
    [Fact]
    public void Constructor_sets_identity_fields()
    {
        var node = new CgDiagramNode("n1", "Label", "Detail");

        Assert.Equal("n1", node.Id);
        Assert.Equal("Label", node.Label);
        Assert.Equal("Detail", node.Description);
    }

    /// <summary>
    /// This method asserts that assigning a new position raises property change notification.
    /// </summary>
    [Fact]
    public void Position_change_raises_PropertyChanged()
    {
        var node = new CgDiagramNode("a", "A");
        string? changed = null;
        node.PropertyChanged += (_, e) => changed = e.PropertyName;

        node.Position = new Point(10, 20);

        Assert.Equal(nameof(CgDiagramNode.Position), changed);
        Assert.Equal(new Point(10, 20), node.Position);
    }

    /// <summary>
    /// This method asserts that resetting position to the existing coordinates does not notify again.
    /// </summary>
    [Fact]
    public void Position_same_value_does_not_raise_PropertyChanged()
    {
        var node = new CgDiagramNode("a", "A") { Position = new Point(1, 2) };
        var count = 0;
        node.PropertyChanged += (_, _) => count++;

        node.Position = new Point(1, 2);

        Assert.Equal(0, count);
    }

    /// <summary>
    /// This method asserts that meaningful selection toggles raise property change events.
    /// </summary>
    [Fact]
    public void IsSelected_toggle_raises_PropertyChanged()
    {
        var node = new CgDiagramNode("a", "A");
        var names = new List<string?>();
        node.PropertyChanged += (_, e) => names.Add(e.PropertyName);

        node.IsSelected = true;
        node.IsSelected = true;
        node.IsSelected = false;

        Assert.Equal(new[] { nameof(CgDiagramNode.IsSelected), nameof(CgDiagramNode.IsSelected) }, names);
    }
}
