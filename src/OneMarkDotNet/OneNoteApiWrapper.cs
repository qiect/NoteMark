namespace OneMarkDotNet
{
    using System;
    using System.Runtime.InteropServices;

    public class OneNoteApiWrapper : IDisposable
    {
        private dynamic onenoteApp;
        private bool disposed;

        public OneNoteApiWrapper()
        {
            disposed = false;
        }

        public void SetApplication(object application)
        {
            if (application != null)
            {
                onenoteApp = application;
            }
        }

        private dynamic GetApplication()
        {
            if (onenoteApp == null)
            {
                try
                {
                    var type = Type.GetTypeFromProgID("OneNote.Application");
                    if (type != null)
                    {
                        onenoteApp = Activator.CreateInstance(type);
                    }
                }
                catch (Exception)
                {
                }
            }

            return onenoteApp;
        }

        public string GetCurrentPageContent()
        {
            try
            {
                var app = GetApplication();
                if (app == null)
                {
                    return string.Empty;
                }

                var pageId = GetCurrentPageId();
                if (string.IsNullOrEmpty(pageId))
                {
                    return string.Empty;
                }

                string xml = null;
                app.GetPageContent(pageId, out xml, 1, 2013);
                return xml ?? string.Empty;
            }
            catch (Exception)
            {
                return string.Empty;
            }
        }

        public void UpdatePageContent(string xml)
        {
            try
            {
                var app = GetApplication();
                if (app == null || string.IsNullOrEmpty(xml))
                {
                    return;
                }

                app.UpdatePageContent(xml, DateTime.MinValue, 2013, true);
            }
            catch (Exception)
            {
            }
        }

        public string GetCurrentPageId()
        {
            try
            {
                var app = GetApplication();
                if (app == null)
                {
                    return string.Empty;
                }

                var windows = app.Windows;
                if (windows == null)
                {
                    return string.Empty;
                }

                try
                {
                    var currentWindow = windows.CurrentWindow;
                    if (currentWindow != null)
                    {
                        var pageId = currentWindow.CurrentPageId ?? string.Empty;

                        if (Marshal.IsComObject(currentWindow))
                        {
                            Marshal.ReleaseComObject(currentWindow);
                        }

                        return pageId;
                    }
                }
                finally
                {
                    if (Marshal.IsComObject(windows))
                    {
                        Marshal.ReleaseComObject(windows);
                    }
                }
            }
            catch (Exception)
            {
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
                // Do NOT release the COM object if it was passed from OnConnection
                // OneNote owns it and will release it
                onenoteApp = null;
                disposed = true;
            }
        }

        ~OneNoteApiWrapper()
        {
            Dispose(false);
        }
    }

    public class HierarchyInfo
    {
        public string CurrentNotebookId { get; set; }
        public string CurrentSectionId { get; set; }
        public string CurrentPageId { get; set; }

        public HierarchyInfo()
        {
            CurrentNotebookId = string.Empty;
            CurrentSectionId = string.Empty;
            CurrentPageId = string.Empty;
        }
    }
}
