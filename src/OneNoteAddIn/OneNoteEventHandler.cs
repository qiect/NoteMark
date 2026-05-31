using System.Runtime.InteropServices;
using Microsoft.Office.Interop.OneNote;
using OneMarkDotNet.ThemeManager;

namespace OneMarkDotNet.AddIn;

public sealed class OneNoteEventHandler : IDisposable
{
    private Application? _app;
    private readonly ThemeManager _themeManager;
    private readonly AddInSettings _settings;
    private string _currentPageId = string.Empty;
    private bool _disposed;

    public event Action<string>? PageChanged;
    public event Action<string>? ContentChanged;

    public OneNoteEventHandler(ThemeManager themeManager, AddInSettings settings)
    {
        _themeManager = themeManager;
        _settings = settings;
    }

    public void Initialize(IApplication application)
    {
        try
        {
            _app = (Application)application;
            _app.OnPageChange += OnPageChange;
            AppLogger.Instance.LogInfo("OneNote event handler initialized");
        }
        catch (Exception ex)
        {
            AppLogger.Instance.LogError("Failed to initialize OneNote event handler", ex);
        }
    }

    private void OnPageChange(string pageId)
    {
        if (_disposed) return;

        try
        {
            if (pageId == _currentPageId) return;

            var oldPageId = _currentPageId;
            _currentPageId = pageId;

            PageChanged?.Invoke(pageId);
            AppLogger.Instance.LogInfo($"Page changed: {oldPageId} -> {pageId}");
        }
        catch (Exception ex)
        {
            AppLogger.Instance.LogError("OnPageChange handler failed", ex);
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
                _app.OnPageChange -= OnPageChange;
            }
            catch
            {
            }

            _app = null;
        }
    }
}
