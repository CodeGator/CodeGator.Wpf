using System.Collections.Concurrent;

namespace CodeGator.Wpf.Layouts;

/// <summary>
/// This class resolves <see cref="ICgDiagramLayout"/> instances from string layout ids and supports
/// application-defined registrations.
/// </summary>
public static class CgDiagramLayouts
{
    static readonly ConcurrentDictionary<string, Func<ICgDiagramLayout>> Factories =
        new(StringComparer.Ordinal)
        {
            [CgDiagramLayoutIds.HierarchicalTopDown] = () => new HierarchicalTopDownLayout(),
            [CgDiagramLayoutIds.HierarchicalLeftToRight] = () => new HierarchicalLeftToRightLayout(),
            [CgDiagramLayoutIds.Radial] = () => new RadialLayout(),
            [CgDiagramLayoutIds.ForceDirected] = () => new ForceDirectedLayout(),
            [CgDiagramLayoutIds.Swimlanes] = () => new SwimlaneLayout(),
            [CgDiagramLayoutIds.CircularRing] = () => new CircularRingLayout(),
        };

    /// <summary>
    /// This method registers a layout factory under <paramref name="layoutId"/> for the process.
    /// </summary>
    /// <param name="layoutId">The non-empty id callers pass to <see cref="Resolve"/> or diagram APIs.</param>
    /// <param name="factory">The factory that creates a fresh layout instance for each resolve.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="layoutId"/> is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="factory"/> is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="layoutId"/> is already registered.
    /// </exception>
    public static void Register(string layoutId, Func<ICgDiagramLayout> factory)
    {
        ArgumentException.ThrowIfNullOrEmpty(layoutId);
        ArgumentNullException.ThrowIfNull(factory);
        if (!Factories.TryAdd(layoutId, factory))
        {
            throw new InvalidOperationException(
                $"A diagram layout is already registered for id '{layoutId}'.");
        }
    }

    /// <summary>
    /// This method returns a new layout instance for the given <paramref name="layoutId"/>.
    /// </summary>
    /// <param name="layoutId">The registered layout identifier.</param>
    /// <returns>A new <see cref="ICgDiagramLayout"/> from the registered factory.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="layoutId"/> is null or empty.</exception>
    /// <exception cref="KeyNotFoundException">
    /// Thrown when no factory is registered for <paramref name="layoutId"/>.
    /// </exception>
    public static ICgDiagramLayout Resolve(string layoutId)
    {
        ArgumentException.ThrowIfNullOrEmpty(layoutId);
        if (!Factories.TryGetValue(layoutId, out var factory))
        {
            throw new KeyNotFoundException(
                $"No diagram layout is registered for id '{layoutId}'.");
        }

        return factory();
    }
}
