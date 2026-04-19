using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace CodeGator.Wpf.Converters;

/// <summary>
/// This class builds a frozen <see cref="BitmapImage"/> from a file path or absolute URI string for circle glyphs.
/// </summary>
public sealed class CgGlyphStringToBitmapSourceConverter : IValueConverter
{
    /// <summary>
    /// This method loads a bitmap when <paramref name="value"/> is a non-empty string path or URI.
    /// </summary>
    /// <param name="value">A pack URI, absolute URI, or file path string.</param>
    /// <param name="targetType">Unused.</param>
    /// <param name="parameter">Unused.</param>
    /// <param name="culture">Unused.</param>
    /// <returns>A <see cref="BitmapImage"/>, or null when the value is not loadable.</returns>
    public object? Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not string s || string.IsNullOrWhiteSpace(s))
        {
            return null;
        }

        try
        {
            Uri uri;
            if (Uri.TryCreate(s, UriKind.Absolute, out var abs))
            {
                uri = abs;
            }
            else
            {
                var combined = Path.IsPathRooted(s)
                    ? s
                    : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, s.Replace('/', Path.DirectorySeparatorChar));
                uri = new Uri(combined, UriKind.Absolute);
            }

            var bmp = new BitmapImage();
            bmp.BeginInit();
            bmp.UriSource = uri;
            bmp.CacheOption = BitmapCacheOption.OnLoad;
            bmp.CreateOptions = BitmapCreateOptions.IgnoreColorProfile;
            bmp.EndInit();
            if (bmp.CanFreeze)
            {
                bmp.Freeze();
            }

            return bmp;
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// This method is not supported for this converter.
    /// </summary>
    /// <returns>Throws <see cref="NotSupportedException"/>.</returns>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
