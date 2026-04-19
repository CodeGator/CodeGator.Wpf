using System.Windows;
using CodeGator.Wpf;
using CodeGator.Wpf.Layouts;

namespace CodeGator.Wpf.UnitTests;

/// <summary>
/// This class contains automated tests that exercise built-in diagram layout algorithms and their geometric outcomes.
/// </summary>
public sealed class DiagramLayoutAlgorithmTests
{
    /// <summary>
    /// This property returns shared spacing and force settings reused by several layout assertions.
    /// </summary>
    static CgDiagramLayoutOptions Options => new(new Size(100, 50), 80, 60, forceIterations: 80, forceSeed: 42);

    /// <summary>
    /// This method verifies that the top-down hierarchical layout stacks chain nodes with increasing Y.
    /// </summary>
    [Fact]
    public void HierarchicalTopDown_chain_orders_layers_along_Y()
    {
        var nodes = new List<CgDiagramNode>
        {
            new("a", "A"),
            new("b", "B"),
            new("c", "C"),
        };
        var edges = new List<CgDiagramEdge>
        {
            new("a", "b"),
            new("b", "c"),
        };

        var positions = CgDiagramLayouts.Resolve(CgDiagramLayoutIds.HierarchicalTopDown)
            .Compute(nodes, edges, Options);

        Assert.Equal(3, positions.Count);
        Assert.True(positions["a"].Y < positions["b"].Y);
        Assert.True(positions["b"].Y < positions["c"].Y);
        Assert.All(positions.Values, p => Assert.True(p.X >= 0 && p.Y >= 0));
    }

    /// <summary>
    /// This method verifies that the left-to-right hierarchical layout advances chain nodes with increasing X.
    /// </summary>
    [Fact]
    public void HierarchicalLeftToRight_chain_orders_layers_along_X()
    {
        var nodes = new List<CgDiagramNode>
        {
            new("a", "A"),
            new("b", "B"),
            new("c", "C"),
        };
        var edges = new List<CgDiagramEdge>
        {
            new("a", "b"),
            new("b", "c"),
        };

        var positions = CgDiagramLayouts.Resolve(CgDiagramLayoutIds.HierarchicalLeftToRight)
            .Compute(nodes, edges, Options);

        Assert.Equal(3, positions.Count);
        Assert.True(positions["a"].X < positions["b"].X);
        Assert.True(positions["b"].X < positions["c"].X);
        Assert.All(positions.Values, p => Assert.True(p.X >= 0 && p.Y >= 0));
    }

    /// <summary>
    /// This method verifies that swimlane layout places distinct lane identifiers on different rows.
    /// </summary>
    [Fact]
    public void Swimlanes_separates_groups_by_SwimlaneId_on_Y()
    {
        var nodes = new List<CgDiagramNode>
        {
            new("x1", "X1") { SwimlaneId = "LaneA" },
            new("y1", "Y1") { SwimlaneId = "LaneB" },
        };

        var positions = CgDiagramLayouts.Resolve(CgDiagramLayoutIds.Swimlanes)
            .Compute(nodes, [], Options);

        Assert.Equal(2, positions.Count);
        Assert.NotEqual(positions["x1"].Y, positions["y1"].Y);
    }

    /// <summary>
    /// This method verifies that nodes without swimlane ids share the default lane row.
    /// </summary>
    [Fact]
    public void Swimlanes_empty_SwimlaneId_groups_under_Default()
    {
        var nodes = new List<CgDiagramNode>
        {
            new("n1", "N1"),
            new("n2", "N2"),
        };

        var positions = CgDiagramLayouts.Resolve(CgDiagramLayoutIds.Swimlanes)
            .Compute(nodes, [], Options);

        Assert.Equal(2, positions.Count);
        Assert.Equal(positions["n1"].Y, positions["n2"].Y);
    }

    /// <summary>
    /// This method verifies that circular ring coordinates remain in the first quadrant after layout.
    /// </summary>
    [Fact]
    public void CircularRing_places_all_nodes_with_non_negative_origin()
    {
        var nodes = new List<CgDiagramNode>
        {
            new("w", "W"),
            new("x", "X"),
            new("y", "Y"),
            new("z", "Z"),
        };

        var positions = CgDiagramLayouts.Resolve(CgDiagramLayoutIds.CircularRing)
            .Compute(nodes, [], Options);

        Assert.Equal(4, positions.Count);
        foreach (var n in nodes)
        {
            Assert.True(positions.ContainsKey(n.Id));
            var p = positions[n.Id];
            Assert.True(p.X >= 0 && p.Y >= 0);
        }
    }

    /// <summary>
    /// This method verifies that a single-node radial layout still produces a usable non-negative position.
    /// </summary>
    [Fact]
    public void Radial_single_root_has_one_position()
    {
        var nodes = new List<CgDiagramNode> { new("root", "Root") };

        var positions = CgDiagramLayouts.Resolve(CgDiagramLayoutIds.Radial)
            .Compute(nodes, [], Options);

        Assert.Single(positions);
        var p = positions["root"];
        Assert.True(p.X >= 0 && p.Y >= 0);
    }

    /// <summary>
    /// This method ensures every built-in layout returns an empty map when no nodes are provided.
    /// </summary>
    [Fact]
    public void Each_layout_returns_empty_for_empty_node_list()
    {
        var opts = Options;
        foreach (var id in CgDiagramLayoutIds.All)
        {
            var positions = CgDiagramLayouts.Resolve(id).Compute([], [], opts);
            Assert.Empty(positions);
        }
    }
}
