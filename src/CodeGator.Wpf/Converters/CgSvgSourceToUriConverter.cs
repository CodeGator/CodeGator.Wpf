using System.Globalization;
using System.Windows.Data;
using System.IO;

namespace CodeGator.Wpf.Converters;

/// <summary>
/// This class resolves SVG source strings from bindings into file paths or URIs suitable for rendering.
/// </summary>
/// <remarks>
/// Absolute URIs are returned as-is; relative paths are combined with the application base directory.
/// </remarks>
public sealed class CgSvgSourceToUriConverter : IValueConverter
{
    /// <summary>
    /// This method turns a bound SVG source string into a file path or URI string understood by SVG loaders.
    /// </summary>
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
    /// This method indicates that SVG source bindings do not support two-way conversion back to the original string.
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        Binding.DoNothing;
}

