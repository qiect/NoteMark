using System.Globalization;
using System.Text;
using NoteMark.MarkdownEngine.Elements;

namespace NoteMark.MarkdownEngine;

public interface IDocumentRenderer
{
    string Render(MarkdownDocument document);
}

public abstract class MarkdownRendererBase : IDocumentRenderer
{
    public abstract string Render(MarkdownDocument document);
    public abstract string ToHtml(MarkdownDocument document);
    public abstract string ToOneNoteXml(MarkdownDocument document);
}

public class HtmlMarkdownRenderer : MarkdownRendererBase
{
    public override string Render(MarkdownDocument document) => ToHtml(document);

    public override string ToHtml(MarkdownDocument document)
    {
        var sb = new StringBuilder();
        foreach (var block in document.Blocks)
            sb.Append(RenderBlockHtml(block));
        return sb.ToString();
    }

    public override string ToOneNoteXml(MarkdownDocument document) =>
        throw new NotSupportedException("Use OneNoteXmlRenderer for XML rendering.");

    private static string RenderBlockHtml(BlockElement block)
    {
        return block switch
        {
            HeadingElement h => RenderHeadingHtml(h),
            ParagraphElement p => RenderParagraphHtml(p),
            CodeBlockElement c => RenderCodeBlockHtml(c),
            QuoteBlockElement q => RenderQuoteBlockHtml(q),
            TableElement t => RenderTableHtml(t),
            ListElement l => RenderListHtml(l),
            TaskListElement tl => RenderTaskListHtml(tl),
            MathBlockElement m => RenderMathBlockHtml(m),
            DiagramBlockElement d => RenderDiagramBlockHtml(d),
            HorizontalRuleElement => "<hr />\n",
            _ => ""
        };
    }

    private static string RenderHeadingHtml(HeadingElement heading)
    {
        var tag = string.Format(CultureInfo.InvariantCulture, "h{0}", heading.Level);
        var content = RenderInlinesHtml(heading.Inlines);
        return string.Format(CultureInfo.InvariantCulture, "<{0}>{1}</{0}>\n", tag, content);
    }

    private static string RenderParagraphHtml(ParagraphElement paragraph)
    {
        var content = RenderInlinesHtml(paragraph.Inlines);
        return string.Format(CultureInfo.InvariantCulture, "<p>{0}</p>\n", content);
    }

    private static string RenderCodeBlockHtml(CodeBlockElement code)
    {
        var langAttr = string.IsNullOrEmpty(code.Language)
            ? ""
            : string.Format(CultureInfo.InvariantCulture, " class=\"language-{0}\"", code.Language);
        return string.Format(CultureInfo.InvariantCulture, "<pre><code{0}>{1}</code></pre>\n", langAttr, EscapeHtml(code.Code));
    }

    private static string RenderQuoteBlockHtml(QuoteBlockElement quote)
    {
        var classAttr = quote.HasHeadingIcon ? " class=\"has-heading-icon\"" : "";
        var sb = new StringBuilder();
        sb.AppendFormat(CultureInfo.InvariantCulture, "<blockquote{0}>\n", classAttr);
        foreach (var child in quote.Children)
            sb.Append(RenderBlockHtml(child));
        sb.Append("</blockquote>\n");
        return sb.ToString();
    }

    private static string RenderTableHtml(TableElement table)
    {
        var sb = new StringBuilder();
        sb.Append("<table>\n");

        if (table.Headers.Count > 0)
        {
            sb.Append("<thead>\n<tr>\n");
            foreach (var header in table.Headers)
                sb.AppendFormat(CultureInfo.InvariantCulture, "<th>{0}</th>\n", RenderInlinesHtml(header));
            sb.Append("</tr>\n</thead>\n");
        }

        sb.Append("<tbody>\n");
        foreach (var row in table.Rows)
        {
            sb.Append("<tr>\n");
            foreach (var cell in row.Cells)
                sb.AppendFormat(CultureInfo.InvariantCulture, "<td>{0}</td>\n", RenderInlinesHtml(cell));
            sb.Append("</tr>\n");
        }
        sb.Append("</tbody>\n</table>\n");
        return sb.ToString();
    }

    private static string RenderListHtml(ListElement list)
    {
        var tag = list.IsOrdered ? "ol" : "ul";
        var sb = new StringBuilder();
        sb.AppendFormat(CultureInfo.InvariantCulture, "<{0}>\n", tag);
        foreach (var item in list.Items)
        {
            sb.AppendFormat(CultureInfo.InvariantCulture, "<li>{0}", RenderInlinesHtml(item.Content));
            foreach (var nested in item.NestedLists)
                sb.Append(RenderListHtml(nested));
            sb.Append("</li>\n");
        }
        sb.AppendFormat(CultureInfo.InvariantCulture, "</{0}>\n", tag);
        return sb.ToString();
    }

    private static string RenderTaskListHtml(TaskListElement taskList)
    {
        var sb = new StringBuilder();
        sb.Append("<ul class=\"task-list\">\n");
        foreach (var item in taskList.Items)
        {
            var checkAttr = item.IsChecked ? " checked" : "";
            sb.AppendFormat(CultureInfo.InvariantCulture, "<li><input type=\"checkbox\"{0} disabled /> {1}</li>\n", checkAttr, RenderInlinesHtml(item.Content));
        }
        sb.Append("</ul>\n");
        return sb.ToString();
    }

    private static string RenderMathBlockHtml(MathBlockElement math)
    {
        var tag = math.IsInline ? "span" : "div";
        var classAttr = math.IsInline ? "math inline" : "math";
        return string.Format(CultureInfo.InvariantCulture, "<{0} class=\"{1}\">{2}</{0}>\n", tag, classAttr, EscapeHtml(math.Formula));
    }

    private static string RenderDiagramBlockHtml(DiagramBlockElement diagram)
    {
        var typeStr = diagram.DiagramType.ToString().ToLowerInvariant();
        return string.Format(CultureInfo.InvariantCulture, "<div class=\"diagram\" data-type=\"{0}\">{1}</div>\n", typeStr, EscapeHtml(diagram.Content));
    }

    private static string RenderInlinesHtml(List<InlineElement> inlines)
    {
        var sb = new StringBuilder();
        foreach (var inline in inlines)
            sb.Append(RenderInlineHtml(inline));
        return sb.ToString();
    }

    private static string RenderInlineHtml(InlineElement inline)
    {
        return inline switch
        {
            TextElement t => EscapeHtml(t.Text),
            BoldElement b => string.Format(CultureInfo.InvariantCulture, "<strong>{0}</strong>", RenderInlinesHtml(b.Inlines)),
            ItalicElement i => string.Format(CultureInfo.InvariantCulture, "<em>{0}</em>", RenderInlinesHtml(i.Inlines)),
            StrikethroughElement s => string.Format(CultureInfo.InvariantCulture, "<del>{0}</del>", RenderInlinesHtml(s.Inlines)),
            CodeInlineElement c => string.Format(CultureInfo.InvariantCulture, "<code>{0}</code>", EscapeHtml(c.Code)),
            LinkElement l => string.Format(CultureInfo.InvariantCulture, "<a href=\"{0}\">{1}</a>", EscapeHtml(l.Url), EscapeHtml(l.Text)),
            ImageElement img => string.Format(CultureInfo.InvariantCulture, "<img src=\"{0}\" alt=\"{1}\" />", EscapeHtml(img.Url), EscapeHtml(img.Alt)),
            MathInlineElement m => string.Format(CultureInfo.InvariantCulture, "<span class=\"math inline\">{0}</span>", EscapeHtml(m.Formula)),
            _ => ""
        };
    }

    private static string EscapeHtml(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");
    }
}

public class OneNoteXmlRenderer : MarkdownRendererBase
{
    public override string Render(MarkdownDocument document) => ToOneNoteXml(document);

    public override string ToHtml(MarkdownDocument document) =>
        throw new NotSupportedException("Use HtmlMarkdownRenderer for HTML rendering.");

    public override string ToOneNoteXml(MarkdownDocument document)
    {
        var sb = new StringBuilder();
        sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>\n");
        sb.Append("<one:Page xmlns:one=\"http://schemas.microsoft.com/office/onenote/2013/onenote\">\n");
        sb.Append("  <one:Outline>\n");
        sb.Append("    <one:OEChildren>\n");

        foreach (var block in document.Blocks)
            sb.Append(RenderBlockXml(block, 3));

        sb.Append("    </one:OEChildren>\n");
        sb.Append("  </one:Outline>\n");
        sb.Append("</one:Page>\n");
        return sb.ToString();
    }

    private static string RenderBlockXml(BlockElement block, int indent)
    {
        var prefix = new string(' ', indent * 2);
        return block switch
        {
            HeadingElement h => string.Format(CultureInfo.InvariantCulture,
                "{0}<one:OE style=\"font-size:{1}pt;font-weight:bold\">{2}</one:OE>\n",
                prefix, 28 - h.Level * 2, EscapeXml(RenderInlinesText(h.Inlines))),
            ParagraphElement p => string.Format(CultureInfo.InvariantCulture,
                "{0}<one:OE>{1}</one:OE>\n", prefix, EscapeXml(RenderInlinesText(p.Inlines))),
            CodeBlockElement c => string.Format(CultureInfo.InvariantCulture,
                "{0}<one:OE><one:T style=\"font-family:Consolas\"><![CDATA[{1}]]></one:T></one:OE>\n",
                prefix, c.Code),
            HorizontalRuleElement => string.Format(CultureInfo.InvariantCulture,
                "{0}<one:OE><one:HR /></one:OE>\n", prefix),
            _ => string.Format(CultureInfo.InvariantCulture,
                "{0}<one:OE>{1}</one:OE>\n", prefix, EscapeXml(block.ElementType))
        };
    }

    private static string RenderInlinesText(List<InlineElement> inlines)
    {
        var sb = new StringBuilder();
        foreach (var inline in inlines)
        {
            switch (inline)
            {
                case TextElement t:
                    sb.Append(t.Text);
                    break;
                case BoldElement b:
                    sb.Append(RenderInlinesText(b.Inlines));
                    break;
                case ItalicElement i:
                    sb.Append(RenderInlinesText(i.Inlines));
                    break;
                case CodeInlineElement c:
                    sb.Append(c.Code);
                    break;
                case LinkElement l:
                    sb.Append(l.Text);
                    break;
            }
        }
        return sb.ToString();
    }

    private static string EscapeXml(string text)
    {
        return text
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }
}
