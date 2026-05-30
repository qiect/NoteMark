using System.Text;
using System.Xml.Linq;

namespace OneMarkDotNet.OneNoteConverter;

public sealed class OneNoteToMarkdownConverter
{
    private const string OneNoteNamespace = "http://schemas.microsoft.com/office/onenote/2013/onenote";

    public string ConvertToMarkdown(string oneNoteXml)
    {
        var doc = XDocument.Parse(oneNoteXml);
        var root = doc.Root ?? throw new InvalidOperationException("Invalid OneNote XML");

        var sb = new StringBuilder();

        var titleElement = root.Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "Title");

        if (titleElement is not null)
        {
            var titleText = ExtractTextFromOe(titleElement);
            if (!string.IsNullOrWhiteSpace(titleText))
            {
                sb.AppendLine($"# {titleText.Trim()}");
                sb.AppendLine();
            }
        }

        var outlines = root.Descendants()
            .Where(e => e.Name.LocalName == "Outline");

        foreach (var outline in outlines)
        {
            ProcessOutline(outline, sb, 0);
        }

        return sb.ToString().TrimEnd();
    }

    public List<OutlineElement> ConvertToOutlineElements(string oneNoteXml)
    {
        var doc = XDocument.Parse(oneNoteXml);
        var root = doc.Root ?? throw new InvalidOperationException("Invalid OneNote XML");

        var elements = new List<OutlineElement>();

        var outlines = root.Descendants()
            .Where(e => e.Name.LocalName == "Outline");

        foreach (var outline in outlines)
        {
            ExtractOutlineElements(outline, elements, 0);
        }

        return elements;
    }

    private void ProcessOutline(XElement outline, StringBuilder sb, int baseIndent)
    {
        var oeChildren = outline.Element(XName.Get("OEChildren", OneNoteNamespace));
        if (oeChildren is null) return;

        foreach (var oe in oeChildren.Elements())
        {
            ProcessOe(oe, sb, baseIndent);
        }
    }

    private void ProcessOe(XElement oe, StringBuilder sb, int indentLevel)
    {
        var localName = oe.Name.LocalName;
        if (localName != "OE") return;

        var indentAttr = oe.Attribute("indent");
        var currentIndent = indentAttr is not null ? int.Parse(indentAttr.Value) : indentLevel;

        var tElement = oe.Element(XName.Get("T", OneNoteNamespace));
        var tableElement = oe.Element(XName.Get("Table", OneNoteNamespace));
        var imageElement = oe.Element(XName.Get("Image", OneNoteNamespace));
        var insertedFile = oe.Element(XName.Get("InsertedFile", OneNoteNamespace));
        var childOeChildren = oe.Element(XName.Get("OEChildren", OneNoteNamespace));

        if (tElement is not null)
        {
            var cdata = tElement.Nodes().OfType<XCData>().FirstOrDefault();
            var html = cdata?.Value ?? tElement.Value;

            var style = ExtractStyleFromOe(oe);
            var markdown = ConvertHtmlToMarkdown(html, style, currentIndent);
            sb.AppendLine(markdown);
        }
        else if (tableElement is not null)
        {
            ProcessTable(tableElement, sb);
        }
        else if (imageElement is not null)
        {
            var alt = imageElement.Element(XName.Get("Alt", OneNoteNamespace))?.Value ?? "image";
            sb.AppendLine($"![{alt}]()");
        }
        else if (insertedFile is not null)
        {
            var name = insertedFile.Attribute("preferredName")?.Value ?? "file";
            sb.AppendLine($"[{name}]()");
        }

        if (childOeChildren is not null)
        {
            foreach (var childOe in childOeChildren.Elements())
            {
                ProcessOe(childOe, sb, currentIndent + 1);
            }
        }
    }

    private void ProcessTable(XElement table, StringBuilder sb)
    {
        var rows = table.Elements(XName.Get("Row", OneNoteNamespace)).ToList();
        if (rows.Count == 0) return;

        var firstRow = rows[0];
        var colCount = firstRow.Elements(XName.Get("Cell", OneNoteNamespace)).Count();

        for (var i = 0; i < rows.Count; i++)
        {
            var cells = rows[i].Elements(XName.Get("Cell", OneNoteNamespace));
            var cellTexts = cells.Select(c =>
            {
                var oeChildren = c.Element(XName.Get("OEChildren", OneNoteNamespace));
                var oe = oeChildren?.Element(XName.Get("OE", OneNoteNamespace));
                var t = oe?.Element(XName.Get("T", OneNoteNamespace));
                var cdata = t?.Nodes().OfType<XCData>().FirstOrDefault();
                var html = cdata?.Value ?? t?.Value ?? string.Empty;
                return StripHtmlTags(html).Trim();
            }).ToList();

            sb.AppendLine($"| {string.Join(" | ", cellTexts)} |");

            if (i == 0)
            {
                sb.AppendLine($"| {string.Join(" | ", Enumerable.Repeat("---", colCount))} |");
            }
        }

        sb.AppendLine();
    }

    private string ConvertHtmlToMarkdown(string html, OneNoteStyle style, int indentLevel)
    {
        var text = StripHtmlTags(html);

        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        var isHeading = style.FontSize.HasValue && style.FontSize >= 12 && style.Bold;
        if (isHeading)
        {
            var level = style.FontSize switch
            {
                >= 20 => 1,
                >= 18 => 2,
                >= 16 => 3,
                >= 14 => 4,
                >= 12 => 5,
                _ => 6
            };
            text = $"{new string('#', level)} {text}";
        }
        else
        {
            var md = new StringBuilder();

            if (style.Bold) md.Append("**");
            if (style.Italic) md.Append("*");
            if (style.Strikethrough) md.Append("~~");

            md.Append(ApplyInlineHtmlFormatting(html));

            if (style.Strikethrough) md.Append("~~");
            if (style.Italic) md.Append("*");
            if (style.Bold) md.Append("**");

            text = md.ToString();
        }

        if (indentLevel > 0 && !isHeading)
        {
            text = $"{new string(' ', (indentLevel - 1) * 2)}> {text}";
        }

        return text;
    }

    private string ApplyInlineHtmlFormatting(string html)
    {
        var result = html;

        result = System.Text.RegularExpressions.Regex.Replace(result, "<b>(.*?)</b>", "**$1**", RegexOptions.IgnoreCase);
        result = System.Text.RegularExpressions.Regex.Replace(result, "<strong>(.*?)</strong>", "**$1**", RegexOptions.IgnoreCase);
        result = System.Text.RegularExpressions.Regex.Replace(result, "<i>(.*?)</i>", "*$1*", RegexOptions.IgnoreCase);
        result = System.Text.RegularExpressions.Regex.Replace(result, "<em>(.*?)</em>", "*$1*", RegexOptions.IgnoreCase);
        result = System.Text.RegularExpressions.Regex.Replace(result, "<u>(.*?)</u>", "<u>$1</u>", RegexOptions.IgnoreCase);
        result = System.Text.RegularExpressions.Regex.Replace(result, "<s>(.*?)</s>", "~~$1~~", RegexOptions.IgnoreCase);
        result = System.Text.RegularExpressions.Regex.Replace(result, "<strike>(.*?)</strike>", "~~$1~~", RegexOptions.IgnoreCase);
        result = System.Text.RegularExpressions.Regex.Replace(result, "<del>(.*?)</del>", "~~$1~~", RegexOptions.IgnoreCase);
        result = System.Text.RegularExpressions.Regex.Replace(result, @"<a\s+href=""([^""]*)""[^>]*>(.*?)</a>", "[$2]($1)", RegexOptions.IgnoreCase);
        result = System.Text.RegularExpressions.Regex.Replace(result, @"<a\s+href='([^']*)'[^>]*>(.*?)</a>", "[$2]($1)", RegexOptions.IgnoreCase);
        result = System.Text.RegularExpressions.Regex.Replace(result, @"<code[^>]*>(.*?)</code>", "`$1`", RegexOptions.IgnoreCase);
        result = System.Text.RegularExpressions.Regex.Replace(result, @"<img[^>]*alt=""([^""]*)""[^>]*/?>", "![$1]()", RegexOptions.IgnoreCase);
        result = System.Text.RegularExpressions.Regex.Replace(result, @"<br\s*/?>", "  \n", RegexOptions.IgnoreCase);

        result = StripHtmlTags(result);

        return result;
    }

    private OneNoteStyle ExtractStyleFromOe(XElement oe)
    {
        string? fontFamily = null;
        string? fontColor = null;
        string? highlightColor = null;
        double? fontSize = null;
        var bold = false;
        var italic = false;
        var underline = false;
        var strikethrough = false;

        var styleAttr = oe.Attribute("style")?.Value;
        if (styleAttr is not null)
        {
            foreach (var part in styleAttr.Split(';'))
            {
                var kv = part.Split(':', 2);
                if (kv.Length != 2) continue;

                var key = kv[0].Trim().ToLowerInvariant();
                var value = kv[1].Trim();

                switch (key)
                {
                    case "font-family":
                        fontFamily = value;
                        break;
                    case "font-size" when value.EndsWith("pt"):
                        fontSize = double.TryParse(value[..^2], out var fs) ? fs : null;
                        break;
                    case "color":
                        fontColor = value;
                        break;
                    case "background":
                        highlightColor = value;
                        break;
                    case "font-weight" when value is "bold" or "700":
                        bold = true;
                        break;
                    case "font-style" when value == "italic":
                        italic = true;
                        break;
                    case "text-decoration" when value.Contains("underline"):
                        underline = true;
                        break;
                    case "text-decoration" when value.Contains("line-through"):
                        strikethrough = true;
                        break;
                }
            }
        }

        var fontFamilyAttr = oe.Attribute("fontFamily")?.Value;
        if (fontFamilyAttr is not null) fontFamily = fontFamilyAttr;

        var fontSizeAttr = oe.Attribute("fontSize")?.Value;
        if (fontSizeAttr is not null && double.TryParse(fontSizeAttr, out var fsize))
            fontSize = fsize;

        var fontColorAttr = oe.Attribute("fontColor")?.Value;
        if (fontColorAttr is not null) fontColor = fontColorAttr;

        var highlightAttr = oe.Attribute("highlightColor")?.Value;
        if (highlightAttr is not null) highlightColor = highlightAttr;

        return new OneNoteStyle(
            FontFamily: fontFamily,
            FontColor: fontColor,
            HighlightColor: highlightColor,
            FontSize: fontSize,
            Bold: bold,
            Italic: italic,
            Underline: underline,
            Strikethrough: strikethrough,
            Superscript: false,
            Subscript: false
        );
    }

    private void ExtractOutlineElements(XElement outline, List<OutlineElement> elements, int indentLevel)
    {
        var oeChildren = outline.Element(XName.Get("OEChildren", OneNoteNamespace));
        if (oeChildren is null) return;

        foreach (var oe in oeChildren.Elements())
        {
            ExtractOeElement(oe, elements, indentLevel);
        }
    }

    private void ExtractOeElement(XElement oe, List<OutlineElement> elements, int indentLevel)
    {
        if (oe.Name.LocalName != "OE") return;

        var indentAttr = oe.Attribute("indent");
        var currentIndent = indentAttr is not null ? int.Parse(indentAttr.Value) : indentLevel;

        var tElement = oe.Element(XName.Get("T", OneNoteNamespace));
        var tableElement = oe.Element(XName.Get("Table", OneNoteNamespace));
        var imageElement = oe.Element(XName.Get("Image", OneNoteNamespace));
        var childOeChildren = oe.Element(XName.Get("OEChildren", OneNoteNamespace));

        var style = ExtractStyleFromOe(oe);

        if (tElement is not null)
        {
            var cdata = tElement.Nodes().OfType<XCData>().FirstOrDefault();
            var html = cdata?.Value ?? tElement.Value;
            var text = StripHtmlTags(html);

            var elementType = DetermineElementType(style, currentIndent);

            var children = new List<OutlineElement>();
            if (childOeChildren is not null)
            {
                foreach (var childOe in childOeChildren.Elements())
                {
                    ExtractOeElement(childOe, children, currentIndent + 1);
                }
            }

            elements.Add(new OutlineElement
            {
                Type = elementType,
                Content = text,
                Style = style,
                Level = currentIndent,
                Children = children
            });
        }
        else if (tableElement is not null)
        {
            elements.Add(new OutlineElement
            {
                Type = ElementType.Table,
                Content = ExtractTableContent(tableElement),
                Level = currentIndent
            });
        }
        else if (imageElement is not null)
        {
            elements.Add(new OutlineElement
            {
                Type = ElementType.Image,
                Content = imageElement.Element(XName.Get("Alt", OneNoteNamespace))?.Value ?? string.Empty,
                Level = currentIndent
            });
        }
    }

    private static ElementType DetermineElementType(OneNoteStyle style, int indentLevel)
    {
        if (style.FontSize.HasValue && style.FontSize >= 12 && style.Bold)
            return ElementType.Heading;

        if (style.FontFamily?.Equals("Consolas", StringComparison.OrdinalIgnoreCase) == true)
            return ElementType.CodeBlock;

        if (indentLevel > 0)
            return ElementType.Quote;

        return ElementType.Text;
    }

    private static string ExtractTextFromOe(XElement container)
    {
        var sb = new StringBuilder();
        var tElements = container.Descendants(XName.Get("T", OneNoteNamespace));

        foreach (var t in tElements)
        {
            var cdata = t.Nodes().OfType<XCData>().FirstOrDefault();
            var html = cdata?.Value ?? t.Value;
            sb.Append(StripHtmlTags(html));
        }

        return sb.ToString();
    }

    private static string ExtractTableContent(XElement table)
    {
        var rows = new List<string>();
        foreach (var row in table.Elements(XName.Get("Row", OneNoteNamespace)))
        {
            var cells = new List<string>();
            foreach (var cell in row.Elements(XName.Get("Cell", OneNoteNamespace)))
            {
                var oeChildren = cell.Element(XName.Get("OEChildren", OneNoteNamespace));
                var oe = oeChildren?.Element(XName.Get("OE", OneNoteNamespace));
                var t = oe?.Element(XName.Get("T", OneNoteNamespace));
                var cdata = t?.Nodes().OfType<XCData>().FirstOrDefault();
                var html = cdata?.Value ?? t?.Value ?? string.Empty;
                cells.Add(StripHtmlTags(html).Trim());
            }
            rows.Add(string.Join("|", cells));
        }
        return string.Join("\n", rows);
    }

    private static string StripHtmlTags(string html)
    {
        var result = System.Text.RegularExpressions.Regex.Replace(html, "<br\\s*/?>", "\n", RegexOptions.IgnoreCase);
        result = System.Text.RegularExpressions.Regex.Replace(result, "<[^>]+>", string.Empty);
        result = System.Net.WebUtility.HtmlDecode(result);
        return result;
    }
}
