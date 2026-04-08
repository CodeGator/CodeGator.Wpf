using CodeGator.Wpf;

namespace CodeGator.Wpf.UnitTests;

/// <summary>
/// This class contains automated tests that verify construction and behavior of <see cref="CgDiagramEdge"/>.
/// </summary>
public sealed class CgDiagramEdgeTests
{
    /// <summary>
    /// This method verifies that constructor arguments populate endpoint ids and optional label.
    /// </summary>
    [Fact]
    public void Constructor_sets_endpoints_and_label()
    {
        var edge = new CgDiagramEdge("from", "to", "relates");

        Assert.Equal("from", edge.FromId);
        Assert.Equal("to", edge.ToId);
        Assert.Equal("relates", edge.Label);
    }

    /// <summary>
    /// This method asserts that selection changes emit property notifications only when values change.
    /// </summary>
    [Fact]
    public void IsSelected_toggle_raises_PropertyChanged()
    {
        var edge = new CgDiagramEdge("a", "b");
        var names = new List<string?>();
        edge.PropertyChanged += (_, e) => names.Add(e.PropertyName);

        edge.IsSelected = true;
        edge.IsSelected = false;

        Assert.Equal(new[] { nameof(CgDiagramEdge.IsSelected), nameof(CgDiagramEdge.IsSelected) }, names);
    }

    /// <summary>
    /// This method asserts that hover flags notify listeners once per actual transition.
    /// </summary>
    [Fact]
    public void IsHovered_toggle_raises_PropertyChanged()
    {
        var edge = new CgDiagramEdge("a", "b");
        var names = new List<string?>();
        edge.PropertyChanged += (_, e) => names.Add(e.PropertyName);

        edge.IsHovered = true;
        edge.IsHovered = true;

        Assert.Single(names);
        Assert.Equal(nameof(CgDiagramEdge.IsHovered), names[0]);
    }
}
