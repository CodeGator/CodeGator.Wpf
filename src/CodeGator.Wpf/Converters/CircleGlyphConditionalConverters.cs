using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace CodeGator.Wpf.Converters;

/// <summary>
/// This class supplies SVG file source strings for the circle glyph only when <see cref="CgDiagramNode.CircleGlyphKind"/>
/// is <see cref="CgDiagramNodeCircleGlyphKind.SvgFile"/>.
/// </summary>
public sealed class CircleGlyphSvgFileSourceConverter : IMultiValueConverter
{
    static readonly CgSvgSourceToUriConverter Inner = new();

    /// <summary>
    /// This method resolves the glyph string for SVG file mode, or returns unset when not applicable.
    /// </summary>
    public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not CgDiagramNodeCircleGlyphKind kind ||
            kind != CgDiagramNodeCircleGlyphKind.SvgFile)
        {
            return DependencyProperty.UnsetValue;
        }

        var uri = Inner.Convert(values[1], typeof(string), null!, culture);
        return uri ?? DependencyProperty.UnsetValue;
    }

    /// <summary>
    /// This method is not supported for this converter.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// This class supplies path geometry only when <see cref="CgDiagramNode.CircleGlyphKind"/> is
/// <see cref="CgDiagramNodeCircleGlyphKind.SvgPathData"/>.
/// </summary>
/// <remarks>
/// Geometry is translated so the visual center of the stroked path matches the template design square center.
/// Centering uses a widened <see cref="PathGeometry"/> from <see cref="Geometry.GetFlattenedPathGeometry(double, ToleranceType)"/>
/// so stroke thickness matches the glyph <see cref="System.Windows.Shapes.Path"/> in XAML. Optional
/// <c>ConverterParameter</c> is <c>designSide</c> or <c>designSide,strokeThickness</c> (defaults 100, 1.25).
/// </remarks>
public sealed class CircleGlyphPathGeometryConverter : IMultiValueConverter
{
    static readonly CgPathDataToGeometryConverter Inner = new();

    /// <summary>
    /// This method converts path mini-language only for SVG path glyph mode.
    /// </summary>
    /// <param name="values">Glyph kind and path mini-language string.</param>
    /// <param name="targetType">Unused.</param>
    /// <param name="parameter">Design square side, or <c>side,strokeThickness</c> (must match theme path stroke).</param>
    /// <param name="culture">The culture for parsing the parameter.</param>
    /// <returns>Centered frozen geometry, or <see cref="DependencyProperty.UnsetValue"/>.</returns>
    public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not CgDiagramNodeCircleGlyphKind kind ||
            kind != CgDiagramNodeCircleGlyphKind.SvgPathData)
        {
            return DependencyProperty.UnsetValue;
        }

        var geo = Inner.Convert(values[1], typeof(Geometry), null!, culture) as Geometry;
        if (geo is null)
        {
            return DependencyProperty.UnsetValue;
        }

        ParseDesignAndStroke(parameter as string, culture, out var designSide, out var strokeThickness);
        var half = designSide * 0.5;
        var bounds = GetStrokedCenteringBounds(geo, strokeThickness);
        if (bounds.IsEmpty || double.IsNaN(bounds.X) || double.IsNaN(bounds.Y))
        {
            return geo;
        }

        var cx = bounds.X + bounds.Width * 0.5;
        var cy = bounds.Y + bounds.Height * 0.5;
        var clone = geo.Clone();
        clone.Transform = new TranslateTransform(half - cx, half - cy);
        if (clone.CanFreeze)
        {
            clone.Freeze();
        }

        return clone;
    }

    static void ParseDesignAndStroke(string? parameter, CultureInfo culture, out double designSide, out double strokeThickness)
    {
        designSide = 100.0;
        strokeThickness = 1.25;
        if (string.IsNullOrWhiteSpace(parameter))
        {
            return;
        }

        var parts = parameter.Split(',');
        if (parts.Length >= 1 &&
            double.TryParse(parts[0].Trim(), NumberStyles.Float, culture, out var ds) &&
            ds > 0)
        {
            designSide = ds;
        }

        if (parts.Length >= 2 &&
            double.TryParse(parts[1].Trim(), NumberStyles.Float, culture, out var st) &&
            st > 0)
        {
            strokeThickness = st;
        }
    }

    static Rect GetStrokedCenteringBounds(Geometry geo, double strokeThickness)
    {
        try
        {
            var flat = geo.GetFlattenedPathGeometry(Geometry.StandardFlatteningTolerance, ToleranceType.Absolute);
            var pen = new Pen(Brushes.Black, strokeThickness);
            var widened = flat.GetWidenedPathGeometry(pen);
            if (widened is not null && !widened.Bounds.IsEmpty)
            {
                return widened.Bounds;
            }
        }
        catch
        {
        }

        var b = geo.Bounds;
        if (b.IsEmpty)
        {
            return b;
        }

        var halfStroke = strokeThickness * 0.5;
        b.Inflate(halfStroke, halfStroke);
        return b;
    }

    /// <summary>
    /// This method is not supported for this converter.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// This class supplies a bitmap only when <see cref="CgDiagramNode.CircleGlyphKind"/> is
/// <see cref="CgDiagramNodeCircleGlyphKind.BitmapSource"/>.
/// </summary>
public sealed class CircleGlyphBitmapSourceConverter : IMultiValueConverter
{
    static readonly CgGlyphStringToBitmapSourceConverter Inner = new();

    /// <summary>
    /// This method loads a bitmap only for bitmap glyph mode.
    /// </summary>
    public object? Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not CgDiagramNodeCircleGlyphKind kind ||
            kind != CgDiagramNodeCircleGlyphKind.BitmapSource)
        {
            return DependencyProperty.UnsetValue;
        }

        var img = Inner.Convert(values[1], typeof(ImageSource), null!, culture);
        return img ?? DependencyProperty.UnsetValue;
    }

    /// <summary>
    /// This method is not supported for this converter.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

/// <summary>
/// This class supplies circle glyph text only when <see cref="CgDiagramNode.CircleGlyphKind"/> is
/// <see cref="CgDiagramNodeCircleGlyphKind.Text"/>.
/// </summary>
public sealed class CircleGlyphTextOnlyConverter : IMultiValueConverter
{
    /// <summary>
    /// This method returns the glyph string for text mode, or an empty string otherwise.
    /// </summary>
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (values.Length < 2 || values[0] is not CgDiagramNodeCircleGlyphKind kind ||
            kind != CgDiagramNodeCircleGlyphKind.Text)
        {
            return string.Empty;
        }

        return values[1] as string ?? string.Empty;
    }

    /// <summary>
    /// This method is not supported for this converter.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
