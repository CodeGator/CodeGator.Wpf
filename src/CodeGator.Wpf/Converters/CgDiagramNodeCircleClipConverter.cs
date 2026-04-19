using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace CodeGator.Wpf.Converters;

/// <summary>
/// This class maps node width to a circular clip for <see cref="CgDiagram"/> node visuals.
/// </summary>
public sealed class CgDiagramNodeCircleClipConverter : IValueConverter
{
    /// <summary>
    /// This method builds a frozen <see cref="EllipseGeometry"/> from a width value.
    /// </summary>
    /// <param name="value">The bound width (typically <see cref="CgDiagramNode.Width"/>).</param>
    /// <param name="targetType">The target type requested by the binding engine.</param>
    /// <param name="parameter">An optional converter parameter (unused).</param>
    /// <param name="culture">The culture for conversion (unused).</param>
    /// <returns>A frozen ellipse or <see cref="Geometry.Empty"/> when width is invalid.</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var w = value is double d ? d : 0;
        if (w <= 0)
        {
            return Geometry.Empty;
        }

        var r = w * 0.5;
        var geo = new EllipseGeometry(new Point(r, r), r, r);
        if (geo.CanFreeze)
        {
            geo.Freeze();
        }

        return geo;
    }

    /// <summary>
    /// This method is not supported for this converter.
    /// </summary>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
