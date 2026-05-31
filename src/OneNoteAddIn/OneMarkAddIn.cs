using System.Runtime.InteropServices;
using Extensibility;
using Microsoft.Office.Core;
using Microsoft.Office.Interop.OneNote;
using OneMarkDotNet.AddIn.Ribbon;
using OneMarkDotNet.OneNoteConverter;
using OneMarkDotNet.ThemeManager;

namespace OneMarkDotNet.AddIn;

[ComVisible(true)]
[Guid("B8F2E4A1-3D7C-4F9B-A5E6-8C1D2F3A4B5E")]
[ProgId("OneMarkDotNet.AddIn")]
[ClassInterface(ClassInterfaceType.AutoDispatch)]
public class OneMarkAddIn : IDTExtensibility2, IRibbonExtensibility
{
    private IApplication? _oneNoteApp;
    private OneMarkRibbon? _ribbon;
    private KeyboardHook? _keyboardHook;
    private MarkdownRenderHandler? _renderHandler;
    private ExportHandler? _exportHandler;
    private OneNoteEventHandler? _eventHandler;
    private OneMarkDotNet.ThemeManager.ThemeManager? _themeManager;
    private OneNoteApiWrapper? _apiWrapper;
    private AddInSettings? _settings;
    private AppLogger? _logger;

    public OneMarkAddIn()
    {
        DiagnosticLog("Instance constructor called");
    }

    static OneMarkAddIn()
    {
        DiagnosticLog("Static constructor called - .NET runtime loaded successfully");
    }

    private static void DiagnosticLog(string message)
    {
        try
        {
            var dir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OneMarkDotNet", "logs");
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
            _logger.LogInfo("OneMarkDotNet AddIn connecting...");
        }
        catch (Exception ex)
        {
            DiagnosticLog($"Logger init FAILED: {ex.GetType().Name}: {ex.Message}");
        }

        try
        {
            _settings = AddInSettings.Instance;
            _settings.LoadSettings();
            DiagnosticLog("Settings loaded");
        }
        catch (Exception ex)
        {
            DiagnosticLog($"Settings FAILED: {ex.GetType().Name}: {ex.Message}");
        }

        try
        {
            _themeManager = new OneMarkDotNet.ThemeManager.ThemeManager();
            var themesDir = _settings?.GetThemesDirectory();
            if (!string.IsNullOrEmpty(themesDir))
            {
                Directory.CreateDirectory(themesDir);
                _themeManager.LoadThemes(themesDir);
            }
            DiagnosticLog("ThemeManager initialized");
        }
        catch (Exception ex)
        {
            DiagnosticLog($"ThemeManager FAILED: {ex.GetType().Name}: {ex.Message}");
        }

        try
        {
            if (_oneNoteApp is not null)
            {
                _apiWrapper = new OneNoteApiWrapper(_oneNoteApp);
            }
            DiagnosticLog("ApiWrapper initialized");
        }
        catch (Exception ex)
        {
            DiagnosticLog($"ApiWrapper FAILED: {ex.GetType().Name}: {ex.Message}");
        }

        try
        {
            if (_apiWrapper is not null && _themeManager is not null && _settings is not null)
            {
                _renderHandler = new MarkdownRenderHandler(_apiWrapper, _themeManager, _settings);
                _exportHandler = new ExportHandler(_apiWrapper, _themeManager, _settings);
            }
            DiagnosticLog("Render/Export handlers initialized");
        }
        catch (Exception ex)
        {
            DiagnosticLog($"Handlers FAILED: {ex.GetType().Name}: {ex.Message}");
        }

        try
        {
            if (_themeManager is not null && _settings is not null)
            {
                _eventHandler = new OneNoteEventHandler(_themeManager, _settings);
                if (_oneNoteApp is not null)
                {
                    _eventHandler.Initialize(_oneNoteApp);
                }
            }
            DiagnosticLog("EventHandler initialized");
        }
        catch (Exception ex)
        {
            DiagnosticLog($"EventHandler FAILED: {ex.GetType().Name}: {ex.Message}");
        }

        try
        {
            if (_themeManager is not null)
            {
                _ribbon = new OneMarkRibbon(_themeManager);
            }
            DiagnosticLog("Ribbon initialized");
        }
        catch (Exception ex)
        {
            DiagnosticLog($"Ribbon FAILED: {ex.GetType().Name}: {ex.Message}");
        }

        try
        {
            _keyboardHook = new KeyboardHook();
            SubscribeKeyboardEvents();
            _keyboardHook.Install();
            DiagnosticLog("KeyboardHook installed");
        }
        catch (Exception ex)
        {
            DiagnosticLog($"KeyboardHook FAILED: {ex.GetType().Name}: {ex.Message}");
        }

        _logger?.LogInfo("OneMarkDotNet AddIn OnConnection completed");
        DiagnosticLog("OnConnection completed");
    }

    public void OnDisconnection(ext_DisconnectMode removeMode, ref Array custom)
    {
        try
        {
            _logger?.LogInfo("OneMarkDotNet AddIn disconnecting...");

            _keyboardHook?.Dispose();
            _keyboardHook = null;

            _renderHandler?.Dispose();
            _renderHandler = null;

            _eventHandler?.Dispose();
            _eventHandler = null;

            _apiWrapper?.Dispose();
            _apiWrapper = null;

            _settings?.SaveSettings();
            _logger?.LogInfo("OneMarkDotNet AddIn disconnected");
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
        _logger?.LogInfo("OneMarkDotNet AddIn startup complete");
    }

    public void OnBeginShutdown(ref Array custom)
    {
        _logger?.LogInfo("OneMarkDotNet AddIn beginning shutdown");
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
        try { _renderHandler?.HandleF5Render(); }
        catch (Exception ex) { _logger?.LogError("OnRenderMarkdown failed", ex); }
    }

    public void OnExportMarkdown(IRibbonControl control)
    {
        try { _exportHandler?.HandleF8Export(); }
        catch (Exception ex) { _logger?.LogError("OnExportMarkdown failed", ex); }
    }

    public void OnSourceModeToggle(IRibbonControl control)
    {
        try { _renderHandler?.HandleSourceModeToggle(); }
        catch (Exception ex) { _logger?.LogError("OnSourceModeToggle failed", ex); }
    }

    public void OnOpenThemeDirectory(IRibbonControl control)
    {
        try
        {
            var themesDir = _settings?.GetThemesDirectory();
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
            _themeManager?.ReloadThemes();
            _ribbon?.InvalidateControl("dynThemeMenu");
            _logger?.LogInfo("Themes reloaded");
        }
        catch (Exception ex) { _logger?.LogError("OnReloadThemes failed", ex); }
    }

    public void OnImportMarkdown(IRibbonControl control)
    {
        try { _exportHandler?.HandleImportFromFile().GetAwaiter().GetResult(); }
        catch (Exception ex) { _logger?.LogError("OnImportMarkdown failed", ex); }
    }

    public void OnExportMarkdownFile(IRibbonControl control)
    {
        try { _exportHandler?.HandleExportToFile().GetAwaiter().GetResult(); }
        catch (Exception ex) { _logger?.LogError("OnExportMarkdownFile failed", ex); }
    }

    public void OnAbout(IRibbonControl control)
    {
        try
        {
            System.Windows.Forms.MessageBox.Show(
                "OneMarkDotNet v1.0.0\n\nMarkdown rendering for OneNote\nhttps://github.com/onemarkdotnet",
                "About OneMarkDotNet",
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
            if (string.IsNullOrEmpty(themeName) || _settings is null) return;
            _settings.CurrentThemeName = themeName;
            _settings.SaveSettings();
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

    private void SubscribeKeyboardEvents()
    {
        if (_keyboardHook is null) return;

        _keyboardHook.EnterPressed += OnEnterPressed;
        _keyboardHook.CtrlEnterPressed += OnCtrlEnterPressed;
        _keyboardHook.CtrlCommaPressed += OnCtrlCommaPressed;
        _keyboardHook.F5Pressed += OnF5Pressed;
        _keyboardHook.F8Pressed += OnF8Pressed;
        _keyboardHook.TabPressed += OnTabPressed;
    }

    private void OnEnterPressed() { try { _renderHandler?.HandleEnterKey(); } catch { } }
    private void OnCtrlEnterPressed() { try { _renderHandler?.HandleCtrlEnter(); } catch { } }
    private void OnCtrlCommaPressed() { try { _renderHandler?.HandleSourceModeToggle(); } catch { } }
    private void OnF5Pressed() { try { _renderHandler?.HandleF5Render(); } catch { } }
    private void OnF8Pressed() { try { _exportHandler?.HandleF8Export(); } catch { } }
    private void OnTabPressed() { try { _renderHandler?.HandleTabRender(); } catch { } }
}
