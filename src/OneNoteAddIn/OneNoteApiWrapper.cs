using System.Runtime.InteropServices;
using Microsoft.Office.Interop.OneNote;

namespace OneMarkDotNet.AddIn;

public sealed class OneNoteApiWrapper : IDisposable
{
    private IApplication? _app;
    private readonly bool _ownsApp;
    private bool _disposed;

    public OneNoteApiWrapper()
    {
        _app = new Microsoft.Office.Interop.OneNote.Application();
        _ownsApp = true;
    }

    public OneNoteApiWrapper(IApplication application)
    {
        _app = application;
        _ownsApp = false;
    }

    public void GetPageContent(string pageId, out string xml)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(OneNoteApiWrapper));
        _app!.GetPageContent(pageId, out xml, PageInfo.piAll);
    }

    public void UpdatePageContent(string xml, DateTime lastModified)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(OneNoteApiWrapper));
        _app!.UpdatePageContent(xml, lastModified);
    }

    public void GetHierarchy(string parentId, HierarchyScope scope, out string xml)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(OneNoteApiWrapper));
        _app!.GetHierarchy(parentId, scope, out xml);
    }

    public void FindPages(string notebookId, string searchQuery, out string xml)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(OneNoteApiWrapper));
        _app!.FindPages(notebookId, searchQuery, out xml);
    }

    public void CreateNewPage(string sectionId, out string pageId)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(OneNoteApiWrapper));
        _app!.CreateNewPage(sectionId, out pageId, NewPageStyle.npsDefault);
    }

    public void DeletePageContent(string pageId, string objectId)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(OneNoteApiWrapper));
        _app!.DeletePageContent(pageId, objectId);
    }

    public void NavigateTo(string bstrHierarchyID, string bstrObjectID = "", bool fNewWindow = false)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(OneNoteApiWrapper));
        _app!.NavigateTo(bstrHierarchyID, bstrObjectID, fNewWindow);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_app is null) return;

        if (_ownsApp)
        {
            try
            {
                Marshal.ReleaseComObject(_app);
            }
            catch (COMException)
            {
            }
        }

        _app = null;
    }
}
