using System.Reflection;
using System.Runtime.InteropServices;
using System.Xml;
using Microsoft.Office.Core;
using OneMarkDotNet.ThemeManager;

namespace OneMarkDotNet.AddIn.Ribbon;

[ComVisible(false)]
[ClassInterface(ClassInterfaceType.None)]
public sealed class OneMarkRibbon
{
    private IRibbonUI? _ribbonUi;
    private readonly OneMarkDotNet.ThemeManager.ThemeManager _themeManager;

    public event Action? RenderMarkdownRequested;
    public event Action? ExportMarkdownRequested;
    public event Action? SourceModeToggleRequested;
    public event Action<string>? ThemeSelected;
    public event Action? OpenThemeDirectoryRequested;
    public event Action? ReloadThemesRequested;
    public event Action? ImportMarkdownRequested;
    public event Action? ExportMarkdownFileRequested;
    public event Action? AboutRequested;

    public OneMarkRibbon(OneMarkDotNet.ThemeManager.ThemeManager themeManager)
    {
        _themeManager = themeManager;
    }

    public string GetCustomUI(string ribbonId)
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = "OneMarkDotNet.AddIn.Ribbon.RibbonXml.xml";

        using var stream = assembly.GetManifestResourceStream(resourceName);
        if (stream is null)
        {
            AppLogger.Instance.LogError($"Embedded resource '{resourceName}' not found");
            return string.Empty;
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public void RibbonOnLoad(IRibbonUI ribbonUi)
    {
        _ribbonUi = ribbonUi;
        AppLogger.Instance.LogInfo("Ribbon UI loaded");
    }

    public void OnRenderMarkdown(IRibbonControl control)
    {
        RenderMarkdownRequested?.Invoke();
    }

    public void OnExportMarkdown(IRibbonControl control)
    {
        ExportMarkdownRequested?.Invoke();
    }

    public void OnSourceModeToggle(IRibbonControl control)
    {
        SourceModeToggleRequested?.Invoke();
    }

    public void OnOpenThemeDirectory(IRibbonControl control)
    {
        OpenThemeDirectoryRequested?.Invoke();
    }

    public void OnReloadThemes(IRibbonControl control)
    {
        ReloadThemesRequested?.Invoke();
    }

    public void OnImportMarkdown(IRibbonControl control)
    {
        ImportMarkdownRequested?.Invoke();
    }

    public void OnExportMarkdownFile(IRibbonControl control)
    {
        ExportMarkdownFileRequested?.Invoke();
    }

    public void OnAbout(IRibbonControl control)
    {
        AboutRequested?.Invoke();
    }

    public void OnThemeSelected(IRibbonControl control)
    {
        var themeName = control.Tag;
        if (!string.IsNullOrEmpty(themeName))
        {
            ThemeSelected?.Invoke(themeName);
        }
    }

    public string GetThemeMenuContent(IRibbonControl control)
    {
        var themes = _themeManager.GetThemeList();
        var currentTheme = AddInSettings.Instance.CurrentThemeName;

        var sb = new System.Text.StringBuilder();
        sb.Append("<menu xmlns=\"http://schemas.microsoft.com/office/2009/07/customui\">");

        foreach (var theme in themes)
        {
            var isChecked = string.Equals(theme.Name, currentTheme, StringComparison.OrdinalIgnoreCase);
            sb.Append($"<button id=\"btnTheme_{theme.Name}\" ");
            sb.Append($"label=\"{System.Security.SecurityElement.Escape(theme.Name)}\" ");
            sb.Append($"tag=\"{System.Security.SecurityElement.Escape(theme.Name)}\" ");
            sb.Append($"onAction=\"OnThemeSelected\" ");
            if (isChecked)
                sb.Append("imageMso=\"AcceptInvitation\" ");
            sb.Append("/>");
        }

        sb.Append("</menu>");
        return sb.ToString();
    }

    public void Invalidate()
    {
        _ribbonUi?.Invalidate();
    }

    public void InvalidateControl(string controlId)
    {
        _ribbonUi?.InvalidateControl(controlId);
    }
}
