using System.Globalization;
using System.Windows.Data;
using System.IO;

namespace CodeGator.Wpf.Converters;

/// <summary>
/// This class resolves SVG source strings from bindings to paths or URIs.
/// </summary>
/// <remarks>
/// Absolute URIs are returned as-is; relative paths are combined with the application base directory.
/// </remarks>
public sealed class CgSvgSourceToUriConverter : IValueConverter
{
    /// <summary>
    /// This method resolves a bound SVG source string to a path or URI for loaders.
    /// </summary>
    /// <param name="value">The SVG path or URI string from the binding source.</param>
    /// <param name="targetType">The target type requested by the binding engine.</param>
    /// <param name="parameter">An optional converter parameter (unused).</param>
    /// <param name="culture">The culture for conversion (unused).</param>
    /// <returns>A file path or URI string, or null when the value is empty.</returns>
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string s || string.IsNullOrWhiteSpace(s))
        {
            return null;
        }

        // SharpVectors' SvgCanvas supports either absolute file paths or pack URIs
        // via its Source property. For relative paths, resolve against app base dir.
        if (Uri.TryCreate(s, UriKind.Absolute, out var abs))
        {
            return abs.IsFile ? abs.LocalPath : abs.ToString();
        }

        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var combined = Path.Combine(baseDir, s.Replace('/', Path.DirectorySeparatorChar));
        return combined;
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

