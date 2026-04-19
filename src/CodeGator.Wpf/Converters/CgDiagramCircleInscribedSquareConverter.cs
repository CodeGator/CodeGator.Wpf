using System.Globalization;
using System.Windows.Data;

namespace CodeGator.Wpf.Converters;

/// <summary>
/// This class converts a circular node diameter to the largest square that fits inside the circle.
/// </summary>
/// <remarks>
/// Square Viewbox content uses MaxWidth and MaxHeight set from this value so corners and strokes
/// stay inside the circular clip; a small inset factor leaves room for stroke thickness.
/// </remarks>
public sealed class CgDiagramCircleInscribedSquareConverter : IValueConverter
{
    const double StrokeInset = 0.93;

    /// <summary>
    /// This method returns the side length of an axis-aligned square inscribed in the circle.
    /// </summary>
    /// <param name="value">The circle diameter, typically <see cref="CgDiagramNode.Width"/>.</param>
    /// <param name="targetType">Unused.</param>
    /// <param name="parameter">Unused.</param>
    /// <param name="culture">Unused.</param>
    /// <returns>The maximum width or height for square glyph content, or zero when invalid.</returns>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        var diameter = value is double d ? d : 0;
        if (diameter <= 0)
        {
            return 0.0;
        }

        return diameter * StrokeInset / Math.Sqrt(2.0);
    }

    /// <summary>
    /// This method is not supported for this converter.
    /// </summary>
    /// <returns>Throws <see cref="NotSupportedException"/>.</returns>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
