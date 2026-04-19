namespace CodeGator.Wpf;

/// <summary>
/// This enumeration represents optional content drawn inside the force-directed node circle.
/// </summary>
public enum CgDiagramNodeCircleGlyphKind
{
    /// <summary>
    /// This enumeration member leaves the circle interior empty (default).
    /// </summary>
    None = 0,

    /// <summary>
    /// This enumeration member shows <see cref="CgDiagramNode.CircleGlyph"/> as centered text.
    /// </summary>
    Text = 1,

    /// <summary>
    /// This enumeration member shows <see cref="CgDiagramNode.CircleGlyph"/> as SVG path mini-language.
    /// </summary>
    SvgPathData = 2,

    /// <summary>
    /// This enumeration member shows <see cref="CgDiagramNode.CircleGlyph"/> as an SVG file path or URI.
    /// </summary>
    SvgFile = 3,

    /// <summary>
    /// This enumeration member shows <see cref="CgDiagramNode.CircleGlyph"/> as a bitmap URI or file path.
    /// </summary>
    BitmapSource = 4,
}
