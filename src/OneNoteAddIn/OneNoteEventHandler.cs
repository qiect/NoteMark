using System.Runtime.InteropServices;
using Microsoft.Office.Interop.OneNote;
using OneMarkDotNet.ThemeManager;

namespace OneMarkDotNet.AddIn;

public sealed class OneNoteEventHandler : IDisposable
{
    private Microsoft.Office.Interop.OneNote.Application? _app;
    private readonly OneMarkDotNet.ThemeManager.ThemeManager _themeManager;
    private readonly AddInSettings _settings;
    private string _currentPageId = string.Empty;
    private bool _disposed;

    public event Action<string>? PageChanged;
#pragma warning disable CS0067
    public event Action<string>? ContentChanged;
#pragma warning restore CS0067

    public OneNoteEventHandler(OneMarkDotNet.ThemeManager.ThemeManager themeManager, AddInSettings settings)
    {
        _themeManager = themeManager;
        _settings = settings;
    }

    public void Initialize(IApplication application)
    {
        try
        {
            _app = application as Microsoft.Office.Interop.OneNote.Application;
            if (_app is not null)
            {
                _app.OnHierarchyChange += OnHierarchyChange;
                AppLogger.Instance.LogInfo("OneNote event handler initialized");
            }
            else
            {
                AppLogger.Instance.LogWarning("Could not cast IApplication to Application; event handling disabled");
            }
        }
        catch (Exception ex)
        {
            AppLogger.Instance.LogError("Failed to initialize OneNote event handler", ex);
        }
    }

    private void OnHierarchyChange(string changesXml)
    {
        if (_disposed) return;

        try
        {
            var pageId = ExtractCurrentPageId(changesXml);
            if (pageId is null || pageId == _currentPageId) return;

            var oldPageId = _currentPageId;
            _currentPageId = pageId;

            PageChanged?.Invoke(pageId);
            AppLogger.Instance.LogInfo($"Page changed: {oldPageId} -> {pageId}");
        }
        catch (Exception ex)
        {
            AppLogger.Instance.LogError("OnHierarchyChange handler failed", ex);
        }
    }

    private static string? ExtractCurrentPageId(string changesXml)
    {
        try
        {
            var doc = System.Xml.Linq.XDocument.Parse(changesXml);
            var ns = System.Xml.Linq.XNamespace.Get("http://schemas.microsoft.com/office/onenote/2013/onenote");
            var page = doc.Descendants(ns + "Page")
                .FirstOrDefault(p => p.Attribute("isCurrentlyViewed")?.Value == "true");
            return page?.Attribute("ID")?.Value;
        }
        catch
        {
            return null;
        }
    }

    public string CurrentPageId => _currentPageId;

    public bool IsPageTracked(string pageId)
    {
        return !string.IsNullOrEmpty(_currentPageId) && _currentPageId == pageId;
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_app is not null)
        {
            try
            {
                _app.OnHierarchyChange -= OnHierarchyChange;
            }
            catch
            {
            }

            _app = null;
        }
    }
}
