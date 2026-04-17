namespace CodeGator.Wpf;

/// <summary>
/// This enumeration represents how diagram node content is presented in the UI.
/// </summary>
public enum CgDiagramNodePresentation
{
    /// <summary>
    /// This enumeration member uses the default surface card chrome.
    /// </summary>
    Surface,

    /// <summary>
    /// This enumeration member loads SVG from <see cref="CgDiagramNode.SvgSource"/>.
    /// </summary>
    SvgFile,

    /// <summary>
    /// This enumeration member renders from <see cref="CgDiagramNode.SvgPathData"/>.
    /// </summary>
    SvgPath,
}
