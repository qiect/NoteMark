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
public sealed class OneMarkAddIn : IDTExtensibility2, IRibbonExtensibility
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
#pragma warning disable CS0169
    private bool _disposed;
#pragma warning restore CS0169

    public void OnConnection(object application, ext_ConnectMode connectMode, object addInInst, ref Array custom)
    {
        try
        {
            _oneNoteApp = (IApplication)application;
            _logger = AppLogger.Instance;
            _logger.LogInfo("OneMarkDotNet AddIn connecting...");

            _settings = AddInSettings.Instance;
            _settings.LoadSettings();

            _themeManager = new OneMarkDotNet.ThemeManager.ThemeManager();
            var themesDir = _settings.GetThemesDirectory();
            if (!string.IsNullOrEmpty(themesDir))
            {
                Directory.CreateDirectory(themesDir);
                _themeManager.LoadThemes(themesDir);
            }

            _apiWrapper = new OneNoteApiWrapper(_oneNoteApp);
            _renderHandler = new MarkdownRenderHandler(_apiWrapper, _themeManager, _settings);
            _exportHandler = new ExportHandler(_apiWrapper, _themeManager, _settings);

            _eventHandler = new OneNoteEventHandler(_themeManager, _settings);
            _eventHandler.Initialize(_oneNoteApp);

            _ribbon = new OneMarkRibbon(_themeManager);

            _keyboardHook = new KeyboardHook();
            SubscribeKeyboardEvents();
            _keyboardHook.Install();

            _logger.LogInfo("OneMarkDotNet AddIn connected successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError("OnConnection failed", ex);
        }
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

            if (_oneNoteApp is not null)
            {
                Marshal.ReleaseComObject(_oneNoteApp);
                _oneNoteApp = null;
            }

            _settings?.SaveSettings();
            _logger?.LogInfo("OneMarkDotNet AddIn disconnected");
        }
        catch (Exception ex)
        {
            _logger?.LogError("OnDisconnection failed", ex);
        }
    }

    public void OnAddInsUpdate(ref Array custom)
    {
    }

    public void OnStartupComplete(ref Array custom)
    {
        _logger?.LogInfo("OneMarkDotNet AddIn startup complete");
    }

    public void OnBeginShutdown(ref Array custom)
    {
        _logger?.LogInfo("OneMarkDotNet AddIn beginning shutdown");
    }

    public string GetCustomUI(string ribbonId)
    {
        return _ribbon?.GetCustomUI(ribbonId) ?? string.Empty;
    }

    public void RibbonOnLoad(IRibbonUI ribbonUi)
    {
        _ribbon?.RibbonOnLoad(ribbonUi);
    }

    public void OnRenderMarkdown(IRibbonControl control)
    {
        _renderHandler?.HandleF5Render();
    }

    public void OnExportMarkdown(IRibbonControl control)
    {
        _exportHandler?.HandleF8Export();
    }

    public void OnSourceModeToggle(IRibbonControl control)
    {
        _renderHandler?.HandleSourceModeToggle();
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
        catch (Exception ex)
        {
            _logger?.LogError("Failed to open theme directory", ex);
        }
    }

    public void OnReloadThemes(IRibbonControl control)
    {
        _themeManager?.ReloadThemes();
        _ribbon?.InvalidateControl("dynThemeMenu");
        _logger?.LogInfo("Themes reloaded");
    }

    public void OnImportMarkdown(IRibbonControl control)
    {
        _exportHandler?.HandleImportFromFile().GetAwaiter().GetResult();
    }

    public void OnExportMarkdownFile(IRibbonControl control)
    {
        _exportHandler?.HandleExportToFile().GetAwaiter().GetResult();
    }

    public void OnAbout(IRibbonControl control)
    {
        System.Windows.Forms.MessageBox.Show(
            "OneMarkDotNet v1.0.0\n\nMarkdown rendering for OneNote\nhttps://github.com/onemarkdotnet",
            "About OneMarkDotNet",
            System.Windows.Forms.MessageBoxButtons.OK,
            System.Windows.Forms.MessageBoxIcon.Information);
    }

    public void OnThemeSelected(IRibbonControl control)
    {
        var themeName = control.Tag;
        if (string.IsNullOrEmpty(themeName) || _settings is null) return;
        _settings.CurrentThemeName = themeName;
        _settings.SaveSettings();
        _ribbon?.InvalidateControl("dynThemeMenu");
        _logger?.LogInfo($"Theme changed to: {themeName}");
    }

    public string GetThemeMenuContent(IRibbonControl control)
    {
        return _ribbon?.GetThemeMenuContent(control) ?? string.Empty;
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

    private void OnEnterPressed()
    {
        _renderHandler?.HandleEnterKey();
    }

    private void OnCtrlEnterPressed()
    {
        _renderHandler?.HandleCtrlEnter();
    }

    private void OnCtrlCommaPressed()
    {
        _renderHandler?.HandleSourceModeToggle();
    }

    private void OnF5Pressed()
    {
        _renderHandler?.HandleF5Render();
    }

    private void OnF8Pressed()
    {
        _exportHandler?.HandleF8Export();
    }

    private void OnTabPressed()
    {
        _renderHandler?.HandleTabRender();
    }
}
