using System.Globalization;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Media;

namespace CodeGator.Wpf.Converters;

/// <summary>
/// This class converts path mini-language strings to <see cref="Geometry"/> values.
/// </summary>
public sealed class CgPathDataToGeometryConverter : IValueConverter
{
    /// <summary>
    /// This method converts a path string to geometry, or null if invalid.
    /// </summary>
    /// <param name="value">The path mini-language string from the binding source.</param>
    /// <param name="targetType">The target type requested by the binding engine.</param>
    /// <param name="parameter">An optional converter parameter (unused).</param>
    /// <param name="culture">The culture for conversion (unused).</param>
    /// <returns>A <see cref="Geometry"/> instance, or null when the value is not convertible.</returns>
    /// <remarks>
    /// On success, returns a WPF <see cref="Geometry"/> parsed from the path mini-language.
    /// </remarks>
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string s || string.IsNullOrWhiteSpace(s))
        {
            return null;
        }

        try
        {
            var c = TypeDescriptor.GetConverter(typeof(Geometry));
            return c.ConvertFromInvariantString(s) as Geometry;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// This method returns <see cref="Binding.DoNothing"/>; ConvertBack is unsupported.
    /// </summary>
    /// <param name="value">The value produced by the target (unused).</param>
    /// <param name="targetType">The source type requested for conversion (unused).</param>
    /// <param name="parameter">An optional converter parameter (unused).</param>
    /// <param name="culture">The culture for conversion (unused).</param>
    /// <returns><see cref="Binding.DoNothing"/> always.</returns>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

