namespace CodeGator.Wpf;

/// <summary>
/// This enumeration selects how a diagram node content is presented in the UI.
/// </summary>
public enum CgDiagramNodePresentation
{
    /// <summary>
    /// This enumeration member uses the default surface card template with title and description chrome.
    /// </summary>
    Surface,

    /// <summary>
    /// This enumeration member loads vector content from the path stored in <see cref="CgDiagramNode.SvgSource"/>.
    /// </summary>
    SvgFile,

    /// <summary>
    /// This enumeration member renders vector geometry from <see cref="CgDiagramNode.SvgPathData"/> path mini-language.
    /// </summary>
    SvgPath,
}
