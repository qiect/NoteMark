namespace NoteMark
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.Office.Interop.OneNote;

    public class OneNoteApiWrapper : IDisposable
    {
        private IApplication onenoteApp;
        private bool disposed;
        private readonly AppLogger logger;

        public OneNoteApiWrapper()
        {
            disposed = false;
            logger = AppLogger.Instance;
            CreateApplication();
        }

        private void CreateApplication()
        {
            // Follow OneMore's approach: create a standalone Application instance
            // This is more reliable than using the COM object from OnConnection
            try
            {
                onenoteApp = new Application();
                logger.Info("CreateApplication: successfully created IApplication instance");
            }
            catch (COMException ex)
            {
                int retries = 0;
                while (retries < 3)
                {
                    try
                    {
                        System.Threading.Thread.Sleep(250 * (retries + 1));
                        onenoteApp = new Application();
                        logger.Info(string.Format("CreateApplication: success after {0} retries", retries + 1));
                        return;
                    }
                    catch (COMException)
                    {
                        retries++;
                    }
                }
                logger.Error(string.Format("CreateApplication: failed after {0} retries", retries), ex);
            }
            catch (Exception ex)
            {
                logger.Error("CreateApplication: failed to create IApplication", ex);
            }
        }

        public void SetApplication(object application)
        {
            // Keep for interface compatibility but we use our own Application instance
            logger.Info("SetApplication: called (using standalone Application instance instead)");
        }

        public string GetCurrentPageContent()
        {
            try
            {
                if (onenoteApp == null)
                {
                    logger.Warning("GetCurrentPageContent: no application available");
                    return string.Empty;
                }

                var pageId = GetCurrentPageId();
                if (string.IsNullOrEmpty(pageId))
                {
                    logger.Warning("GetCurrentPageContent: no current page ID");
                    return string.Empty;
                }

                logger.Info(string.Format("GetCurrentPageContent: pageId={0}", pageId));

                string xml = null;
                onenoteApp.GetPageContent(pageId, out xml, PageInfo.piAll, XMLSchema.xs2013);

                if (string.IsNullOrEmpty(xml))
                {
                    logger.Warning("GetCurrentPageContent: returned empty XML");
                    return string.Empty;
                }

                logger.Info(string.Format("GetCurrentPageContent: got XML, length={0}", xml.Length));
                return xml;
            }
            catch (COMException ex)
            {
                logger.Error(string.Format("GetCurrentPageContent COM error: 0x{0:X}", ex.ErrorCode), ex);
                return string.Empty;
            }
            catch (Exception ex)
            {
                logger.Error("GetCurrentPageContent failed", ex);
                return string.Empty;
            }
        }

        public void UpdatePageContent(string xml)
        {
            try
            {
                if (onenoteApp == null || string.IsNullOrEmpty(xml))
                {
                    logger.Warning("UpdatePageContent: no application or empty XML");
                    return;
                }

                logger.Info(string.Format("UpdatePageContent: XML length={0}", xml.Length));
                onenoteApp.UpdatePageContent(xml, DateTime.MinValue, XMLSchema.xs2013, true);
                logger.Info("UpdatePageContent: success");
            }
            catch (COMException ex)
            {
                logger.Error(string.Format("UpdatePageContent COM error: 0x{0:X}", ex.ErrorCode), ex);
            }
            catch (Exception ex)
            {
                logger.Error("UpdatePageContent failed", ex);
            }
        }

        public string GetCurrentPageId()
        {
            try
            {
                if (onenoteApp == null)
                {
                    return string.Empty;
                }

                // Follow OneMore's WithCurrentWindow pattern
                var windows = onenoteApp.Windows;
                try
                {
                    var window = windows.CurrentWindow;
                    try
                    {
                        if (window != null)
                        {
                            var pageId = window.CurrentPageId;
                            if (!string.IsNullOrEmpty(pageId))
                            {
                                return pageId;
                            }
                        }
                    }
                    finally
                    {
                        if (window != null && Marshal.IsComObject(window))
                        {
                            Marshal.ReleaseComObject(window);
                        }
                    }
                }
                catch (COMException ex)
                {
                    logger.Error(string.Format("GetCurrentPageId COM error reading Window: 0x{0:X}", ex.ErrorCode), ex);
                }
                finally
                {
                    if (windows != null && Marshal.IsComObject(windows))
                    {
                        Marshal.ReleaseComObject(windows);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error("GetCurrentPageId failed", ex);
            }

            return string.Empty;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                // Do NOT release the COM object - OneNote owns it
                // Following OneMore's pattern: GC.SuppressFinalize is NOT called
                onenoteApp = null;
                disposed = true;
            }
        }

        ~OneNoteApiWrapper()
        {
            Dispose(false);
        }
    }
}
