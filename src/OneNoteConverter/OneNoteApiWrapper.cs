using System.Runtime.InteropServices;
using Microsoft.Office.Interop.OneNote;

namespace OneMarkDotNet.OneNoteConverter;

public sealed class OneNoteApiWrapper : IDisposable
{
    private IApplication? _app;
    private bool _disposed;

    public OneNoteApiWrapper()
    {
        _app = new Microsoft.Office.Interop.OneNote.Application();
    }

    public void GetPageContent(string pageId, out string xml)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _app!.GetPageContent(pageId, out xml, PageInfo.piAll);
    }

    public void UpdatePageContent(string xml, DateTime lastModified)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _app!.UpdatePageContent(xml, lastModified);
    }

    public void GetHierarchy(string parentId, HierarchyScope scope, out string xml)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _app!.GetHierarchy(parentId, scope, out xml);
    }

    public void FindPages(string notebookId, string searchQuery, out string xml)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _app!.FindPages(notebookId, searchQuery, out xml);
    }

    public void CreateNewPage(string sectionId, out string pageId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _app!.CreateNewPage(sectionId, out pageId, NewPageStyle.npsDefault);
    }

    public void DeletePageContent(string pageId, string objectId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _app!.DeletePageContent(pageId, objectId);
    }

    public void NavigateTo(string bstrHierarchyID, string bstrObjectID = "", bool fNewWindow = false)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _app!.NavigateTo(bstrHierarchyID, bstrObjectID, fNewWindow);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_app is null) return;

        try
        {
            Marshal.ReleaseComObject(_app);
        }
        catch (COMException)
        {
        }
        finally
        {
            _app = null;
        }
    }
}
