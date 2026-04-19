using CodeGator.Wpf.Layouts;

namespace CodeGator.Wpf.UnitTests;

/// <summary>
/// This class contains automated tests for <see cref="CgDiagramLayouts"/> resolution and registration.
/// </summary>
public sealed class CgDiagramLayoutsTests
{
    /// <summary>
    /// This property supplies theory rows enumerating every built-in layout id.
    /// </summary>
    public static TheoryData<string> AllBuiltinIds => new()
    {
        CgDiagramLayoutIds.HierarchicalTopDown,
        CgDiagramLayoutIds.HierarchicalLeftToRight,
        CgDiagramLayoutIds.Radial,
        CgDiagramLayoutIds.Swimlanes,
        CgDiagramLayoutIds.CircularRing,
    };

    /// <summary>
    /// This method checks that each built-in id resolves to a non-null layout implementation.
    /// </summary>
    [Theory]
    [MemberData(nameof(AllBuiltinIds))]
    public void Resolve_returns_layout_instance(string layoutId)
    {
        var layout = CgDiagramLayouts.Resolve(layoutId);
        Assert.NotNull(layout);
    }

    /// <summary>
    /// This method ensures unknown ids throw <see cref="KeyNotFoundException"/>.
    /// </summary>
    [Fact]
    public void Resolve_unknown_id_throws()
    {
        Assert.Throws<KeyNotFoundException>(() =>
            CgDiagramLayouts.Resolve("__no_such_layout__"));
    }

    /// <summary>
    /// This method ensures <see cref="CgDiagramLayouts.Register"/> exposes custom layouts by id.
    /// </summary>
    [Fact]
    public void Register_then_Resolve_uses_custom_factory()
    {
        var customId = $"__unit_test_custom_layout_{Guid.NewGuid():N}__";
        CgDiagramLayouts.Register(
            customId,
            () => CgDiagramLayouts.Resolve(CgDiagramLayoutIds.HierarchicalTopDown));
        var layout = CgDiagramLayouts.Resolve(customId);
        Assert.NotNull(layout);
    }

    /// <summary>
    /// This method ensures duplicate registration throws <see cref="InvalidOperationException"/>.
    /// </summary>
    [Fact]
    public void Register_duplicate_id_throws()
    {
        Assert.Throws<InvalidOperationException>(() =>
            CgDiagramLayouts.Register(
                CgDiagramLayoutIds.Radial,
                () => CgDiagramLayouts.Resolve(CgDiagramLayoutIds.Radial)));
    }
}
