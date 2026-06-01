using System.Text;
using System.Xml.Linq;

namespace OneMarkDotNet.OneNoteConverter;

public sealed class HtmlToOneNoteConverter
{
    private const string OneNoteNamespace = "http://schemas.microsoft.com/office/onenote/2013/onenote";
    private static readonly XNamespace Ns = OneNoteNamespace;

    private static readonly HashSet<string> SupportedTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "h1", "h2", "h3", "h4", "h5", "h6",
        "p", "br", "b", "i", "u", "s", "a", "img",
        "table", "ul", "ol", "li", "blockquote", "code", "pre", "span"
    };

    private static readonly Dictionary<string, double> HeadingFontSizes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["h1"] = 20.0,
        ["h2"] = 18.0,
        ["h3"] = 16.0,
        ["h4"] = 14.0,
        ["h5"] = 12.0,
        ["h6"] = 11.0
    };

    public XElement ConvertHtmlToOneNoteXml(string html)
    {
        var wrapped = $"<div>{html}</div>";
        var doc = XHtmlParser.Parse(wrapped);
        var root = doc.Root ?? throw new InvalidOperationException("Failed to parse HTML");

        var oeChildren = new XElement(Ns + "OEChildren");
        ProcessNode(root, oeChildren, 0);

        return oeChildren;
    }

    public List<OutlineElement> ConvertHtmlToOutlineElements(string html)
    {
        var wrapped = $"<div>{html}</div>";
        var doc = XHtmlParser.Parse(wrapped);
        var root = doc.Root ?? throw new InvalidOperationException("Failed to parse HTML");

        var elements = new List<OutlineElement>();
        ProcessNodeToElements(root, elements, 0);
        return elements;
    }

    public string ConvertHtmlToCdata(string html)
    {
        var sb = new StringBuilder();
        var doc = XHtmlParser.Parse($"<div>{html}</div>");
        var root = doc.Root;
        if (root is null) return string.Empty;

        foreach (var node in root.Nodes())
        {
            sb.Append(RenderNodeToHtml(node));
        }

        return sb.ToString();
    }

    private void ProcessNode(XNode node, XElement parent, int indentLevel)
    {
        if (node is XText textNode)
        {
            var text = textNode.Value.Trim();
            if (string.IsNullOrEmpty(text)) return;

            var oe = new XElement(Ns + "OE");
            if (indentLevel > 0)
                oe.Add(new XAttribute("indent", indentLevel.ToString()));

            oe.Add(new XElement(Ns + "T", new XCData(SecurityElementEscape(text))));
            parent.Add(oe);
            return;
        }

        if (node is not XElement element) return;

        var tagName = element.Name.LocalName.ToLowerInvariant();

        if (!SupportedTags.Contains(tagName) && tagName != "div")
        {
            foreach (var child in element.Nodes())
                ProcessNode(child, parent, indentLevel);
            return;
        }

        switch (tagName)
        {
            case "h1" or "h2" or "h3" or "h4" or "h5" or "h6":
                ProcessHeading(element, parent);
                break;
            case "p":
                ProcessParagraph(element, parent, indentLevel);
                break;
            case "br":
                break;
            case "b" or "i" or "u" or "s" or "span":
                ProcessInlineElement(element, parent, indentLevel);
                break;
            case "a":
                ProcessAnchor(element, parent, indentLevel);
                break;
            case "img":
                ProcessImage(element, parent);
                break;
            case "ul" or "ol":
                ProcessList(element, parent, tagName == "ol", indentLevel);
                break;
            case "li":
                ProcessListItem(element, parent, indentLevel);
                break;
            case "blockquote":
                ProcessBlockquote(element, parent, indentLevel);
                break;
            case "code" or "pre":
                ProcessCode(element, parent);
                break;
            case "table":
                ProcessTable(element, parent);
                break;
            default:
                foreach (var child in element.Nodes())
                    ProcessNode(child, parent, indentLevel);
                break;
        }
    }

    private void ProcessHeading(XElement element, XElement parent)
    {
        var tagName = element.Name.LocalName.ToLowerInvariant();
        var fontSize = HeadingFontSizes[tagName];
        var level = int.Parse(tagName.Substring(1));

        var html = BuildInlineHtml(element);
        var style = new OneNoteStyle
        {
            FontFamily = "Calibri",
            FontSize = fontSize,
            Bold = true
        };

        var oe = new XElement(Ns + "OE");
        oe.Add(new XAttribute("style", $"font-family:Calibri;font-size:{fontSize:F0}pt;font-weight:bold"));
        oe.Add(new XElement(Ns + "T", new XCData(html)));
        parent.Add(oe);
    }

    private void ProcessParagraph(XElement element, XElement parent, int indentLevel)
    {
        var html = BuildInlineHtml(element);
        var oe = new XElement(Ns + "OE");
        if (indentLevel > 0)
            oe.Add(new XAttribute("indent", indentLevel.ToString()));

        oe.Add(new XElement(Ns + "T", new XCData(html)));
        parent.Add(oe);
    }

    private void ProcessInlineElement(XElement element, XElement parent, int indentLevel)
    {
        var html = BuildInlineHtml(element);
        var oe = new XElement(Ns + "OE");
        if (indentLevel > 0)
            oe.Add(new XAttribute("indent", indentLevel.ToString()));

        oe.Add(new XElement(Ns + "T", new XCData(html)));
        parent.Add(oe);
    }

    private void ProcessAnchor(XElement element, XElement parent, int indentLevel)
    {
        var href = element.Attribute("href")?.Value ?? string.Empty;
        var text = element.Value;

        var html = $"<a href=\"{href}\">{SecurityElementEscape(text)}</a>";
        var oe = new XElement(Ns + "OE");
        if (indentLevel > 0)
            oe.Add(new XAttribute("indent", indentLevel.ToString()));

        oe.Add(new XElement(Ns + "T", new XCData(html)));
        parent.Add(oe);
    }

    private void ProcessImage(XElement element, XElement parent)
    {
        var src = element.Attribute("src")?.Value ?? string.Empty;
        var alt = element.Attribute("alt")?.Value ?? string.Empty;

        var image = new XElement(Ns + "Image",
            new XAttribute("format", "png"));

        if (Uri.TryCreate(src, UriKind.Absolute, out var uri) && uri.IsFile && File.Exists(uri.LocalPath))
        {
            image.Add(new XElement(Ns + "Data",
                Convert.ToBase64String(File.ReadAllBytes(uri.LocalPath))));
        }

        var oe = new XElement(Ns + "OE", image);
        parent.Add(oe);
    }

    private void ProcessList(XElement element, XElement parent, bool isOrdered, int indentLevel)
    {
        var listType = isOrdered ? "ol" : "ul";
        foreach (var li in element.Elements())
        {
            if (li.Name.LocalName.Equals("li", StringComparison.OrdinalIgnoreCase))
            {
                ProcessListItem(li, parent, indentLevel + 1);
            }
        }
    }

    private void ProcessListItem(XElement element, XElement parent, int indentLevel)
    {
        var html = BuildInlineHtml(element);
        var bullet = indentLevel > 1 ? "  " + new string('·', 1) + " " : "• ";
        html = bullet + html;

        var oe = new XElement(Ns + "OE");
        oe.Add(new XAttribute("indent", Math.Max(0, indentLevel - 1).ToString()));
        oe.Add(new XElement(Ns + "T", new XCData(html)));
        parent.Add(oe);
    }

    private void ProcessBlockquote(XElement element, XElement parent, int indentLevel)
    {
        var newIndent = indentLevel + 1;
        foreach (var child in element.Nodes())
        {
            ProcessNode(child, parent, newIndent);
        }
    }

    private void ProcessCode(XElement element, XElement parent)
    {
        var text = element.Value;
        var html = $"<span style='font-family:Consolas;font-size:10pt;background:#F5F5F5'>{SecurityElementEscape(text)}</span>";

        var oe = new XElement(Ns + "OE",
            new XAttribute("style", "font-family:Consolas;font-size:10pt"),
            new XElement(Ns + "T", new XCData(html)));
        parent.Add(oe);
    }

    private void ProcessTable(XElement element, XElement parent)
    {
        var table = new XElement(Ns + "Table",
            new XAttribute("bordersVisible", "true"));

        foreach (var row in element.Elements())
        {
            if (!row.Name.LocalName.Equals("tr", StringComparison.OrdinalIgnoreCase)) continue;

            var rowElement = new XElement(Ns + "Row");
            foreach (var cell in row.Elements())
            {
                if (!cell.Name.LocalName.Equals("td", StringComparison.OrdinalIgnoreCase) &&
                    !cell.Name.LocalName.Equals("th", StringComparison.OrdinalIgnoreCase)) continue;

                var isHeader = cell.Name.LocalName.Equals("th", StringComparison.OrdinalIgnoreCase);
                var cellText = cell.Value.Trim();
                var html = isHeader
                    ? $"<span style='font-weight:bold'>{SecurityElementEscape(cellText)}</span>"
                    : SecurityElementEscape(cellText);

                var cellElement = new XElement(Ns + "Cell",
                    new XElement(Ns + "OEChildren",
                        new XElement(Ns + "OE",
                            new XElement(Ns + "T", new XCData(html)))));
                rowElement.Add(cellElement);
            }
            table.Add(rowElement);
        }

        var oe = new XElement(Ns + "OE", table);
        parent.Add(oe);
    }

    private void ProcessNodeToElements(XNode node, List<OutlineElement> elements, int indentLevel)
    {
        if (node is XText textNode)
        {
            var text = textNode.Value.Trim();
            if (string.IsNullOrEmpty(text)) return;

            elements.Add(new OutlineElement
            {
                Type = ElementType.Text,
                Content = text,
                Level = indentLevel
            });
            return;
        }

        if (node is not XElement element) return;

        var tagName = element.Name.LocalName.ToLowerInvariant();

        switch (tagName)
        {
            case "h1" or "h2" or "h3" or "h4" or "h5" or "h6":
                var level = int.Parse(tagName.Substring(1));
                elements.Add(new OutlineElement
                {
                    Type = ElementType.Heading,
                    Content = element.Value.Trim(),
                    Level = level,
                    Style = new OneNoteStyle
                    {
                        FontFamily = "Calibri",
                        FontSize = HeadingFontSizes[tagName],
                        Bold = true
                    }
                });
                break;
            case "p":
                elements.Add(new OutlineElement
                {
                    Type = ElementType.Text,
                    Content = element.Value.Trim(),
                    Level = indentLevel
                });
                break;
            case "blockquote":
                var quoteChildren = new List<OutlineElement>();
                foreach (var child in element.Nodes())
                    ProcessNodeToElements(child, quoteChildren, indentLevel + 1);

                elements.Add(new OutlineElement
                {
                    Type = ElementType.Quote,
                    Content = element.Value.Trim(),
                    Level = indentLevel,
                    Children = quoteChildren
                });
                break;
            case "pre" or "code":
                elements.Add(new OutlineElement
                {
                    Type = ElementType.CodeBlock,
                    Content = element.Value,
                    Style = new OneNoteStyle
                    {
                        FontFamily = "Consolas",
                        FontSize = 10.0,
                        HighlightColor = "#F5F5F5"
                    }
                });
                break;
            case "ul" or "ol":
                ProcessListToElements(element, elements, indentLevel, tagName == "ol");
                break;
            case "table":
                elements.Add(new OutlineElement
                {
                    Type = ElementType.Table,
                    Content = ExtractTableContent(element)
                });
                break;
            case "img":
                elements.Add(new OutlineElement
                {
                    Type = ElementType.Image,
                    Content = element.Attribute("src")?.Value ?? string.Empty
                });
                break;
            default:
                foreach (var child in element.Nodes())
                    ProcessNodeToElements(child, elements, indentLevel);
                break;
        }
    }

    private void ProcessListToElements(XElement element, List<OutlineElement> elements, int indentLevel, bool isOrdered)
    {
        foreach (var li in element.Elements())
        {
            if (!li.Name.LocalName.Equals("li", StringComparison.OrdinalIgnoreCase)) continue;

            var children = new List<OutlineElement>();
            foreach (var child in li.Nodes())
            {
                if (child is XElement childEl &&
                    (childEl.Name.LocalName.Equals("ul", StringComparison.OrdinalIgnoreCase) ||
                     childEl.Name.LocalName.Equals("ol", StringComparison.OrdinalIgnoreCase)))
                {
                    ProcessListToElements(childEl, children, indentLevel + 1, childEl.Name.LocalName.Equals("ol", StringComparison.OrdinalIgnoreCase));
                }
            }

            elements.Add(new OutlineElement
            {
                Type = ElementType.List,
                Content = li.Value.Trim(),
                Level = indentLevel + 1,
                Children = children
            });
        }
    }

    private static string ExtractTableContent(XElement tableElement)
    {
        var rows = new List<string>();
        foreach (var row in tableElement.Elements())
        {
            if (!row.Name.LocalName.Equals("tr", StringComparison.OrdinalIgnoreCase)) continue;
            var cells = new List<string>();
            foreach (var cell in row.Elements())
            {
                var localName = cell.Name.LocalName;
                if (localName.Equals("td", StringComparison.OrdinalIgnoreCase) ||
                    localName.Equals("th", StringComparison.OrdinalIgnoreCase))
                {
                    cells.Add(cell.Value.Trim());
                }
            }
            rows.Add(string.Join("|", cells));
        }
        return string.Join("\n", rows);
    }

    private static string BuildInlineHtml(XElement element)
    {
        var sb = new StringBuilder();
        foreach (var node in element.Nodes())
        {
            sb.Append(RenderNodeToHtml(node));
        }
        return sb.ToString();
    }

    private static string RenderNodeToHtml(XNode node)
    {
        if (node is XText text)
            return SecurityElementEscape(text.Value);

        if (node is not XElement el) return string.Empty;

        var tag = el.Name.LocalName.ToLowerInvariant();
        var innerSb = new StringBuilder();
        foreach (var child in el.Nodes())
            innerSb.Append(RenderNodeToHtml(child));

        var inner = innerSb.ToString();

        return tag switch
        {
            "b" or "strong" => $"<b>{inner}</b>",
            "i" or "em" => $"<i>{inner}</i>",
            "u" => $"<u>{inner}</u>",
            "s" or "strike" or "del" => $"<s>{inner}</s>",
            "a" => $"<a href=\"{el.Attribute("href")?.Value ?? ""}\">{inner}</a>",
            "code" => $"<span style='font-family:Consolas;font-size:10pt;background:#F5F5F5'>{inner}</span>",
            "span" => BuildSpanHtml(el, inner),
            "br" => "<br/>",
            _ => inner
        };
    }

    private static string BuildSpanHtml(XElement span, string inner)
    {
        var style = span.Attribute("style")?.Value;
        return style is not null
            ? $"<span style='{style}'>{inner}</span>"
            : inner;
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

    private static class XHtmlParser
    {
        public static XDocument Parse(string html)
        {
            try
            {
                var sanitized = SanitizeHtml(html);
                return XDocument.Parse(sanitized);
            }
            catch
            {
                return XDocument.Parse($"<div>{SecurityElementEscape(html)}</div>");
            }
        }

        private static string SanitizeHtml(string html)
        {
            return html
                .Replace("&nbsp;", " ")
                .Replace("&copy;", "©")
                .Replace("&reg;", "®")
                .Replace("&mdash;", "—")
                .Replace("&ndash;", "–")
                .Replace("&ldquo;", "\u201C")
                .Replace("&rdquo;", "\u201D")
                .Replace("&lsquo;", "\u2018")
                .Replace("&rsquo;", "\u2019");
        }
    }
}
