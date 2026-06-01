using System.Runtime.InteropServices;
using Extensibility;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.OneNote;
using NoteMark.AddIn.Ribbon;
using NoteMark.OneNoteConverter;
using NoteMark.ThemeManager;

namespace NoteMark.AddIn;

[ComVisible(true)]
[Guid("B8F2E4A1-3D7C-4F9B-A5E6-8C1D2F3A4B5E")]
[ProgId("NoteMark.AddIn")]
[ClassInterface(ClassInterfaceType.None)]
public class NoteMarkAddIn : IDTExtensibility2, IRibbonExtensibility
{
    private IApplication? _oneNoteApp;
    private NoteMarkRibbon? _ribbon;
    private AppLogger? _logger;

    private readonly Lazy<KeyboardHook> _keyboardHook = new(() => new KeyboardHook());
    private readonly Lazy<OneNoteApiWrapper> _apiWrapper;
    private readonly Lazy<NoteMark.ThemeManager.ThemeManager> _themeManager;
    private readonly Lazy<AddInSettings> _settings;
    private readonly Lazy<MarkdownRenderHandler> _renderHandler;
    private readonly Lazy<ExportHandler> _exportHandler;
    private readonly Lazy<OneNoteEventHandler> _eventHandler;
    private readonly Lazy<WebView2Helper> _webViewHelper;

    private bool _keyboardHookInstalled;

    public NoteMarkAddIn()
    {
        DiagnosticLog("Instance constructor called");

        _settings = new Lazy<AddInSettings>(() =>
        {
            var s = AddInSettings.Instance;
            s.LoadSettings();
            DiagnosticLog("Settings loaded (lazy)");
            return s;
        });

        _themeManager = new Lazy<NoteMark.ThemeManager.ThemeManager>(() =>
        {
            var tm = new NoteMark.ThemeManager.ThemeManager();
            var themesDir = _settings.Value.GetThemesDirectory();
            if (!string.IsNullOrEmpty(themesDir))
            {
                Directory.CreateDirectory(themesDir);
                tm.LoadThemes(themesDir);
            }
            DiagnosticLog("ThemeManager initialized (lazy)");
            return tm;
        });

        _apiWrapper = new Lazy<OneNoteApiWrapper>(() =>
        {
            if (_oneNoteApp is null)
                throw new InvalidOperationException("OneNote application not available");
            var wrapper = new OneNoteApiWrapper(_oneNoteApp);
            DiagnosticLog("ApiWrapper initialized (lazy)");
            return wrapper;
        });

        _renderHandler = new Lazy<MarkdownRenderHandler>(() =>
        {
            var handler = new MarkdownRenderHandler(_apiWrapper.Value, _themeManager.Value, _settings.Value);
            DiagnosticLog("RenderHandler initialized (lazy)");
            return handler;
        });

        _exportHandler = new Lazy<ExportHandler>(() =>
        {
            var handler = new ExportHandler(_apiWrapper.Value, _themeManager.Value, _settings.Value);
            DiagnosticLog("ExportHandler initialized (lazy)");
            return handler;
        });

        _eventHandler = new Lazy<OneNoteEventHandler>(() =>
        {
            var handler = new OneNoteEventHandler(_themeManager.Value, _settings.Value);
            if (_oneNoteApp is not null)
            {
                handler.Initialize(_oneNoteApp);
            }
            DiagnosticLog("EventHandler initialized (lazy)");
            return handler;
        });

        _webViewHelper = new Lazy<WebView2Helper>(() =>
        {
            var helper = new WebView2Helper();
            DiagnosticLog("WebView2Helper initialized (lazy)");
            return helper;
        });
    }

    static NoteMarkAddIn()
    {
        DiagnosticLog("Static constructor called - .NET Framework 4.8 runtime loaded successfully");
    }

    private static void DiagnosticLog(string message)
    {
        try
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "NoteMark", "logs");
            Directory.CreateDirectory(dir);
            var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}\r\n";
            File.AppendAllText(Path.Combine(dir, "startup.log"), line);
        }
        catch
        {
        }
    }

    public void OnConnection(object application, ext_ConnectMode connectMode, object addInInst, ref Array custom)
    {
        DiagnosticLog($"OnConnection called, connectMode={connectMode}");

        try
        {
            _oneNoteApp = application as IApplication;
            if (_oneNoteApp is null)
            {
                DiagnosticLog($"OnConnection: application type={application?.GetType().Name ?? "null"}, casting...");
                _oneNoteApp = (IApplication)application!;
            }
            DiagnosticLog("IApplication acquired");
        }
        catch (Exception ex)
        {
            DiagnosticLog($"IApplication cast FAILED: {ex.GetType().Name}: {ex.Message}");
        }

        try
        {
            _logger = AppLogger.Instance;
            _logger.LogInfo("NoteMark AddIn connecting...");
        }
        catch (Exception ex)
        {
            DiagnosticLog($"Logger init FAILED: {ex.GetType().Name}: {ex.Message}");
        }

        try
        {
            if (_themeManager is not null && _settings is not null)
            {
                _ribbon = new NoteMarkRibbon(_themeManager.Value);
            }
            DiagnosticLog("Ribbon initialized");
        }
        catch (Exception ex)
        {
            DiagnosticLog($"Ribbon FAILED: {ex.GetType().Name}: {ex.Message}");
        }

        _logger?.LogInfo("NoteMark AddIn OnConnection completed");
        DiagnosticLog("OnConnection completed - heavy init deferred to Lazy");
    }

    public void OnDisconnection(ext_DisconnectMode removeMode, ref Array custom)
    {
        try
        {
            _logger?.LogInfo("NoteMark AddIn disconnecting...");

            if (_keyboardHook.IsValueCreated && _keyboardHookInstalled)
            {
                _keyboardHook.Value.Dispose();
                _keyboardHookInstalled = false;
            }

            if (_renderHandler.IsValueCreated)
                _renderHandler.Value.Dispose();

            if (_eventHandler.IsValueCreated)
                _eventHandler.Value.Dispose();

            if (_apiWrapper.IsValueCreated)
                _apiWrapper.Value.Dispose();

            if (_webViewHelper.IsValueCreated)
                _webViewHelper.Value.Dispose();

            _settings.Value.SaveSettings();
            _logger?.LogInfo("NoteMark AddIn disconnected");
        }
        catch (Exception ex)
        {
            DiagnosticLog($"OnDisconnection FAILED: {ex.GetType().Name}: {ex.Message}");
        }
    }

    public void OnAddInsUpdate(ref Array custom)
    {
    }

    public void OnStartupComplete(ref Array custom)
    {
        DiagnosticLog("OnStartupComplete called");
        _logger?.LogInfo("NoteMark AddIn startup complete");
    }

    public void OnBeginShutdown(ref Array custom)
    {
        _logger?.LogInfo("NoteMark AddIn beginning shutdown");
    }

    public string GetCustomUI(string ribbonId)
    {
        DiagnosticLog($"GetCustomUI called, ribbonId={ribbonId}");
        try
        {
            return _ribbon?.GetCustomUI(ribbonId) ?? string.Empty;
        }
        catch (Exception ex)
        {
            DiagnosticLog($"GetCustomUI FAILED: {ex.GetType().Name}: {ex.Message}");
            return string.Empty;
        }
    }

    public void RibbonOnLoad(IRibbonUI ribbonUi)
    {
        DiagnosticLog("RibbonOnLoad called");
        try
        {
            _ribbon?.RibbonOnLoad(ribbonUi);
        }
        catch (Exception ex)
        {
            DiagnosticLog($"RibbonOnLoad FAILED: {ex.GetType().Name}: {ex.Message}");
        }
    }

    public void OnRenderMarkdown(IRibbonControl control)
    {
        EnsureKeyboardHook();
        try { _renderHandler.Value.HandleF5Render(); }
        catch (Exception ex) { _logger?.LogError("OnRenderMarkdown failed", ex); }
    }

    public void OnExportMarkdown(IRibbonControl control)
    {
        EnsureKeyboardHook();
        try { _exportHandler.Value.HandleF8Export(); }
        catch (Exception ex) { _logger?.LogError("OnExportMarkdown failed", ex); }
    }

    public void OnSourceModeToggle(IRibbonControl control)
    {
        try { _renderHandler.Value.HandleSourceModeToggle(); }
        catch (Exception ex) { _logger?.LogError("OnSourceModeToggle failed", ex); }
    }

    public void OnOpenThemeDirectory(IRibbonControl control)
    {
        try
        {
            var themesDir = _settings.Value.GetThemesDirectory();
            if (!string.IsNullOrEmpty(themesDir))
            {
                Directory.CreateDirectory(themesDir);
                System.Diagnostics.Process.Start("explorer.exe", themesDir);
            }
        }
        catch (Exception ex) { _logger?.LogError("OnOpenThemeDirectory failed", ex); }
    }

    public void OnReloadThemes(IRibbonControl control)
    {
        try
        {
            _themeManager.Value.ReloadThemes();
            _ribbon?.InvalidateControl("dynThemeMenu");
            _logger?.LogInfo("Themes reloaded");
        }
        catch (Exception ex) { _logger?.LogError("OnReloadThemes failed", ex); }
    }

    public void OnImportMarkdown(IRibbonControl control)
    {
        try { _exportHandler.Value.HandleImportFromFile().GetAwaiter().GetResult(); }
        catch (Exception ex) { _logger?.LogError("OnImportMarkdown failed", ex); }
    }

    public void OnExportMarkdownFile(IRibbonControl control)
    {
        try { _exportHandler.Value.HandleExportToFile().GetAwaiter().GetResult(); }
        catch (Exception ex) { _logger?.LogError("OnExportMarkdownFile failed", ex); }
    }

    public void OnAbout(IRibbonControl control)
    {
        try
        {
            System.Windows.Forms.MessageBox.Show(
                "NoteMark v1.0.0\n\nMarkdown rendering for OneNote\nhttps://github.com/onemark",
                "About NoteMark",
                System.Windows.Forms.MessageBoxButtons.OK,
                System.Windows.Forms.MessageBoxIcon.Information);
        }
        catch (Exception ex) { _logger?.LogError("OnAbout failed", ex); }
    }

    public void OnThemeSelected(IRibbonControl control)
    {
        try
        {
            var themeName = control.Tag;
            if (string.IsNullOrEmpty(themeName)) return;
            _settings.Value.CurrentThemeName = themeName;
            _settings.Value.SaveSettings();
            _ribbon?.InvalidateControl("dynThemeMenu");
            _logger?.LogInfo($"Theme changed to: {themeName}");
        }
        catch (Exception ex) { _logger?.LogError("OnThemeSelected failed", ex); }
    }

    public string GetThemeMenuContent(IRibbonControl control)
    {
        try
        {
            return _ribbon?.GetThemeMenuContent(control) ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger?.LogError("GetThemeMenuContent failed", ex);
            return string.Empty;
        }
    }

    private void EnsureKeyboardHook()
    {
        if (_keyboardHookInstalled) return;

        try
        {
            SubscribeKeyboardEvents();
            _keyboardHook.Value.Install();
            _keyboardHookInstalled = true;
            DiagnosticLog("KeyboardHook installed (on first button click)");
        }
        catch (Exception ex)
        {
            DiagnosticLog($"KeyboardHook install FAILED: {ex.GetType().Name}: {ex.Message}");
        }
    }

    private void SubscribeKeyboardEvents()
    {
        var hook = _keyboardHook.Value;
        hook.EnterPressed += OnEnterPressed;
        hook.CtrlEnterPressed += OnCtrlEnterPressed;
        hook.CtrlCommaPressed += OnCtrlCommaPressed;
        hook.F5Pressed += OnF5Pressed;
        hook.F8Pressed += OnF8Pressed;
        hook.TabPressed += OnTabPressed;
    }

    private void OnEnterPressed() { try { _renderHandler.Value.HandleEnterKey(); } catch { } }
    private void OnCtrlEnterPressed() { try { _renderHandler.Value.HandleCtrlEnter(); } catch { } }
    private void OnCtrlCommaPressed() { try { _renderHandler.Value.HandleSourceModeToggle(); } catch { } }
    private void OnF5Pressed() { try { _renderHandler.Value.HandleF5Render(); } catch { } }
    private void OnF8Pressed() { try { _exportHandler.Value.HandleF8Export(); } catch { } }
    private void OnTabPressed() { try { _renderHandler.Value.HandleTabRender(); } catch { } }
}
