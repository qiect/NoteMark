using System.Xml.Linq;
using NoteMark.ImportExport;
using NoteMark.OneNoteConverter;
using NoteMark.ThemeManager;

namespace NoteMark.AddIn;

public sealed class ExportHandler
{
    private const string OneNoteNamespace = "http://schemas.microsoft.com/office/onenote/2013/onenote";

    private readonly OneNoteApiWrapper _api;
    private readonly OneNoteXmlConverter _converter = new();
    private readonly MarkdownExporter _exporter = new();
    private readonly MarkdownImporter _importer = new();
    private readonly NoteMark.ThemeManager.ThemeManager _themeManager;
    private readonly AddInSettings _settings;

    public ExportHandler(OneNoteApiWrapper api, NoteMark.ThemeManager.ThemeManager themeManager, AddInSettings settings)
    {
        _api = api;
        _themeManager = themeManager;
        _settings = settings;
    }

    public void HandleF8Export()
    {
        try
        {
            var pageId = GetCurrentPageId();
            if (pageId is null)
            {
                AppLogger.Instance.LogWarning("F8 export: no current page found");
                return;
            }

            _api.GetPageContent(pageId, out var pageXml);
            var markdown = _converter.ConvertFromOneNoteXmlToMarkdown(pageXml);

            ClipboardHelper.SetText(markdown);
            AppLogger.Instance.LogInfo("F8 export: page exported to clipboard");
        }
        catch (Exception ex)
        {
            AppLogger.Instance.LogError("HandleF8Export failed", ex);
        }
    }

    public async Task HandleExportToFile()
    {
        try
        {
            var pageId = GetCurrentPageId();
            if (pageId is null)
            {
                AppLogger.Instance.LogWarning("Export to file: no current page found");
                return;
            }

            using var dialog = new System.Windows.Forms.SaveFileDialog
            {
                Filter = "Markdown files (*.md)|*.md|All files (*.*)|*.*",
                DefaultExt = "md",
                Title = "Export Markdown",
                FileName = $"export_{DateTime.Now:yyyyMMdd_HHmmss}.md"
            };

            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            _api.GetPageContent(pageId, out var pageXml);
            var markdown = _converter.ConvertFromOneNoteXmlToMarkdown(pageXml);

            var document = new MarkdownEngine.MarkdownDocument { RawContent = markdown };
            await _exporter.ExportToFile(document, dialog.FileName);

            AppLogger.Instance.LogInfo($"Export to file: {dialog.FileName}");
        }
        catch (Exception ex)
        {
            AppLogger.Instance.LogError("HandleExportToFile failed", ex);
        }
    }

    public async Task HandleImportFromFile()
    {
        try
        {
            using var dialog = new System.Windows.Forms.OpenFileDialog
            {
                Filter = "Markdown files (*.md)|*.md|All files (*.*)|*.*",
                Title = "Import Markdown",
                CheckFileExists = true
            };

            if (dialog.ShowDialog() != System.Windows.Forms.DialogResult.OK) return;

            var document = await _importer.ImportFromFile(dialog.FileName);
            var theme = GetCurrentTheme();

            var pageId = GetCurrentPageId();
            if (pageId is not null)
            {
                using var pageUpdater = new OneNotePageUpdater(_api);
                pageUpdater.AppendContentToPage(pageId, document.RawContent, theme);
            }

            AppLogger.Instance.LogInfo($"Import from file: {dialog.FileName}");
        }
        catch (Exception ex)
        {
            AppLogger.Instance.LogError("HandleImportFromFile failed", ex);
        }
    }

    private string? GetCurrentPageId()
    {
        try
        {
            _api.GetHierarchy(string.Empty, Microsoft.Office.Interop.OneNote.HierarchyScope.hsPages, out var hierarchyXml);
            var doc = XDocument.Parse(hierarchyXml);
            var ns = XNamespace.Get(OneNoteNamespace);

            var currentPage = doc.Descendants(ns + "Page")
                .FirstOrDefault(p => p.Attribute("isCurrentlyViewed")?.Value == "true");

            return currentPage?.Attribute("ID")?.Value;
        }
        catch (Exception ex)
        {
            AppLogger.Instance.LogError("GetCurrentPageId failed", ex);
            return null;
        }
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
}
