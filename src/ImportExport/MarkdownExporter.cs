using System.Text;
using System.Xml.Linq;
using Markdig;
using OneMarkDotNet.MarkdownEngine;

namespace OneMarkDotNet.ImportExport;

public sealed class MarkdownExporter
{
    readonly FrontMatterParser _frontMatterParser = new();
    readonly ImageHandler _imageHandler = new();
    readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseYamlFrontMatter()
        .UseAdvancedExtensions()
        .Build();

    public async Task ExportToFile(MarkdownDocument document, string filePath)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(filePath));
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);

        var content = document.RawContent;
        var assetsDir = Path.Combine(
            Path.GetDirectoryName(Path.GetFullPath(filePath)) ?? string.Empty,
            Path.GetFileNameWithoutExtension(filePath) + "_assets");
        Directory.CreateDirectory(assetsDir);

        var (processedContent, _) = await _imageHandler.ConvertBase64ToLocalFiles(content, assetsDir);

        var sb = new StringBuilder();

        if (document.FrontMatter.Count > 0)
        {
            var frontMatter = BuildFrontMatter(document);
            sb.AppendLine(frontMatter);
        }

        sb.Append(processedContent);

        File.WriteAllText(filePath, sb.ToString());
    }

    public void ExportToClipboard(MarkdownDocument document)
    {
        var content = BuildFullMarkdown(document);
        ClipboardHelper.SetText(content);
    }

    public async Task<string> ExportFromOneNotePage(string pageId)
    {
        var pageXml = await FetchOneNotePageXml(pageId);
        var document = ConvertOneNotePageToDocument(pageXml);
        return BuildFullMarkdown(document);
    }

    public async Task ExportFromOneNotePageToFile(string pageId, string filePath)
    {
        var markdown = await ExportFromOneNotePage(pageId);
        var document = new MarkdownDocument { RawContent = markdown };
        await ExportToFile(document, filePath);
    }

    string BuildFullMarkdown(MarkdownDocument document)
    {
        var sb = new StringBuilder();

        if (document.FrontMatter.Count > 0)
        {
            var frontMatter = BuildFrontMatter(document);
            sb.AppendLine(frontMatter);
        }

        sb.Append(document.RawContent);
        return sb.ToString();
    }

    string BuildFrontMatter(MarkdownDocument document)
    {
        var fm = new Dictionary<string, object>();

        if (!string.IsNullOrEmpty(document.Title))
            fm["title"] = document.Title;

        if (document.CreatedDate.HasValue)
            fm["date"] = document.CreatedDate.Value;

        if (!string.IsNullOrEmpty(document.Author))
            fm["author"] = document.Author;

        if (document.Tags.Count > 0)
            fm["tags"] = document.Tags;

        foreach (var kv in document.FrontMatter)
        {
            if (!fm.ContainsKey(kv.Key))
                fm[kv.Key] = kv.Value;
        }

        return _frontMatterParser.Serialize(fm);
    }

    async Task<string> FetchOneNotePageXml(string pageId)
    {
        await Task.CompletedTask;
        throw new NotImplementedException(
            "OneNote page fetching requires the OneNote Interop API. " +
            "Implement this method with Microsoft.Office.Interop.OneNote integration.");
    }

    MarkdownDocument ConvertOneNotePageToDocument(string xml)
    {
        var document = new MarkdownDocument();

        try
        {
            var xdoc = XDocument.Parse(xml);
            var ns = xdoc.Root?.GetDefaultNamespace() ?? XNamespace.None;

            var titleEl = xdoc.Descendants(ns + "Title").FirstOrDefault();
            if (titleEl != null)
                document.Title = titleEl.Value.Trim();

            var oneImages = _imageHandler.ExtractOneNoteImagesFromXml(xml);
            document.Images.AddRange(oneImages);

            var body = ConvertOneNoteXmlToMarkdown(xdoc, ns);

            foreach (var img in oneImages)
            {
                var placeholder = $"![{img.AltText ?? "image"}](data:{img.MimeType};base64,{img.Base64Data})";
                body = body.Replace($"{{{{image-{document.Images.IndexOf(img)}}}}}", placeholder);
            }

            document.RawContent = body;
        }
        catch (Exception)
        {
            document.RawContent = xml;
        }

        return document;
    }

    string ConvertOneNoteXmlToMarkdown(XDocument xdoc, XNamespace ns)
    {
        var sb = new StringBuilder();
        var body = xdoc.Descendants(ns + "Outline").FirstOrDefault();

        if (body == null)
            return string.Empty;

        ProcessOneNoteElement(body, ns, sb, 0);

        return sb.ToString();
    }

    void ProcessOneNoteElement(XElement element, XNamespace ns, StringBuilder sb, int depth)
    {
        foreach (var child in element.Elements())
        {
            var localName = child.Name.LocalName;

            switch (localName)
            {
                case "OEChildren":
                    ProcessOneNoteElement(child, ns, sb, depth);
                    break;
                case "OE":
                    ProcessOneNoteElement(child, ns, sb, depth);
                    sb.AppendLine();
                    break;
                case "T":
                    var text = child.Value;
                    var style = child.Attribute("style")?.Value;
                    text = ApplyStyleToText(text, style);
                    sb.Append(text);
                    break;
                case "Bullet":
                    sb.Append($"{new string(' ', depth * 2)}- ");
                    ProcessOneNoteElement(child, ns, sb, depth + 1);
                    break;
                case "Numbering":
                    sb.Append($"{new string(' ', depth * 2)}1. ");
                    ProcessOneNoteElement(child, ns, sb, depth + 1);
                    break;
                case "Image":
                    var imgIndex = sb.Length;
                    sb.Append($"![image]({{{{image-{imgIndex}}}}})");
                    break;
                case "Table":
                    ProcessOneNoteTable(child, ns, sb);
                    break;
                default:
                    ProcessOneNoteElement(child, ns, sb, depth);
                    break;
            }
        }
    }

    void ProcessOneNoteTable(XElement tableEl, XNamespace ns, StringBuilder sb)
    {
        var rows = tableEl.Elements(ns + "Row").ToList();
        if (rows.Count == 0) return;

        var firstRow = rows[0];
        var cells = firstRow.Elements(ns + "Cell").ToList();
        var colCount = cells.Count;

        sb.Append("| ");
        for (var i = 0; i < colCount; i++)
        {
            var cellText = cells[i].Value.Trim().Replace("\n", " ");
            sb.Append(cellText);
            sb.Append(" |");
        }

        sb.AppendLine();
        sb.Append("| ");
        for (var i = 0; i < colCount; i++)
        {
            sb.Append("--- |");
        }

        sb.AppendLine();

        for (var r = 1; r < rows.Count; r++)
        {
            var rowCells = rows[r].Elements(ns + "Cell").ToList();
            sb.Append("| ");
            for (var i = 0; i < Math.Min(rowCells.Count, colCount); i++)
            {
                var cellText = rowCells[i].Value.Trim().Replace("\n", " ");
                sb.Append(cellText);
                sb.Append(" |");
            }

            sb.AppendLine();
        }
    }

    static string ApplyStyleToText(string text, string? style)
    {
        if (string.IsNullOrEmpty(style))
            return text;

        if (style.IndexOf("bold", StringComparison.OrdinalIgnoreCase) >= 0 &&
            style.IndexOf("italic", StringComparison.OrdinalIgnoreCase) >= 0)
            return $"***{text}***";

        if (style.IndexOf("bold", StringComparison.OrdinalIgnoreCase) >= 0)
            return $"**{text}**";

        if (style.IndexOf("italic", StringComparison.OrdinalIgnoreCase) >= 0)
            return $"*{text}*";

        if (style.IndexOf("underline", StringComparison.OrdinalIgnoreCase) >= 0)
            return $"<u>{text}</u>";

        if (style.IndexOf("strikethrough", StringComparison.OrdinalIgnoreCase) >= 0)
            return $"~~{text}~~";

        return text;
    }
}
