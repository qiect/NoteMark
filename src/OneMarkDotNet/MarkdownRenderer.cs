namespace OneMarkDotNet
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using OneMarkDotNet.Elements;

    public sealed class RendererStyle
    {
        public string FontFamily { get; set; }
        public double FontSize { get; set; }
        public string TextColor { get; set; }
        public string BackgroundColor { get; set; }
        public string CodeFontFamily { get; set; }
        public string CodeBackgroundColor { get; set; }
        public string CodeTextColor { get; set; }

        public RendererStyle()
        {
            FontFamily = "Calibri";
            FontSize = 11.0;
            TextColor = "#000000";
            BackgroundColor = "#FFFFFF";
            CodeFontFamily = "Consolas";
            CodeBackgroundColor = "#F5F5F5";
            CodeTextColor = "#333333";
        }
    }

    public sealed class HtmlMarkdownRenderer
    {
        public string Render(MarkdownDocument doc)
        {
            var sb = new StringBuilder();

            foreach (var block in doc.Blocks)
            {
                RenderBlock(sb, block);
            }

            return sb.ToString();
        }

        private void RenderBlock(StringBuilder sb, BlockElement block)
        {
            if (block is HeadingElement heading)
            {
                RenderHeading(sb, heading);
            }
            else if (block is ParagraphElement)
            {
                RenderParagraph(sb, block);
            }
            else if (block is CodeBlockElement codeBlock)
            {
                RenderCodeBlock(sb, codeBlock);
            }
            else if (block is DiagramBlockElement diagram)
            {
                RenderDiagram(sb, diagram);
            }
            else if (block is ListElement list)
            {
                RenderList(sb, list);
            }
            else if (block is TaskListElement taskList)
            {
                RenderTaskList(sb, taskList);
            }
            else if (block is QuoteBlockElement quote)
            {
                RenderQuoteBlock(sb, quote);
            }
            else if (block is TableElement table)
            {
                RenderTable(sb, table);
            }
            else if (block is HorizontalRuleElement)
            {
                sb.AppendLine("<hr />");
            }
            else if (block is MathBlockElement math)
            {
                RenderMathBlock(sb, math);
            }
        }

        private void RenderHeading(StringBuilder sb, HeadingElement heading)
        {
            var tag = "h" + heading.Level;
            sb.Append('<');
            sb.Append(tag);
            sb.Append('>');
            RenderInlines(sb, heading.Children);
            sb.Append("</");
            sb.Append(tag);
            sb.AppendLine(">");
        }

        private void RenderParagraph(StringBuilder sb, BlockElement paragraph)
        {
            sb.Append("<p>");
            RenderInlines(sb, paragraph.Children);
            sb.AppendLine("</p>");
        }

        private void RenderCodeBlock(StringBuilder sb, CodeBlockElement codeBlock)
        {
            sb.Append("<pre><code");
            if (!string.IsNullOrEmpty(codeBlock.Language))
            {
                sb.Append(" class=\"language-");
                sb.Append(HtmlEncode(codeBlock.Language));
                sb.Append("\"");
            }

            sb.Append('>');
            sb.Append(HtmlEncode(codeBlock.Code));
            sb.AppendLine("</code></pre>");
        }

        private void RenderDiagram(StringBuilder sb, DiagramBlockElement diagram)
        {
            sb.Append("<div class=\"diagram\"><pre><code");
            if (diagram.DiagramType != DiagramType.Mermaid)
            {
                sb.Append(" class=\"language-");
                sb.Append(HtmlEncode(diagram.DiagramType.ToString().ToLowerInvariant()));
                sb.Append("\"");
            }

            sb.Append('>');
            sb.Append(HtmlEncode(diagram.Code));
            sb.AppendLine("</code></pre></div>");
        }

        private void RenderList(StringBuilder sb, ListElement list)
        {
            var tag = list.IsOrdered ? "ol" : "ul";
            sb.AppendLine("<" + tag + ">");
            foreach (var item in list.Items)
            {
                sb.Append("<li>");
                sb.Append(HtmlEncode(item.Content));
                if (item.Children.Count > 0)
                {
                    sb.AppendLine();
                    var innerList = new ListElement(list.IsOrdered, item.Children);
                    RenderList(sb, innerList);
                }

                sb.AppendLine("</li>");
            }

            sb.AppendLine("</" + tag + ">");
        }

        private void RenderTaskList(StringBuilder sb, TaskListElement taskList)
        {
            sb.AppendLine("<ul>");
            foreach (var item in taskList.Items)
            {
                sb.Append("<li><input type=\"checkbox\"");
                if (item.IsChecked)
                {
                    sb.Append(" checked");
                }

                sb.Append(" disabled /> ");
                sb.Append(HtmlEncode(item.Content));
                sb.AppendLine("</li>");
            }

            sb.AppendLine("</ul>");
        }

        private void RenderQuoteBlock(StringBuilder sb, QuoteBlockElement quote)
        {
            sb.AppendLine("<blockquote>");
            foreach (var block in quote.Blocks)
            {
                RenderBlock(sb, block);
            }

            sb.AppendLine("</blockquote>");
        }

        private void RenderTable(StringBuilder sb, TableElement table)
        {
            sb.AppendLine("<table>");
            if (table.HeaderRowCount > 0 && table.Rows.Count > 0)
            {
                sb.AppendLine("<thead>");
                for (var i = 0; i < table.HeaderRowCount && i < table.Rows.Count; i++)
                {
                    sb.AppendLine("<tr>");
                    foreach (var cell in table.Rows[i])
                    {
                        sb.Append("<th>");
                        sb.Append(HtmlEncode(cell));
                        sb.AppendLine("</th>");
                    }

                    sb.AppendLine("</tr>");
                }

                sb.AppendLine("</thead>");
            }

            if (table.Rows.Count > table.HeaderRowCount)
            {
                sb.AppendLine("<tbody>");
                for (var i = table.HeaderRowCount; i < table.Rows.Count; i++)
                {
                    sb.AppendLine("<tr>");
                    foreach (var cell in table.Rows[i])
                    {
                        sb.Append("<td>");
                        sb.Append(HtmlEncode(cell));
                        sb.AppendLine("</td>");
                    }

                    sb.AppendLine("</tr>");
                }

                sb.AppendLine("</tbody>");
            }

            sb.AppendLine("</table>");
        }

        private void RenderMathBlock(StringBuilder sb, MathBlockElement math)
        {
            sb.Append("<div class=\"math\">$$");
            sb.Append(HtmlEncode(math.Formula));
            sb.AppendLine("$$</div>");
        }

        private void RenderInlines(StringBuilder sb, List<InlineElement> inlines)
        {
            foreach (var inline in inlines)
            {
                RenderInline(sb, inline);
            }
        }

        private void RenderInline(StringBuilder sb, InlineElement inline)
        {
            if (inline is TextElement text)
            {
                sb.Append(HtmlEncode(text.Text));
            }
            else if (inline is BoldElement bold)
            {
                sb.Append("<strong>");
                RenderInlines(sb, bold.Children);
                sb.Append("</strong>");
            }
            else if (inline is ItalicElement italic)
            {
                sb.Append("<em>");
                RenderInlines(sb, italic.Children);
                sb.Append("</em>");
            }
            else if (inline is StrikethroughElement strike)
            {
                sb.Append("<del>");
                RenderInlines(sb, strike.Children);
                sb.Append("</del>");
            }
            else if (inline is CodeInlineElement code)
            {
                sb.Append("<code>");
                sb.Append(HtmlEncode(code.Code));
                sb.Append("</code>");
            }
            else if (inline is LinkElement link)
            {
                sb.Append("<a href=\"");
                sb.Append(HtmlAttributeEncode(link.Url));
                sb.Append("\">");
                sb.Append(HtmlEncode(link.Text));
                sb.Append("</a>");
            }
            else if (inline is ImageElement image)
            {
                sb.Append("<img src=\"");
                sb.Append(HtmlAttributeEncode(image.Url));
                sb.Append("\" alt=\"");
                sb.Append(HtmlAttributeEncode(image.AltText));
                sb.Append("\" />");
            }
            else if (inline is MathInlineElement math)
            {
                sb.Append("$");
                sb.Append(HtmlEncode(math.Formula));
                sb.Append("$");
            }
        }

        private static string HtmlEncode(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;");
        }

        private static string HtmlAttributeEncode(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&#39;");
        }
    }

    public sealed class OneNoteXmlRenderer
    {
        private const string OneNoteNamespace = "http://schemas.microsoft.com/office/onenote/2013/onenote";

        private static readonly Dictionary<int, double> HeadingFontSizes = new Dictionary<int, double>
        {
            { 1, 24.0 },
            { 2, 20.0 },
            { 3, 18.0 },
            { 4, 16.0 },
            { 5, 14.0 },
            { 6, 12.0 }
        };

        public string Render(MarkdownDocument doc, RendererStyle style)
        {
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
            sb.Append("<Page xmlns=\"");
            sb.Append(OneNoteNamespace);
            sb.AppendLine("\">");
            sb.AppendLine("<Outline>");

            foreach (var block in doc.Blocks)
            {
                RenderBlock(sb, block, style);
            }

            sb.AppendLine("</Outline>");
            sb.AppendLine("</Page>");
            return sb.ToString();
        }

        private void RenderBlock(StringBuilder sb, BlockElement block, RendererStyle style)
        {
            if (block is HeadingElement heading)
            {
                RenderHeading(sb, heading, style);
            }
            else if (block is ParagraphElement)
            {
                RenderParagraph(sb, block, style);
            }
            else if (block is CodeBlockElement codeBlock)
            {
                RenderCodeBlock(sb, codeBlock, style);
            }
            else if (block is DiagramBlockElement diagram)
            {
                RenderDiagram(sb, diagram, style);
            }
            else if (block is ListElement list)
            {
                RenderList(sb, list, style);
            }
            else if (block is TaskListElement taskList)
            {
                RenderTaskList(sb, taskList, style);
            }
            else if (block is QuoteBlockElement quote)
            {
                RenderQuoteBlock(sb, quote, style);
            }
            else if (block is TableElement table)
            {
                RenderTable(sb, table, style);
            }
            else if (block is HorizontalRuleElement)
            {
                RenderHorizontalRule(sb, style);
            }
            else if (block is MathBlockElement math)
            {
                RenderMathBlock(sb, math, style);
            }
        }

        private void RenderHeading(StringBuilder sb, HeadingElement heading, RendererStyle style)
        {
            double fontSize;
            if (!HeadingFontSizes.TryGetValue(heading.Level, out fontSize))
            {
                fontSize = style.FontSize;
            }

            sb.Append("<OE>");
            sb.Append("<T><![CDATA[<span style=\"font-family:");
            sb.Append(XmlEncode(style.FontFamily));
            sb.Append(";font-size:");
            sb.Append(fontSize.ToString("0"));
            sb.Append("pt;font-weight:bold\">");

            RenderInlinesAsCdata(sb, heading.Children, style);

            sb.Append("</span>]]></T>");
            sb.AppendLine("</OE>");
        }

        private void RenderParagraph(StringBuilder sb, BlockElement paragraph, RendererStyle style)
        {
            sb.Append("<OE>");
            sb.Append("<T><![CDATA[<span style=\"font-family:");
            sb.Append(XmlEncode(style.FontFamily));
            sb.Append(";font-size:");
            sb.Append(style.FontSize.ToString("0"));
            sb.Append("pt\">");

            RenderInlinesAsCdata(sb, paragraph.Children, style);

            sb.Append("</span>]]></T>");
            sb.AppendLine("</OE>");
        }

        private void RenderCodeBlock(StringBuilder sb, CodeBlockElement codeBlock, RendererStyle style)
        {
            sb.Append("<OE>");
            sb.Append("<T><![CDATA[<span style=\"font-family:");
            sb.Append(XmlEncode(style.CodeFontFamily));
            sb.Append(";font-size:10pt;background-color:");
            sb.Append(XmlEncode(style.CodeBackgroundColor));
            sb.Append(";color:");
            sb.Append(XmlEncode(style.CodeTextColor));
            sb.Append("\">");

            var code = codeBlock.Code;
            if (!code.EndsWith(Environment.NewLine) && !code.EndsWith("\n"))
            {
                code += Environment.NewLine;
            }

            sb.Append(XmlEncode(code));

            sb.Append("</span>]]></T>");
            sb.AppendLine("</OE>");
        }

        private void RenderDiagram(StringBuilder sb, DiagramBlockElement diagram, RendererStyle style)
        {
            sb.Append("<OE>");
            sb.Append("<T><![CDATA[<span style=\"font-family:");
            sb.Append(XmlEncode(style.CodeFontFamily));
            sb.Append(";font-size:10pt;background-color:");
            sb.Append(XmlEncode(style.CodeBackgroundColor));
            sb.Append(";color:");
            sb.Append(XmlEncode(style.CodeTextColor));
            sb.Append("\">");

            var code = diagram.Code;
            if (!code.EndsWith(Environment.NewLine) && !code.EndsWith("\n"))
            {
                code += Environment.NewLine;
            }

            sb.Append(XmlEncode(code));

            sb.Append("</span>]]></T>");
            sb.AppendLine("</OE>");
        }

        private void RenderList(StringBuilder sb, ListElement list, RendererStyle style)
        {
            var tag = list.IsOrdered ? "ol" : "ul";
            sb.Append("<OE>");
            sb.Append("<OEChildren>");
            foreach (var item in list.Items)
            {
                sb.Append("<OE>");
                sb.Append("<T><![CDATA[<span style=\"font-family:");
                sb.Append(XmlEncode(style.FontFamily));
                sb.Append(";font-size:");
                sb.Append(style.FontSize.ToString("0"));
                sb.Append("pt\">");
                sb.Append(XmlEncode(item.Content));
                sb.Append("</span>]]></T>");

                if (item.Children.Count > 0)
                {
                    sb.Append("<OEChildren>");
                    foreach (var child in item.Children)
                    {
                        sb.Append("<OE>");
                        sb.Append("<T><![CDATA[<span style=\"font-family:");
                        sb.Append(XmlEncode(style.FontFamily));
                        sb.Append(";font-size:");
                        sb.Append(style.FontSize.ToString("0"));
                        sb.Append("pt\">");
                        sb.Append(XmlEncode(child.Content));
                        sb.Append("</span>]]></T>");
                        sb.AppendLine("</OE>");
                    }

                    sb.Append("</OEChildren>");
                }

                sb.AppendLine("</OE>");
            }

            sb.Append("</OEChildren>");
            sb.AppendLine("</OE>");
        }

        private void RenderTaskList(StringBuilder sb, TaskListElement taskList, RendererStyle style)
        {
            sb.Append("<OE>");
            sb.Append("<OEChildren>");
            foreach (var item in taskList.Items)
            {
                sb.Append("<OE>");
                var checkMark = item.IsChecked ? "\u2611 " : "\u2610 ";
                sb.Append("<T><![CDATA[<span style=\"font-family:");
                sb.Append(XmlEncode(style.FontFamily));
                sb.Append(";font-size:");
                sb.Append(style.FontSize.ToString("0"));
                sb.Append("pt\">");
                sb.Append(XmlEncode(checkMark + item.Content));
                sb.Append("</span>]]></T>");
                sb.AppendLine("</OE>");
            }

            sb.Append("</OEChildren>");
            sb.AppendLine("</OE>");
        }

        private void RenderQuoteBlock(StringBuilder sb, QuoteBlockElement quote, RendererStyle style)
        {
            sb.Append("<OE>");
            sb.Append("<OEChildren>");
            foreach (var block in quote.Blocks)
            {
                RenderBlock(sb, block, style);
            }

            sb.Append("</OEChildren>");
            sb.AppendLine("</OE>");
        }

        private void RenderTable(StringBuilder sb, TableElement table, RendererStyle style)
        {
            sb.Append("<OE>");
            sb.Append("<Table>");

            for (var rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
            {
                sb.Append("<Row>");
                var row = table.Rows[rowIndex];
                for (var colIndex = 0; colIndex < row.Length; colIndex++)
                {
                    sb.Append("<Cell>");
                    sb.Append("<OEChildren>");
                    sb.Append("<OE>");
                    var isHeader = rowIndex < table.HeaderRowCount;
                    var fontStyle = isHeader
                        ? "font-weight:bold;"
                        : string.Empty;
                    sb.Append("<T><![CDATA[<span style=\"font-family:");
                    sb.Append(XmlEncode(style.FontFamily));
                    sb.Append(";");
                    sb.Append(fontStyle);
                    sb.Append("font-size:");
                    sb.Append(style.FontSize.ToString("0"));
                    sb.Append("pt\">");
                    sb.Append(XmlEncode(row[colIndex]));
                    sb.Append("</span>]]></T>");
                    sb.AppendLine("</OE>");
                    sb.Append("</OEChildren>");
                    sb.Append("</Cell>");
                }

                sb.AppendLine("</Row>");
            }

            sb.Append("</Table>");
            sb.AppendLine("</OE>");
        }

        private void RenderHorizontalRule(StringBuilder sb, RendererStyle style)
        {
            sb.Append("<OE>");
            sb.Append("<T><![CDATA[<span style=\"font-family:");
            sb.Append(XmlEncode(style.FontFamily));
            sb.Append(";font-size:");
            sb.Append(style.FontSize.ToString("0"));
            sb.Append("pt\">");
            sb.Append("────────────────────────");
            sb.Append("</span>]]></T>");
            sb.AppendLine("</OE>");
        }

        private void RenderMathBlock(StringBuilder sb, MathBlockElement math, RendererStyle style)
        {
            sb.Append("<OE>");
            sb.Append("<T><![CDATA[<span style=\"font-family:");
            sb.Append(XmlEncode(style.FontFamily));
            sb.Append(";font-size:");
            sb.Append(style.FontSize.ToString("0"));
            sb.Append("pt;font-style:italic\">");
            sb.Append(XmlEncode("$$" + math.Formula + "$$"));
            sb.Append("</span>]]></T>");
            sb.AppendLine("</OE>");
        }

        private void RenderInlinesAsCdata(StringBuilder sb, List<InlineElement> inlines, RendererStyle style)
        {
            foreach (var inline in inlines)
            {
                RenderInlineAsCdata(sb, inline, style);
            }
        }

        private void RenderInlineAsCdata(StringBuilder sb, InlineElement inline, RendererStyle style)
        {
            if (inline is TextElement text)
            {
                sb.Append(XmlEncode(text.Text));
            }
            else if (inline is BoldElement bold)
            {
                sb.Append("<span style=\"font-weight:bold\">");
                RenderInlinesAsCdata(sb, bold.Children, style);
                sb.Append("</span>");
            }
            else if (inline is ItalicElement italic)
            {
                sb.Append("<span style=\"font-style:italic\">");
                RenderInlinesAsCdata(sb, italic.Children, style);
                sb.Append("</span>");
            }
            else if (inline is StrikethroughElement strike)
            {
                sb.Append("<span style=\"text-decoration:line-through\">");
                RenderInlinesAsCdata(sb, strike.Children, style);
                sb.Append("</span>");
            }
            else if (inline is CodeInlineElement code)
            {
                sb.Append("<span style=\"font-family:");
                sb.Append(XmlEncode(style.CodeFontFamily));
                sb.Append(";background-color:");
                sb.Append(XmlEncode(style.CodeBackgroundColor));
                sb.Append("\">");
                sb.Append(XmlEncode(code.Code));
                sb.Append("</span>");
            }
            else if (inline is LinkElement link)
            {
                sb.Append("<a href=\"");
                sb.Append(XmlEncode(link.Url));
                sb.Append("\">");
                sb.Append(XmlEncode(link.Text));
                sb.Append("</a>");
            }
            else if (inline is ImageElement image)
            {
                sb.Append("<img src=\"");
                sb.Append(XmlEncode(image.Url));
                sb.Append("\" alt=\"");
                sb.Append(XmlEncode(image.AltText));
                sb.Append("\" />");
            }
            else if (inline is MathInlineElement math)
            {
                sb.Append("<span style=\"font-style:italic\">");
                sb.Append(XmlEncode("$" + math.Formula + "$"));
                sb.Append("</span>");
            }
        }

        private static string XmlEncode(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return string.Empty;
            }

            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }
    }
}
