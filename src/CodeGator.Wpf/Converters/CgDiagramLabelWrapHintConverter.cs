using System.Globalization;
using System.Windows.Data;

namespace CodeGator.Wpf.Converters;

/// <summary>
/// This class maps a node label to a string with soft break hints for <see cref="CgDiagram"/> label layout.
/// </summary>
public sealed class CgDiagramLabelWrapHintConverter : IValueConverter
{
    /// <summary>
    /// This method applies <see cref="CgDiagramLabelWrapFormatting.InsertLineBreakHints"/> to the bound string.
    /// </summary>
    /// <param name="value">The label string (typically <see cref="CgDiagramNode.Label"/>).</param>
    /// <param name="targetType">Unused.</param>
    /// <param name="parameter">Unused.</param>
    /// <param name="culture">Unused.</param>
    /// <returns>The hinted string for display.</returns>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        CgDiagramLabelWrapFormatting.InsertLineBreakHints(value as string);

    /// <summary>
    /// This method is not supported for this converter.
    /// </summary>
    /// <returns>Throws <see cref="NotSupportedException"/>.</returns>
    /// <exception cref="NotSupportedException">Always thrown.</exception>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
