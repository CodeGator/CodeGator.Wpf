using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace CodeGator.Wpf.Converters;

/// <summary>
/// This class maps <see cref="CgDiagramNode.CircleGlyphKind"/> and <see cref="CgDiagramNode.CircleGlyph"/> to
/// <see cref="Visibility"/> for one overlay part identified by the converter parameter string.
/// </summary>
public sealed class CircleGlyphPartVisibilityConverter : IMultiValueConverter
{
    /// <summary>
    /// This method returns <see cref="Visibility.Visible"/> when the part matches the active glyph kind and glyph text.
    /// </summary>
    /// <param name="values">Index 0 is <see cref="CgDiagramNodeCircleGlyphKind"/>; index 1 is the glyph string.</param>
    /// <param name="targetType">Unused.</param>
    /// <param name="parameter">One of <c>Text</c>, <c>SvgPathData</c>, <c>SvgFile</c>, or <c>BitmapSource</c>.</param>
    /// <param name="culture">Unused.</param>
    /// <returns><see cref="Visibility.Visible"/> or <see cref="Visibility.Collapsed"/>.</returns>
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        if (parameter is not string part || values.Length < 2)
        {
            return Visibility.Collapsed;
        }

        var kind = values[0] is CgDiagramNodeCircleGlyphKind g ? g : CgDiagramNodeCircleGlyphKind.None;
        var glyph = values[1] as string;
        if (string.IsNullOrWhiteSpace(glyph) || kind == CgDiagramNodeCircleGlyphKind.None)
        {
            return Visibility.Collapsed;
        }

        return part switch
        {
            "Text" => kind == CgDiagramNodeCircleGlyphKind.Text ? Visibility.Visible : Visibility.Collapsed,
            "SvgPathData" => kind == CgDiagramNodeCircleGlyphKind.SvgPathData ? Visibility.Visible : Visibility.Collapsed,
            "SvgFile" => kind == CgDiagramNodeCircleGlyphKind.SvgFile ? Visibility.Visible : Visibility.Collapsed,
            "BitmapSource" => kind == CgDiagramNodeCircleGlyphKind.BitmapSource ? Visibility.Visible : Visibility.Collapsed,
            _ => Visibility.Collapsed,
        };
    }

    /// <summary>
    /// This method is not supported for this converter.
    /// </summary>
    /// <returns>Throws <see cref="NotSupportedException"/>.</returns>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
