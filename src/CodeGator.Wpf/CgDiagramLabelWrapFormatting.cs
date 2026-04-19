using System.Text;

namespace CodeGator.Wpf;

/// <summary>
/// This class prepares diagram node labels for friendlier line breaking in narrow layouts.
/// </summary>
public static class CgDiagramLabelWrapFormatting
{
    const char ZeroWidthSpace = '\u200B';

    /// <summary>
    /// This method inserts zero-width spaces so wrapping can occur after delimiters and at camelCase boundaries.
    /// </summary>
    /// <param name="label">The raw label text.</param>
    /// <returns>The same text with break hints inserted, or an empty string when <paramref name="label"/> is null.</returns>
    public static string InsertLineBreakHints(string? label)
    {
        if (string.IsNullOrEmpty(label))
        {
            return label ?? string.Empty;
        }

        var extra = Math.Min(label.Length / 4, 48);
        var sb = new StringBuilder(label.Length + extra);
        for (var i = 0; i < label.Length; i++)
        {
            var c = label[i];
            if (i > 0 && char.IsUpper(c))
            {
                var p = label[i - 1];
                if (char.IsLower(p) || char.IsDigit(p))
                {
                    sb.Append(ZeroWidthSpace);
                }
            }

            sb.Append(c);

            if (c is '.' or '-' or '_' or '/' or '\\')
            {
                if (i + 1 < label.Length && !char.IsWhiteSpace(label[i + 1]) && label[i + 1] != ZeroWidthSpace)
                {
                    sb.Append(ZeroWidthSpace);
                }
            }
        }

        return sb.ToString();
    }
}
