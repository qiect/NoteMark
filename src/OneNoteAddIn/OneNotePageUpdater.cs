using System.Xml.Linq;
using OneMarkDotNet.MarkdownEngine;
using OneMarkDotNet.OneNoteConverter;
using OneMarkDotNet.ThemeManager;

namespace OneMarkDotNet.AddIn;

public sealed class OneNotePageUpdater : IDisposable
{
    private const string OneNoteNamespace = "http://schemas.microsoft.com/office/onenote/2013/onenote";

    private readonly OneNoteApiWrapper _api;
    private readonly OneNoteXmlConverter _converter = new();
    private readonly OneNoteXmlBuilder _builder = new();
    private bool _disposed;

    public OneNotePageUpdater(OneNoteApiWrapper api)
    {
        _api = api;
    }

    public void UpdatePageContent(string pageId, string xml)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(OneNotePageUpdater));
        _api.UpdatePageContent(xml, DateTime.Now);
    }

    public string GetPageContent(string pageId)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(OneNotePageUpdater));
        _api.GetPageContent(pageId, out var xml);
        return xml;
    }

    public string GetSelectedText()
    {
        if (_disposed) throw new ObjectDisposedException(nameof(OneNotePageUpdater));
        _api.GetHierarchy(string.Empty, Microsoft.Office.Interop.OneNote.HierarchyScope.hsPages, out var hierarchyXml);

        var doc = XDocument.Parse(hierarchyXml);
        var ns = XNamespace.Get(OneNoteNamespace);

        var currentPage = doc.Descendants(ns + "Page")
            .FirstOrDefault(p => p.Attribute("isCurrentlyViewed")?.Value == "true");

        if (currentPage is null) return string.Empty;

        var pageId = currentPage.Attribute("ID")?.Value;
        if (pageId is null) return string.Empty;

        _api.GetPageContent(pageId, out var pageXml);
        var pageDoc = XDocument.Parse(pageXml);

        var selected = pageDoc.Descendants(ns + "T")
            .Where(t => t.Attribute("selected")?.Value == "all")
            .Select(t =>
            {
                var cdata = t.Nodes().OfType<XCData>().FirstOrDefault();
                return cdata?.Value ?? t.Value;
            });

        return string.Join(Environment.NewLine, selected);
    }

    public void ReplaceSelectedText(string markdown, Theme theme)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(OneNotePageUpdater));
        _api.GetHierarchy(string.Empty, Microsoft.Office.Interop.OneNote.HierarchyScope.hsPages, out var hierarchyXml);

        var doc = XDocument.Parse(hierarchyXml);
        var ns = XNamespace.Get(OneNoteNamespace);

        var currentPage = doc.Descendants(ns + "Page")
            .FirstOrDefault(p => p.Attribute("isCurrentlyViewed")?.Value == "true");

        if (currentPage is null) return;

        var pageId = currentPage.Attribute("ID")?.Value;
        if (pageId is null) return;

        _api.GetPageContent(pageId, out var pageXml);
        var pageDoc = XDocument.Parse(pageXml);

        var document = MarkdownDocument.Parse(markdown);
        var html = new MarkdownParser().ParseToHtml(document.RawContent);
        var outlineElements = _converter.ConvertToOneNoteElements(document, theme);

        var selectedTElements = pageDoc.Descendants(ns + "T")
            .Where(t => t.Attribute("selected")?.Value == "all")
            .ToList();

        if (selectedTElements.Count == 0) return;

        var firstSelected = selectedTElements[0];
        var parent = firstSelected.Parent;
        if (parent is null) return;

        var cdata = new XCData(html);
        firstSelected.Nodes().Remove();
        firstSelected.Add(cdata);

        for (var i = 1; i < selectedTElements.Count; i++)
        {
            selectedTElements[i].Remove();
        }

        _api.UpdatePageContent(pageDoc.ToString(), DateTime.Now);
    }

    public void AppendContentToPage(string pageId, string markdown, Theme theme)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(OneNotePageUpdater));

        _api.GetPageContent(pageId, out var pageXml);
        var pageDoc = XDocument.Parse(pageXml);
        var ns = XNamespace.Get(OneNoteNamespace);

        var document = MarkdownDocument.Parse(markdown);
        var elements = _converter.ConvertToOneNoteElements(document, theme);

        var outline = pageDoc.Descendants(ns + "Outline").FirstOrDefault();
        if (outline is null)
        {
            var newOutlineXml = _builder.BuildOutline(elements);
            var newOutline = XElement.Parse(newOutlineXml);
            pageDoc.Root?.Add(newOutline);
        }
        else
        {
            var oeChildren = outline.Element(ns + "OEChildren");
            if (oeChildren is null)
            {
                oeChildren = new XElement(ns + "OEChildren");
                outline.Add(oeChildren);
            }

            foreach (var element in elements)
            {
                var oe = _builder.BuildOE(element);
                oeChildren.Add(oe);
            }
        }

        _api.UpdatePageContent(pageDoc.ToString(), DateTime.Now);
    }

    public void ReplacePageContent(string pageId, string markdown, Theme theme)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(OneNotePageUpdater));

        var document = MarkdownDocument.Parse(markdown);
        var xml = _converter.ConvertToOneNoteXml(document, theme);

        _api.UpdatePageContent(xml, DateTime.Now);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
    }
}
