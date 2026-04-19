using System.Windows;
using System.Windows.Media;

namespace CodeGator.Wpf;

/// <summary>
/// This class provides factory methods for tiled diagram hatch brushes.
/// </summary>
/// <remarks>
/// Hosts can build consistent node fills and connector strokes from the same parameters.
/// </remarks>
public static class CgDiagramBrushes
{
    /// <summary>
    /// This method builds a tiled diagonal hatch brush for fills or strokes.
    /// </summary>
    /// <param name="background">The brush painted behind the hatch lines.</param>
    /// <param name="line">The brush used for diagonal lines.</param>
    /// <param name="lineThickness">The pen thickness for hatch lines, in logical pixels.</param>
    /// <param name="tileSize">The width and height of each tiled cell, in logical pixels.</param>
    /// <returns>A drawing brush configured to tile; frozen when possible.</returns>
    public static DrawingBrush CreateDiagonalHatchBrush(
        Brush background,
        Brush line,
        double lineThickness,
        double tileSize)
    {
        if (tileSize <= 0)
        {
            tileSize = 4.0;
        }

        var group = new DrawingGroup();
        group.Children.Add(new GeometryDrawing(
            background,
            null,
            new RectangleGeometry(new Rect(0, 0, tileSize, tileSize))));
        var pen = new Pen(line, lineThickness);
        group.Children.Add(new GeometryDrawing(
            null,
            pen,
            new LineGeometry(new Point(0, 0), new Point(tileSize, tileSize))));
        var brush = new DrawingBrush(group)
        {
            TileMode = TileMode.Tile,
            Viewport = new Rect(0, 0, tileSize, tileSize),
            ViewportUnits = BrushMappingMode.Absolute,
            Viewbox = new Rect(0, 0, tileSize, tileSize),
            ViewboxUnits = BrushMappingMode.Absolute
        };
        if (brush.CanFreeze)
        {
            brush.Freeze();
        }

        return brush;
    }
}
