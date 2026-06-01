using System.Xml.Linq;
using NoteMark.MarkdownEngine;
using NoteMark.OneNoteConverter;
using NoteMark.RenderingServices;
using NoteMark.ThemeManager;

namespace NoteMark.AddIn;

public sealed class MarkdownRenderHandler : IDisposable
{
    private const string OneNoteNamespace = "http://schemas.microsoft.com/office/onenote/2013/onenote";

    private readonly OneNoteApiWrapper _api;
    private readonly OneNoteXmlConverter _converter = new();
    private readonly OneNotePageUpdater _pageUpdater;
    private readonly MarkdownParser _parser = new();
    private readonly CodeHighlightService _highlightService = new();
    private readonly NoteMark.ThemeManager.ThemeManager _themeManager;
    private readonly AddInSettings _settings;
    private bool _disposed;

    public MarkdownRenderHandler(OneNoteApiWrapper api, NoteMark.ThemeManager.ThemeManager themeManager, AddInSettings settings)
    {
        _api = api;
        _themeManager = themeManager;
        _settings = settings;
        _pageUpdater = new OneNotePageUpdater(api);
    }

    public void HandleEnterKey()
    {
        if (!_settings.IsRealtimeRenderEnabled) return;

        try
        {
            var currentLine = GetCurrentLineText();
            if (string.IsNullOrWhiteSpace(currentLine)) return;

            if (!LooksLikeMarkdown(currentLine)) return;

            RenderCurrentLine(currentLine);
            AppLogger.Instance.LogInfo($"Realtime render triggered for: {currentLine.Substring(0, Math.Min(currentLine.Length, 50))}");
        }
        catch (Exception ex)
        {
            AppLogger.Instance.LogError("HandleEnterKey failed", ex);
        }
    }

    public void HandleF5Render()
    {
        try
        {
            var selectedText = _pageUpdater.GetSelectedText();

            if (!string.IsNullOrWhiteSpace(selectedText))
            {
                RenderSelection(selectedText);
                AppLogger.Instance.LogInfo("F5 render: rendered selected text");
            }
            else
            {
                var currentLine = GetCurrentLineText();
                if (!string.IsNullOrWhiteSpace(currentLine))
                {
                    RenderCurrentLine(currentLine);
                    AppLogger.Instance.LogInfo("F5 render: rendered current line");
                }
            }
        }
        catch (Exception ex)
        {
            AppLogger.Instance.LogError("HandleF5Render failed", ex);
        }
    }

    public void HandleCtrlEnter()
    {
        try
        {
            var currentLine = GetCurrentLineText();
            if (string.IsNullOrWhiteSpace(currentLine)) return;

            var blockId = GetBlockId();
            if (blockId is null) return;

            _settings.SetSourceMode(blockId, false);
            RenderCurrentLine(currentLine);
            AppLogger.Instance.LogInfo("Ctrl+Enter: exited block source mode");
        }
        catch (Exception ex)
        {
            AppLogger.Instance.LogError("HandleCtrlEnter failed", ex);
        }
    }

    public void HandleSourceModeToggle()
    {
        try
        {
            var blockId = GetBlockId();
            if (blockId is null) return;

            var isSourceMode = _settings.IsSourceMode(blockId);
            _settings.SetSourceMode(blockId, !isSourceMode);

            if (!isSourceMode)
            {
                AppLogger.Instance.LogInfo($"Source mode enabled for block: {blockId}");
            }
            else
            {
                var currentLine = GetCurrentLineText();
                if (!string.IsNullOrWhiteSpace(currentLine))
                    RenderCurrentLine(currentLine);

                AppLogger.Instance.LogInfo($"Source mode disabled for block: {blockId}");
            }
        }
        catch (Exception ex)
        {
            AppLogger.Instance.LogError("HandleSourceModeToggle failed", ex);
        }
    }

    public void HandleTabRender()
    {
        if (!_settings.IsRealtimeRenderEnabled) return;

        try
        {
            var currentLine = GetCurrentLineText();
            if (string.IsNullOrWhiteSpace(currentLine)) return;

            if (IsInTableOrList(currentLine))
            {
                RenderCurrentLine(currentLine);
                AppLogger.Instance.LogInfo("Tab render triggered in table/list");
            }
        }
        catch (Exception ex)
        {
            AppLogger.Instance.LogError("HandleTabRender failed", ex);
        }
    }

    private void RenderCurrentLine(string markdown)
    {
        var theme = GetCurrentTheme();
        _pageUpdater.ReplaceSelectedText(markdown, theme);
    }

    private void RenderSelection(string markdown)
    {
        var theme = GetCurrentTheme();
        _pageUpdater.ReplaceSelectedText(markdown, theme);
    }

    private Theme GetCurrentTheme()
    {
        var themes = _themeManager.GetThemeList();
        var currentThemeName = _settings.CurrentThemeName;
        return themes.FirstOrDefault(t =>
            string.Equals(t.Name, currentThemeName, StringComparison.OrdinalIgnoreCase)) ?? themes.FirstOrDefault() ?? new Theme
        {
            Name = "default",
            FileName = "default.css",
            FilePath = "",
            CssContent = ""
        };
    }

    private string GetCurrentLineText()
    {
        try
        {
            _api.GetHierarchy(string.Empty, Microsoft.Office.Interop.OneNote.HierarchyScope.hsPages, out var hierarchyXml);
            var doc = XDocument.Parse(hierarchyXml);
            var ns = XNamespace.Get(OneNoteNamespace);

            var currentPage = doc.Descendants(ns + "Page")
                .FirstOrDefault(p => p.Attribute("isCurrentlyViewed")?.Value == "true");

            if (currentPage is null) return string.Empty;

            var pageId = currentPage.Attribute("ID")?.Value;
            if (pageId is null) return string.Empty;

            _api.GetPageContent(pageId, out var pageXml);
            var pageDoc = XDocument.Parse(pageXml);

            var selected = pageDoc.Descendants(ns + "T")
                .FirstOrDefault(t => t.Attribute("selected")?.Value == "all");

            if (selected is null) return string.Empty;

            var cdata = selected.Nodes().OfType<XCData>().FirstOrDefault();
            return cdata?.Value?.Trim() ?? selected.Value.Trim();
        }
        catch (Exception ex)
        {
            AppLogger.Instance.LogError("GetCurrentLineText failed", ex);
            return string.Empty;
        }
    }

    private string? GetBlockId()
    {
        try
        {
            _api.GetHierarchy(string.Empty, Microsoft.Office.Interop.OneNote.HierarchyScope.hsPages, out var hierarchyXml);
            var doc = XDocument.Parse(hierarchyXml);
            var ns = XNamespace.Get(OneNoteNamespace);

            var currentPage = doc.Descendants(ns + "Page")
                .FirstOrDefault(p => p.Attribute("isCurrentlyViewed")?.Value == "true");

            if (currentPage is null) return null;

            var pageId = currentPage.Attribute("ID")?.Value;
            if (pageId is null) return null;

            _api.GetPageContent(pageId, out var pageXml);
            var pageDoc = XDocument.Parse(pageXml);

            var selected = pageDoc.Descendants(ns + "T")
                .FirstOrDefault(t => t.Attribute("selected")?.Value == "all");

            return selected?.Attribute("objectID")?.Value;
        }
        catch (Exception ex)
        {
            AppLogger.Instance.LogError("GetBlockId failed", ex);
            return null;
        }
    }

    private static bool LooksLikeMarkdown(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;

        var trimmed = text.TrimStart();

        if (trimmed.StartsWith("#") && trimmed.Length > 1 && (trimmed[1] == ' ' || trimmed[1] == '#')) return true;
        if (trimmed.StartsWith("- ") || trimmed.StartsWith("* ") || trimmed.StartsWith("+ ")) return true;
        if (trimmed.StartsWith("> ")) return true;
        if (trimmed.StartsWith("```")) return true;
        if (trimmed.StartsWith("---") || trimmed.StartsWith("***") || trimmed.StartsWith("___")) return true;
        if (trimmed.StartsWith("|") && trimmed.IndexOf('|', 1) >= 0) return true;
        if (System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^\d+\.\s")) return true;
        if (trimmed.Contains("**") || trimmed.Contains("__")) return true;
        if (trimmed.Contains("$$")) return true;
        if (trimmed.StartsWith("- [") || trimmed.StartsWith("- [x]") || trimmed.StartsWith("- [X]")) return true;
        if (trimmed.StartsWith("[") && trimmed.Contains("](")) return true;
        if (trimmed.StartsWith("![")) return true;
        if (trimmed.Contains("`")) return true;

        return false;
    }

    private static bool IsInTableOrList(string text)
    {
        if (string.IsNullOrWhiteSpace(text)) return false;

        var trimmed = text.TrimStart();
        return trimmed.StartsWith("|") ||
               trimmed.StartsWith("- ") ||
               trimmed.StartsWith("* ") ||
               System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^\d+\.\s");
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        _pageUpdater.Dispose();
    }
}
