namespace OneMarkDotNet
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;
    using Extensibility;
    using Microsoft.Office.Core;

    [ComVisible(true)]
    [Guid("B2C3D4E5-F6A7-8901-BCDE-F12345678901")]
    [ProgId("OneMarkDotNet.AddIn")]
    public class OneMarkAddIn : IDTExtensibility2, IRibbonExtensibility
    {
        private AppLogger logger;
        private MarkdownRenderHandler renderHandler;
        private ExportHandler exportHandler;
        private KeyboardHook keyboardHook;
        private ThemeManager themeManager;
        private OneNoteApiWrapper apiWrapper;
        private IRibbonUI ribbonUI;

        private static OneMarkAddIn self;

        public static OneMarkAddIn Self
        {
            get { return self; }
        }

        public void OnConnection(object application, ext_ConnectMode connectMode, object addInInst, ref Array custom)
        {
            self = this;

            try
            {
                logger = AppLogger.Instance;
                logger.Info("OneMarkAddIn: OnConnection called, mode=" + connectMode.ToString());

                // Use the OneNote application object passed by the host
                apiWrapper = new OneNoteApiWrapper();
                apiWrapper.SetApplication(application);
            }
            catch (Exception ex)
            {
                Debug.WriteLine("OneMarkAddIn OnConnection error: " + ex.Message);
            }
        }

        public void OnDisconnection(ext_DisconnectMode removeMode, ref Array custom)
        {
            try
            {
                if (logger != null)
                {
                    logger.Info("OneMarkAddIn: OnDisconnection called");
                }

                if (keyboardHook != null)
                {
                    keyboardHook.RenderRequested -= OnKeyboardRenderRequested;
                    keyboardHook.ExportRequested -= OnKeyboardExportRequested;
                    keyboardHook.SourceModeRequested -= OnKeyboardSourceModeRequested;
                    keyboardHook.Dispose();
                    keyboardHook = null;
                }

                if (apiWrapper != null)
                {
                    apiWrapper.Dispose();
                    apiWrapper = null;
                }

                renderHandler = null;
                exportHandler = null;
                themeManager = null;
                self = null;
            }
            catch (Exception ex)
            {
                Debug.WriteLine("OneMarkAddIn OnDisconnection error: " + ex.Message);
            }
        }

        public void OnAddInsUpdate(ref Array custom)
        {
        }

        public void OnStartupComplete(ref Array custom)
        {
            try
            {
                if (logger != null)
                {
                    logger.Info("OneMarkAddIn: OnStartupComplete called");
                }

                // Use the apiWrapper created in OnConnection
                renderHandler = new MarkdownRenderHandler(apiWrapper, ThemeManager.Instance);
                exportHandler = new ExportHandler(apiWrapper);

                // Install keyboard hook (lightweight)
                keyboardHook = new KeyboardHook();
                keyboardHook.RenderRequested += OnKeyboardRenderRequested;
                keyboardHook.ExportRequested += OnKeyboardExportRequested;
                keyboardHook.SourceModeRequested += OnKeyboardSourceModeRequested;

                // Defer heavy initialization (theme loading, settings) to avoid blocking OneNote startup
                System.Threading.ThreadPool.QueueUserWorkItem(delegate
                {
                    try
                    {
                        themeManager = ThemeManager.Instance;

                        var settings = AddInSettings.Instance;
                        themeManager.CurrentThemeName = settings.CurrentThemeName;

                        if (logger != null)
                        {
                            logger.Info("OneMarkAddIn: deferred initialization complete");
                        }
                    }
                    catch (Exception ex)
                    {
                        if (logger != null)
                        {
                            logger.Error("OneMarkAddIn: deferred initialization failed", ex);
                        }
                    }
                });

                if (logger != null)
                {
                    logger.Info("OneMarkAddIn: OnStartupComplete finished");
                }
            }
            catch (Exception ex)
            {
                if (logger != null)
                {
                    logger.Error("OneMarkAddIn: OnStartupComplete failed", ex);
                }
                else
                {
                    Debug.WriteLine("OneMarkAddIn OnStartupComplete error: " + ex.Message);
                }
            }
        }

        public void OnBeginShutdown(ref Array custom)
        {
            try
            {
                if (logger != null)
                {
                    logger.Info("OneMarkAddIn: OnBeginShutdown called");
                }

                if (keyboardHook != null)
                {
                    keyboardHook.RenderRequested -= OnKeyboardRenderRequested;
                    keyboardHook.ExportRequested -= OnKeyboardExportRequested;
                    keyboardHook.SourceModeRequested -= OnKeyboardSourceModeRequested;
                    keyboardHook.Dispose();
                    keyboardHook = null;
                }

                try
                {
                    var settings = AddInSettings.Instance;
                    settings.Save();
                }
                catch { }
            }
            catch (Exception ex)
            {
                if (logger != null)
                {
                    logger.Error("OneMarkAddIn: OnBeginShutdown failed", ex);
                }
            }
        }

        public string GetCustomUI(string ribbonID)
        {
            try
            {
                if (logger != null)
                {
                    logger.Info("GetCustomUI called with ribbonID: " + (ribbonID ?? "null"));
                }

                var xml = OneMarkRibbon.LoadRibbonXml();

                if (logger != null)
                {
                    logger.Info("GetCustomUI returned XML length: " + (xml != null ? xml.Length.ToString() : "null"));
                }

                return xml;
            }
            catch (Exception ex)
            {
                if (logger != null)
                {
                    logger.Error("GetCustomUI failed", ex);
                }

                return string.Empty;
            }
        }

        public void OnRibbonLoad(IRibbonUI ribbonUI)
        {
            this.ribbonUI = ribbonUI;
            if (logger != null)
            {
                logger.Info("OnRibbonLoad called");
            }
        }

        public void OnRenderClick(IRibbonControl control)
        {
            try
            {
                if (renderHandler != null)
                {
                    renderHandler.HandleRenderSelection();
                }
            }
            catch (Exception ex)
            {
                if (logger != null)
                {
                    logger.Error("OnRenderClick failed", ex);
                }
            }
        }

        public void OnExportClick(IRibbonControl control)
        {
            try
            {
                if (exportHandler != null)
                {
                    exportHandler.HandleExportToClipboard();
                }
            }
            catch (Exception ex)
            {
                if (logger != null)
                {
                    logger.Error("OnExportClick failed", ex);
                }
            }
        }

        public void OnSourceModeClick(IRibbonControl control)
        {
            try
            {
                if (renderHandler != null)
                {
                    renderHandler.HandleSourceModeToggle();
                }
            }
            catch (Exception ex)
            {
                if (logger != null)
                {
                    logger.Error("OnSourceModeClick failed", ex);
                }
            }
        }

        public void OnImportClick(IRibbonControl control)
        {
            try
            {
                if (exportHandler != null)
                {
                    exportHandler.HandleImportFromFile();
                }
            }
            catch (Exception ex)
            {
                if (logger != null)
                {
                    logger.Error("OnImportClick failed", ex);
                }
            }
        }

        public void OnExportFileClick(IRibbonControl control)
        {
            try
            {
                if (exportHandler != null)
                {
                    exportHandler.HandleExportToFile();
                }
            }
            catch (Exception ex)
            {
                if (logger != null)
                {
                    logger.Error("OnExportFileClick failed", ex);
                }
            }
        }

        public void OnAboutClick(IRibbonControl control)
        {
            try
            {
                var version = GetType().Assembly.GetName().Version.ToString();
                var message = string.Format(
                    "OneMarkDotNet - Markdown for OneNote\nVersion: {0}\n\nA OneNote add-in for rendering Markdown content.",
                    version);
                MessageBox.Show(message, "About OneMarkDotNet", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                if (logger != null)
                {
                    logger.Error("OnAboutClick failed", ex);
                }
            }
        }

        public void OnThemeMenuClick(IRibbonControl control)
        {
            try
            {
                var themeName = control.Tag as string;
                if (string.IsNullOrEmpty(themeName))
                {
                    return;
                }

                var settings = AddInSettings.Instance;
                settings.CurrentThemeName = themeName;
                settings.Save();

                if (themeManager != null)
                {
                    themeManager.CurrentThemeName = themeName;
                }

                if (logger != null)
                {
                    logger.Info(string.Format("Theme changed to: {0}", themeName));
                }
            }
            catch (Exception ex)
            {
                if (logger != null)
                {
                    logger.Error("OnThemeMenuClick failed", ex);
                }
            }
        }

        public void OnOpenThemeDirClick(IRibbonControl control)
        {
            try
            {
                var themeDir = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "OneMarkDotNet", "themes");

                if (!Directory.Exists(themeDir))
                {
                    Directory.CreateDirectory(themeDir);
                }

                Process.Start(themeDir);
            }
            catch (Exception ex)
            {
                if (logger != null)
                {
                    logger.Error("OnOpenThemeDirClick failed", ex);
                }
            }
        }

        public void OnReloadThemesClick(IRibbonControl control)
        {
            try
            {
                if (themeManager != null)
                {
                    themeManager.ReloadThemes();
                }

                if (logger != null)
                {
                    logger.Info("Themes reloaded");
                }
            }
            catch (Exception ex)
            {
                if (logger != null)
                {
                    logger.Error("OnReloadThemesClick failed", ex);
                }
            }
        }

        public string GetThemeMenuContent(IRibbonControl control)
        {
            try
            {
                if (themeManager != null)
                {
                    var themes = themeManager.GetThemes();
                    return OneMarkRibbon.GetThemeMenuXml(themes);
                }
            }
            catch (Exception ex)
            {
                if (logger != null)
                {
                    logger.Error("GetThemeMenuContent failed", ex);
                }
            }

            return "<menu xmlns=\"http://schemas.microsoft.com/office/2009/07/customui\">" +
                   "<button id=\"NoThemesLabel\" label=\"(No themes available)\" enabled=\"false\" />" +
                   "</menu>";
        }

        private void OnKeyboardRenderRequested(object sender, EventArgs e)
        {
            try
            {
                if (renderHandler != null)
                {
                    renderHandler.HandleRenderSelection();
                }
            }
            catch (Exception ex)
            {
                if (logger != null)
                {
                    logger.Error("Keyboard RenderRequested handler failed", ex);
                }
            }
        }

        private void OnKeyboardExportRequested(object sender, EventArgs e)
        {
            try
            {
                if (exportHandler != null)
                {
                    exportHandler.HandleExportToClipboard();
                }
            }
            catch (Exception ex)
            {
                if (logger != null)
                {
                    logger.Error("Keyboard ExportRequested handler failed", ex);
                }
            }
        }

        private void OnKeyboardSourceModeRequested(object sender, EventArgs e)
        {
            try
            {
                if (renderHandler != null)
                {
                    renderHandler.HandleSourceModeToggle();
                }
            }
            catch (Exception ex)
            {
                if (logger != null)
                {
                    logger.Error("Keyboard SourceModeRequested handler failed", ex);
                }
            }
        }
    }
}
