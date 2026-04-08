using CodeGator.Wpf.Layouts;

namespace CodeGator.Wpf.UnitTests;

/// <summary>
/// This class contains automated tests for <see cref="CgDiagramBuiltinLayouts"/> and layout kind dispatch.
/// </summary>
public sealed class CgDiagramBuiltinLayoutsTests
{
    /// <summary>
    /// This property supplies theory rows enumerating every defined <see cref="CgDiagramLayoutKind"/> value.
    /// </summary>
    public static TheoryData<CgDiagramLayoutKind> AllKinds => new()
    {
        CgDiagramLayoutKind.HierarchicalTopDown,
        CgDiagramLayoutKind.HierarchicalLeftToRight,
        CgDiagramLayoutKind.Radial,
        CgDiagramLayoutKind.ForceDirected,
        CgDiagramLayoutKind.Swimlanes,
        CgDiagramLayoutKind.CircularRing,
    };

    /// <summary>
    /// This method checks that each layout kind resolves to a non-null layout implementation.
    /// </summary>
    [Theory]
    [MemberData(nameof(AllKinds))]
    public void For_returns_layout_instance(CgDiagramLayoutKind kind)
    {
        var layout = CgDiagramBuiltinLayouts.For(kind);
        Assert.NotNull(layout);
    }

    /// <summary>
    /// This method ensures unknown enum values throw <see cref="ArgumentOutOfRangeException"/>.
    /// </summary>
    [Fact]
    public void For_invalid_kind_throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            CgDiagramBuiltinLayouts.For((CgDiagramLayoutKind)999));
    }
}
