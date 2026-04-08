using System.Globalization;
using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Media;

namespace CodeGator.Wpf.Converters;

/// <summary>
/// This class converts path mini-language strings into WPF <see cref="Geometry"/> values for data binding.
/// </summary>
public sealed class CgPathDataToGeometryConverter : IValueConverter
{
    /// <summary>
    /// This method converts a bound path string into a <see cref="Geometry"/> instance, or null when conversion fails.
    /// </summary>
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
    /// This method indicates that path data bindings do not support two-way conversion back to string.
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

