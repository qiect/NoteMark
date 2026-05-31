using System.Text;
using System.Xml.Linq;

namespace OneMarkDotNet.OneNoteConverter;

public sealed class OneNoteXmlBuilder
{
    private const string OneNoteNamespace = "http://schemas.microsoft.com/office/onenote/2013/onenote";

    private static readonly XNamespace Ns = OneNoteNamespace;

    public string BuildPage(string title, string content)
    {
        var page = new XElement(Ns + "Page",
            new XAttribute("name", title),
            new XElement(Ns + "Title",
                new XElement(Ns + "OE",
                    new XElement(Ns + "T",
                        new XCData($"<span style='font-weight:bold'>{SecurityElementEscape(title)}</span>")))),
            XElement.Parse(content));

        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            page);

        return doc.ToString();
    }

    public string BuildOutline(IEnumerable<OutlineElement> elements)
    {
        var oeChildren = new XElement(Ns + "OEChildren");

        foreach (var element in elements)
        {
            var oe = BuildOElement(element);
            oeChildren.Add(oe);
        }

        var outline = new XElement(Ns + "Outline",
            new XElement(Ns + "OEChildren", oeChildren.Elements()));

        return outline.ToString();
    }

    public XElement BuildOE(string htmlContent, OneNoteStyle style)
    {
        var t = new XElement(Ns + "T", new XCData(htmlContent));

        if (style.IsEmpty)
            return new XElement(Ns + "OE", t);

        var oe = new XElement(Ns + "OE");
        ApplyStyleAttribute(oe, style);
        oe.Add(t);
        return oe;
    }

    public XElement BuildOE(OutlineElement element)
    {
        return BuildOElement(element);
    }

    public string BuildTable(int rows, int cols)
    {
        var table = new XElement(Ns + "Table",
            new XAttribute("bordersVisible", "true"));

        for (var r = 0; r < rows; r++)
        {
            var row = new XElement(Ns + "Row");
            for (var c = 0; c < cols; c++)
            {
                var cell = new XElement(Ns + "Cell",
                    new XElement(Ns + "OEChildren",
                        new XElement(Ns + "OE",
                            new XElement(Ns + "T",
                                new XCData("&nbsp;")))));
                row.Add(cell);
            }
            table.Add(row);
        }

        return table.ToString();
    }

    public string BuildTable(string[,] data)
    {
        var rows = data.GetLength(0);
        var cols = data.GetLength(1);

        var table = new XElement(Ns + "Table",
            new XAttribute("bordersVisible", "true"));

        for (var r = 0; r < rows; r++)
        {
            var row = new XElement(Ns + "Row");
            for (var c = 0; c < cols; c++)
            {
                var cellContent = SecurityElementEscape(data[r, c]);
                var isHeader = r == 0;
                var html = isHeader
                    ? $"<span style='font-weight:bold'>{cellContent}</span>"
                    : cellContent;

                var cell = new XElement(Ns + "Cell",
                    new XElement(Ns + "OEChildren",
                        new XElement(Ns + "OE",
                            new XElement(Ns + "T", new XCData(html)))));
                row.Add(cell);
            }
            table.Add(row);
        }

        return table.ToString();
    }

    public string BuildImage(string imagePath, double width = 0, double height = 0)
    {
        var image = new XElement(Ns + "Image",
            new XAttribute("format", "png"));

        if (width > 0)
            image.Add(new XAttribute("originalWidth", width.ToString("F0")));
        if (height > 0)
            image.Add(new XAttribute("originalHeight", height.ToString("F0")));

        image.Add(new XElement(Ns + "Data", Convert.ToBase64String(File.ReadAllBytes(imagePath))));

        return image.ToString();
    }

    public string BuildInsertedFile(string filePath, string preferredName)
    {
        var insertedFile = new XElement(Ns + "InsertedFile",
            new XAttribute("pathCache", filePath),
            new XAttribute("pathSource", filePath),
            new XAttribute("preferredName", preferredName));

        return insertedFile.ToString();
    }

    private XElement BuildOElement(OutlineElement element)
    {
        return element.Type switch
        {
            ElementType.Heading => BuildHeadingElement(element),
            ElementType.CodeBlock => BuildCodeBlockElement(element),
            ElementType.Quote => BuildQuoteElement(element),
            ElementType.Table => BuildTableElement(element),
            ElementType.List => BuildListElement(element),
            ElementType.Image => BuildImageElement(element),
            ElementType.Math => BuildMathElement(element),
            _ => BuildTextElement(element)
        };
    }

    private XElement BuildTextElement(OutlineElement element)
    {
        var html = BuildStyledHtml(element.Content, element.Style);
        var oe = BuildOE(html, element.Style);
        return oe;
    }

    private XElement BuildHeadingElement(OutlineElement element)
    {
        var fontSize = element.Level switch
        {
            1 => 20.0,
            2 => 18.0,
            3 => 16.0,
            4 => 14.0,
            5 => 12.0,
            _ => 11.0
        };

        var style = new OneNoteStyle
        {
            FontFamily = element.Style.FontFamily ?? "Calibri",
            FontColor = element.Style.FontColor,
            HighlightColor = element.Style.HighlightColor,
            FontSize = fontSize,
            Bold = true,
            Italic = element.Style.Italic,
            Underline = element.Style.Underline,
            Strikethrough = element.Style.Strikethrough,
            Superscript = element.Style.Superscript,
            Subscript = element.Style.Subscript
        };

        var html = BuildStyledHtml(element.Content, style);
        var oe = BuildOE(html, style);
        return oe;
    }

    private XElement BuildCodeBlockElement(OutlineElement element)
    {
        var style = new OneNoteStyle
        {
            FontFamily = "Consolas",
            FontColor = element.Style.FontColor,
            HighlightColor = element.Style.HighlightColor ?? "#F5F5F5",
            FontSize = element.Style.FontSize ?? 10.0,
            Bold = false,
            Italic = false,
            Underline = false,
            Strikethrough = false,
            Superscript = false,
            Subscript = false
        };

        var escaped = SecurityElementEscape(element.Content);
        var html = $"<span style='font-family:Consolas;font-size:10pt;background:#F5F5F5'>{escaped}</span>";

        var oe = new XElement(Ns + "OE");
        ApplyStyleAttribute(oe, style);
        oe.Add(new XElement(Ns + "T", new XCData(html)));
        return oe;
    }

    private XElement BuildQuoteElement(OutlineElement element)
    {
        var style = new OneNoteStyle
        {
            FontFamily = element.Style.FontFamily,
            FontColor = element.Style.FontColor ?? "#666666",
            HighlightColor = element.Style.HighlightColor,
            FontSize = element.Style.FontSize,
            Bold = element.Style.Bold,
            Italic = element.Style.Italic,
            Underline = element.Style.Underline,
            Strikethrough = element.Style.Strikethrough,
            Superscript = element.Style.Superscript,
            Subscript = element.Style.Subscript
        };

        var html = BuildStyledHtml(element.Content, style);
        var oe = BuildOE(html, style);
        oe.Add(new XAttribute("indent", "1"));

        if (element.Children.Count > 0)
        {
            var childOeChildren = new XElement(Ns + "OEChildren");
            foreach (var child in element.Children)
            {
                childOeChildren.Add(BuildOElement(child));
            }
            oe.Add(childOeChildren);
        }

        return oe;
    }

    private XElement BuildTableElement(OutlineElement element)
    {
        var lines = element.Content.Split('\n');
        var rows = lines.Length;
        var cols = 0;

        var cells = new List<string[]>();
        foreach (var line in lines)
        {
            var rowCells = line.Split('|', StringSplitOptions.RemoveEmptyEntries)
                .Select(c => c.Trim())
                .ToArray();
            cols = Math.Max(cols, rowCells.Length);
            cells.Add(rowCells);
        }

        var table = new XElement(Ns + "Table",
            new XAttribute("bordersVisible", "true"));

        for (var r = 0; r < rows; r++)
        {
            var row = new XElement(Ns + "Row");
            for (var c = 0; c < cols; c++)
            {
                var cellText = c < cells[r].Length ? SecurityElementEscape(cells[r][c]) : "&nbsp;";
                var isHeader = r == 0;
                var html = isHeader
                    ? $"<span style='font-weight:bold'>{cellText}</span>"
                    : cellText;

                var cell = new XElement(Ns + "Cell",
                    new XElement(Ns + "OEChildren",
                        new XElement(Ns + "OE",
                            new XElement(Ns + "T", new XCData(html)))));
                row.Add(cell);
            }
            table.Add(row);
        }

        var oe = new XElement(Ns + "OE");
        oe.Add(table);
        return oe;
    }

    private XElement BuildListElement(OutlineElement element)
    {
        var html = BuildStyledHtml(element.Content, element.Style);
        var oe = BuildOE(html, element.Style);
        oe.Add(new XAttribute("indent", element.Level.ToString()));

        if (element.Children.Count > 0)
        {
            var childOeChildren = new XElement(Ns + "OEChildren");
            foreach (var child in element.Children)
            {
                childOeChildren.Add(BuildOElement(child));
            }
            oe.Add(childOeChildren);
        }

        return oe;
    }

    private XElement BuildImageElement(OutlineElement element)
    {
        var image = new XElement(Ns + "Image",
            new XAttribute("format", "png"));

        if (File.Exists(element.Content))
        {
            image.Add(new XElement(Ns + "Data",
                Convert.ToBase64String(File.ReadAllBytes(element.Content))));
        }

        var oe = new XElement(Ns + "OE");
        oe.Add(image);
        return oe;
    }

    private XElement BuildMathElement(OutlineElement element)
    {
        var html = $"<span style='font-family:Calibri;font-style:italic'>{SecurityElementEscape(element.Content)}</span>";
        var oe = new XElement(Ns + "OE",
            new XElement(Ns + "T", new XCData(html)));
        return oe;
    }

    private static string BuildStyledHtml(string content, OneNoteStyle style)
    {
        if (style.IsEmpty)
            return SecurityElementEscape(content);

        var sb = new StringBuilder();
        sb.Append("<span style='");

        if (style.FontFamily is not null)
            sb.Append($"font-family:{style.FontFamily};");
        if (style.FontSize.HasValue)
            sb.Append($"font-size:{style.FontSize.Value:F0}pt;");
        if (style.FontColor is not null)
            sb.Append($"color:{style.FontColor};");
        if (style.HighlightColor is not null)
            sb.Append($"background:{style.HighlightColor};");

        sb.Append("'>");

        var text = SecurityElementEscape(content);

        if (style.Bold) text = $"<b>{text}</b>";
        if (style.Italic) text = $"<i>{text}</i>";
        if (style.Underline) text = $"<u>{text}</u>";
        if (style.Strikethrough) text = $"<s>{text}</s>";
        if (style.Superscript) text = $"<sup>{text}</sup>";
        if (style.Subscript) text = $"<sub>{text}</sub>";

        sb.Append(text);
        sb.Append("</span>");

        return sb.ToString();
    }

    private static void ApplyStyleAttribute(XElement oe, OneNoteStyle style)
    {
        if (style.FontFamily is not null)
            oe.Add(new XAttribute("fontFamily", style.FontFamily));
        if (style.FontSize.HasValue)
            oe.Add(new XAttribute("fontSize", style.FontSize.Value.ToString("F0")));
        if (style.FontColor is not null)
            oe.Add(new XAttribute("fontColor", style.FontColor));
        if (style.HighlightColor is not null)
            oe.Add(new XAttribute("highlightColor", style.HighlightColor));
    }

    private static string SecurityElementEscape(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }
}
