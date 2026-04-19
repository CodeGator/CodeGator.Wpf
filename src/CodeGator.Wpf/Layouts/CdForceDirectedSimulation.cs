using System.Windows;
using CodeGator.Wpf;

namespace CodeGator.Wpf.Layouts;

/// <summary>
/// This class holds force-directed simulation state for the diagram control.
/// </summary>
/// <remarks>
/// Forces follow the resource graph (d3-force) model: adaptive link length by degree,
/// many-body repulsion, collision, and weak centering.
/// </remarks>
internal sealed class CdForceDirectedSimulation
{
    readonly Dictionary<string, Vector> _pos = new(StringComparer.Ordinal);
    readonly Dictionary<string, int> _degree = new(StringComparer.Ordinal);
    string[] _ids = Array.Empty<string>();
    List<(string From, string To)> _links = new();
    double _alpha = 1.0;

    /// <summary>
    /// This method re-seeds positions and rebuilds topology from the current graph.
    /// </summary>
    public void Reset(IReadOnlyList<CgDiagramNode> nodes, IReadOnlyList<CgDiagramEdge> edges, CgDiagramLayoutOptions options)
    {
        _pos.Clear();
        _degree.Clear();
        _links.Clear();
        var n = nodes.Count;
        if (n == 0)
        {
            _ids = Array.Empty<string>();
            return;
        }

        var rng = new Random(options.ForceSeed);
        var side = (int)Math.Ceiling(Math.Sqrt(n));
        var step = Math.Max(options.NodeSize.Width, options.NodeSize.Height) * 1.2;
        for (var i = 0; i < n; i++)
        {
            _pos[nodes[i].Id] = new Vector(
                i % side * step + (rng.NextDouble() - 0.5) * 10,
                i / side * step + (rng.NextDouble() - 0.5) * 10);
        }

        _ids = nodes.Select(x => x.Id).ToArray();
        ComputeDegreesAndLinks(nodes, edges);
        _alpha = 1.0;
    }

    void ComputeDegreesAndLinks(IReadOnlyList<CgDiagramNode> nodes, IReadOnlyList<CgDiagramEdge> edges)
    {
        foreach (var id in _ids)
        {
            _degree[id] = 0;
        }

        var edgeSet = new HashSet<(string, string)>();
        foreach (var e in edges)
        {
            if (!_degree.ContainsKey(e.FromId) || !_degree.ContainsKey(e.ToId))
            {
                continue;
            }

            _degree[e.FromId]++;
            _degree[e.ToId]++;
            var key = string.CompareOrdinal(e.FromId, e.ToId) < 0 ? (e.FromId, e.ToId) : (e.ToId, e.FromId);
            if (edgeSet.Add(key))
            {
                _links.Add((e.FromId, e.ToId));
            }
        }
    }

    /// <summary>
    /// This method runs many physics steps, then flushes positions once.
    /// </summary>
    public void Settle(IReadOnlyList<CgDiagramNode> nodes, CgDiagramLayoutOptions options, int iterations, string? pinnedId)
    {
        if (_ids.Length == 0)
        {
            return;
        }

        for (var i = 0; i < iterations; i++)
        {
            StepPhysics(nodes, options, pinnedId, coolAlpha: true);
        }

        FlushPositions(nodes);
    }

    /// <summary>
    /// This method advances the simulation one step and writes node positions.
    /// </summary>
    /// <param name="nodes">The nodes whose positions are updated.</param>
    /// <param name="options">Layout and sizing options for the simulation.</param>
    /// <param name="pinnedId">The id of the node held under the pointer, if any.</param>
    /// <param name="normalizeDiagram">When true, shifts the graph so nothing clips at the diagram origin; set false while dragging.</param>
    public void Step(IReadOnlyList<CgDiagramNode> nodes, CgDiagramLayoutOptions options, string? pinnedId, bool normalizeDiagram = true)
    {
        if (_ids.Length == 0)
        {
            return;
        }

        StepPhysics(nodes, options, pinnedId, coolAlpha: true);
        ApplyToNodes(nodes);
        if (normalizeDiagram)
        {
            NormalizeDiagramPositions(nodes);
        }
    }

    void StepPhysics(IReadOnlyList<CgDiagramNode> nodes, CgDiagramLayoutOptions options, string? pinnedId, bool coolAlpha)
    {
        var chargeStrength = 800.0;
        var linkStrength = 0.12;
        var centerStrengthX = 0.12;
        var centerStrengthY = 0.22;

        var fx = new Dictionary<string, Vector>(StringComparer.Ordinal);
        foreach (var id in _ids)
        {
            fx[id] = new Vector(0, 0);
        }

        // Many-body repulsion (similar to d3.forceManyBody negative charge).
        for (var i = 0; i < _ids.Length; i++)
        {
            for (var j = i + 1; j < _ids.Length; j++)
            {
                var a = _ids[i];
                var b = _ids[j];
                var delta = _pos[a] - _pos[b];
                var dist = Math.Max(0.01, delta.Length);
                var f = chargeStrength / (dist * dist);
                var dir = delta / dist;
                fx[a] += dir * f;
                fx[b] -= dir * f;
            }
        }

        foreach (var (from, to) in _links)
        {
            var df = _degree.TryGetValue(from, out var d1) ? d1 : 1;
            var dt = _degree.TryGetValue(to, out var d2) ? d2 : 1;
            var maxDeg = Math.Max(Math.Max(1, df), Math.Max(1, dt));
            var ideal = Math.Min(150.0 + maxDeg * 10.0, 250.0);

            var delta = _pos[from] - _pos[to];
            var dist = Math.Max(0.01, delta.Length);
            var dir = delta / dist;
            var spring = (dist - ideal) * linkStrength;
            var force = dir * spring;
            fx[from] -= force;
            fx[to] += force;
        }

        // Weak pull toward centroid (d3.forceX / forceY analog).
        if (_ids.Length > 0)
        {
            double mx = 0, my = 0;
            foreach (var id in _ids)
            {
                mx += _pos[id].X;
                my += _pos[id].Y;
            }

            mx /= _ids.Length;
            my /= _ids.Length;
            foreach (var id in _ids)
            {
                fx[id] += new Vector((mx - _pos[id].X) * centerStrengthX, (my - _pos[id].Y) * centerStrengthY);
            }
        }

        var dtMove = 0.35 * _alpha;
        foreach (var id in _ids)
        {
            if (pinnedId is not null && id == pinnedId)
            {
                continue;
            }

            var d = fx[id];
            _pos[id] += d * dtMove;
        }

        ResolveCollisions();

        if (pinnedId is not null && TryFindNode(nodes, pinnedId, out var pinned))
        {
            _pos[pinnedId] = NodeCenterVector(pinned);
        }

        if (coolAlpha)
        {
            _alpha = Math.Max(0.001, _alpha * 0.98);
        }
    }

    void ResolveCollisions()
    {
        for (var pass = 0; pass < 3; pass++)
        {
            for (var i = 0; i < _ids.Length; i++)
            {
                for (var j = i + 1; j < _ids.Length; j++)
                {
                    var a = _ids[i];
                    var b = _ids[j];
                    var ra = CollideRadius(_degree.TryGetValue(a, out var da) ? da : 1);
                    var rb = CollideRadius(_degree.TryGetValue(b, out var db) ? db : 1);
                    var delta = _pos[b] - _pos[a];
                    var dist = delta.Length;
                    var minSep = ra + rb;
                    if (dist >= minSep || dist < 1e-9)
                    {
                        continue;
                    }

                    var n = delta / dist;
                    var push = (minSep - dist) * 0.5;
                    _pos[a] -= n * push;
                    _pos[b] += n * push;
                }
            }
        }
    }

    static double CollideRadius(int degree)
    {
        return Math.Min(90.0 + degree * 10.0, 180.0);
    }

    /// <summary>
    /// This method writes internal center positions to node top-left coordinates.
    /// </summary>
    public void FlushPositions(IReadOnlyList<CgDiagramNode> nodes)
    {
        ApplyToNodes(nodes);
        NormalizeDiagramPositions(nodes);
    }

    static bool TryFindNode(IReadOnlyList<CgDiagramNode> nodes, string id, out CgDiagramNode node)
    {
        foreach (var n in nodes)
        {
            if (n.Id == id)
            {
                node = n;
                return true;
            }
        }

        node = null!;
        return false;
    }

    static Vector NodeCenterVector(CgDiagramNode n)
    {
        var c = CgForceDirectedNodeMetrics.GetCircleCenter(n);
        return new Vector(c.X, c.Y);
    }

    /// <summary>
    /// This method overwrites simulation centers from current node positions.
    /// </summary>
    /// <remarks>
    /// Used after a bulk translate so physics centers stay aligned with node layout.
    /// </remarks>
    public void SyncCentersFromNodes(IReadOnlyList<CgDiagramNode> nodes)
    {
        if (_ids.Length == 0)
        {
            return;
        }

        foreach (var n in nodes)
        {
            if (_pos.ContainsKey(n.Id))
            {
                _pos[n.Id] = NodeCenterVector(n);
            }
        }
    }

    void ApplyToNodes(IReadOnlyList<CgDiagramNode> nodes)
    {
        foreach (var n in nodes)
        {
            if (!_pos.TryGetValue(n.Id, out var c))
            {
                continue;
            }

            var w = n.Size?.Width ?? n.Width;
            var head = CgForceDirectedNodeMetrics.GetHeadDiameter(n);
            n.Position = new Point(c.X - w * 0.5, c.Y - head * 0.5);
        }
    }

    /// <summary>
    /// This field holds the minimum inset from the diagram origin for node visuals.
    /// </summary>
    /// <remarks>
    /// Keeps content inside the scrollable area and avoids top or side clipping.
    /// </remarks>
    internal const double MinDiagramInset = 4.0;

    /// <summary>
    /// This method shifts nodes and centers using <see cref="MinDiagramInset"/>.
    /// </summary>
    /// <remarks>
    /// Keeps internal centers aligned with <see cref="CgDiagramNode.Position"/> after the shift.
    /// </remarks>
    void NormalizeDiagramPositions(IReadOnlyList<CgDiagramNode> nodes)
    {
        if (nodes.Count == 0)
        {
            return;
        }

        var minX = double.PositiveInfinity;
        var minY = double.PositiveInfinity;
        foreach (var n in nodes)
        {
            minX = Math.Min(minX, n.Position.X);
            minY = Math.Min(minY, n.Position.Y);
        }

        var dx = Math.Max(0.0, MinDiagramInset - minX);
        var dy = Math.Max(0.0, MinDiagramInset - minY);
        if (dx == 0 && dy == 0)
        {
            return;
        }

        var delta = new Vector(dx, dy);
        foreach (var n in nodes)
        {
            n.Position = new Point(n.Position.X + dx, n.Position.Y + dy);
        }

        foreach (var id in _ids)
        {
            _pos[id] += delta;
        }
    }
}
