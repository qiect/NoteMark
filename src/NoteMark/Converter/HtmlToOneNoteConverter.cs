namespace NoteMark
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Xml.Linq;
    using NoteMark.Elements;

    public class HtmlToOneNoteConverter
    {
        private static readonly XNamespace Ns = OneNoteXmlBuilder.OneNoteNamespace;

        private readonly OneNoteXmlBuilder builder;

        public HtmlToOneNoteConverter()
        {
            builder = new OneNoteXmlBuilder();
        }

        public string Convert(List<BlockElement> elements, ConverterOneNoteStyle style)
        {
            var page = builder.CreatePage();

            var outline = builder.CreateOutline();
            var oeChildren = builder.CreateOEChildren();

            foreach (var element in elements)
            {
                var oe = ConvertBlockElement(element, style);
                if (oe != null)
                {
                    oeChildren.Add(oe);
                }
            }

            outline.Add(oeChildren);
            page.Add(outline);

            return page.ToString(SaveOptions.OmitDuplicateNamespaces);
        }

        public XElement ConvertBlockElement(BlockElement element, ConverterOneNoteStyle style)
        {
            if (element is HeadingElement heading)
            {
                return ConvertHeading(heading, style);
            }

            if (element is ParagraphElement paragraph)
            {
                return ConvertParagraph(paragraph, style);
            }

            if (element is CodeBlockElement codeBlock)
            {
                return ConvertCodeBlock(codeBlock, style);
            }

            if (element is TableElement table)
            {
                return ConvertTable(table, style);
            }

            if (element is ListElement list)
            {
                return ConvertList(list, style);
            }

            if (element is TaskListElement taskList)
            {
                return ConvertTaskList(taskList, style);
            }

            if (element is QuoteBlockElement quote)
            {
                return ConvertQuoteBlock(quote, style);
            }

            if (element is MathBlockElement math)
            {
                return ConvertMathBlock(math, style);
            }

            if (element is DiagramBlockElement diagram)
            {
                return ConvertDiagramBlock(diagram, style);
            }

            if (element is HorizontalRuleElement hr)
            {
                return ConvertHorizontalRule(hr, style);
            }

            return ConvertParagraph(new ParagraphElement(), style);
        }

        private XElement ConvertHeading(HeadingElement heading, ConverterOneNoteStyle style)
        {
            double fontSize = GetHeadingFontSize(heading.Level);
            var headingStyle = CreateDerivedStyle(style);
            headingStyle.FontSize = fontSize;
            headingStyle.IsBold = true;

            var oe = new XElement(Ns + "OE");

            var t = builder.CreateStyledT(
                GetInlineContent(heading.Children),
                headingStyle.IsBold,
                headingStyle.IsItalic,
                headingStyle.IsStrikethrough,
                headingStyle.FontFamily,
                headingStyle.FontSize);

            oe.Add(t);
            return oe;
        }

        private XElement ConvertParagraph(ParagraphElement paragraph, ConverterOneNoteStyle style)
        {
            var oe = new XElement(Ns + "OE");

            if (paragraph.Children == null || paragraph.Children.Count == 0)
            {
                oe.Add(builder.CreateT(string.Empty));
                return oe;
            }

            var content = ConvertInlineElements(paragraph.Children, style);
            var t = new XElement(Ns + "T", new XCData(content));
            oe.Add(t);
            return oe;
        }

        private XElement ConvertCodeBlock(CodeBlockElement codeBlock, ConverterOneNoteStyle style)
        {
            var codeStyle = CreateDerivedStyle(style);
            codeStyle.FontFamily = "Consolas";
            codeStyle.BackgroundColor = style != null ? style.BackgroundColor : "#F5F5F5";
            codeStyle.FontSize = 10.0;

            var lines = codeBlock.Code.Split('\n');
            var oeChildren = builder.CreateOEChildren();

            for (int i = 0; i < lines.Length; i++)
            {
                var lineText = lines[i].TrimEnd('\r');
                var numberedText = string.Format(CultureInfo.InvariantCulture,
                    "{0,4}  {1}", i + 1, lineText);

                var lineOe = new XElement(Ns + "OE");
                var t = builder.CreateStyledT(
                    numberedText,
                    codeStyle.IsBold,
                    codeStyle.IsItalic,
                    codeStyle.IsStrikethrough,
                    codeStyle.FontFamily,
                    codeStyle.FontSize);
                lineOe.Add(t);
                oeChildren.Add(lineOe);
            }

            var oe = new XElement(Ns + "OE");
            oe.Add(oeChildren);
            return oe;
        }

        private XElement ConvertTable(TableElement table, ConverterOneNoteStyle style)
        {
            var oe = new XElement(Ns + "OE");

            var rows = table.Rows;
            var columnCount = table.ColumnCount;
            if (rows == null || rows.Count == 0 || columnCount <= 0)
            {
                oe.Add(builder.CreateT(string.Empty));
                return oe;
            }

            var dataArray = rows.Select(r => r).ToArray();
            var tableElement = builder.CreateTable(rows.Count, columnCount, dataArray);
            oe.Add(tableElement);
            return oe;
        }

        private XElement ConvertList(ListElement list, ConverterOneNoteStyle style)
        {
            var oe = new XElement(Ns + "OE");
            var oeChildren = builder.CreateOEChildren();

            foreach (var item in list.Items)
            {
                var itemOe = ConvertListItem(item, list.IsOrdered, 0, style);
                oeChildren.Add(itemOe);
            }

            oe.Add(oeChildren);
            return oe;
        }

        private XElement ConvertListItem(ListItem item, bool isOrdered, int level, ConverterOneNoteStyle style)
        {
            var oe = new XElement(Ns + "OE");

            var bullet = isOrdered
                ? string.Format(CultureInfo.InvariantCulture, "{0}. ", level + 1)
                : "\u2022 ";
            var content = bullet + item.Content;

            var t = new XElement(Ns + "T", new XCData(content));
            oe.Add(t);

            if (item.Children != null && item.Children.Count > 0)
            {
                var childOeChildren = builder.CreateOEChildren();
                foreach (var child in item.Children)
                {
                    childOeChildren.Add(ConvertListItem(child, isOrdered, level + 1, style));
                }
                oe.Add(childOeChildren);
            }

            return oe;
        }

        private XElement ConvertTaskList(TaskListElement taskList, ConverterOneNoteStyle style)
        {
            var oe = new XElement(Ns + "OE");
            var oeChildren = builder.CreateOEChildren();

            foreach (var item in taskList.Items)
            {
                var itemOe = ConvertTaskItem(item, style);
                oeChildren.Add(itemOe);
            }

            oe.Add(oeChildren);
            return oe;
        }

        private XElement ConvertTaskItem(TaskItem item, ConverterOneNoteStyle style)
        {
            var oe = new XElement(Ns + "OE");

            var check = item.IsChecked ? "[\u2713]" : "[ ]";
            var content = check + " " + item.Content;

            var t = new XElement(Ns + "T", new XCData(content));
            oe.Add(t);

            if (item.Children != null && item.Children.Count > 0)
            {
                var childOeChildren = builder.CreateOEChildren();
                foreach (var child in item.Children)
                {
                    childOeChildren.Add(ConvertTaskItem(child, style));
                }
                oe.Add(childOeChildren);
            }

            return oe;
        }

        private XElement ConvertQuoteBlock(QuoteBlockElement quote, ConverterOneNoteStyle style)
        {
            var oe = new XElement(Ns + "OE");
            var oeChildren = builder.CreateOEChildren();

            var quoteStyle = CreateDerivedStyle(style);
            quoteStyle.IsItalic = true;

            if (quote.Blocks != null)
            {
                foreach (var block in quote.Blocks)
                {
                    var blockOe = ConvertBlockElement(block, quoteStyle);
                    if (blockOe != null)
                    {
                        var indentedOeChildren = builder.CreateOEChildren();
                        indentedOeChildren.Add(blockOe);
                        var indentedOe = new XElement(Ns + "OE");
                        indentedOe.Add(indentedOeChildren);
                        oeChildren.Add(indentedOe);
                    }
                }
            }

            oe.Add(oeChildren);
            return oe;
        }

        private XElement ConvertMathBlock(MathBlockElement math, ConverterOneNoteStyle style)
        {
            var oe = new XElement(Ns + "OE");
            var content = string.Format(CultureInfo.InvariantCulture, "$${0}$$", math.Formula);
            oe.Add(builder.CreateT(content));
            return oe;
        }

        private XElement ConvertDiagramBlock(DiagramBlockElement diagram, ConverterOneNoteStyle style)
        {
            var oe = new XElement(Ns + "OE");

            var diagramStyle = CreateDerivedStyle(style);
            diagramStyle.FontFamily = "Consolas";
            diagramStyle.FontSize = 10.0;

            var header = string.Format(CultureInfo.InvariantCulture,
                "[{0} Diagram]", diagram.DiagramType);
            var headerOe = new XElement(Ns + "OE");
            headerOe.Add(builder.CreateStyledT(header,
                diagramStyle.IsBold, diagramStyle.IsItalic, diagramStyle.IsStrikethrough,
                diagramStyle.FontFamily, diagramStyle.FontSize));

            var codeOe = new XElement(Ns + "OE");
            codeOe.Add(builder.CreateStyledT(diagram.Code,
                false, diagramStyle.IsItalic, diagramStyle.IsStrikethrough,
                diagramStyle.FontFamily, diagramStyle.FontSize));

            var oeChildren = builder.CreateOEChildren();
            oeChildren.Add(headerOe);
            oeChildren.Add(codeOe);
            oe.Add(oeChildren);
            return oe;
        }

        private XElement ConvertHorizontalRule(HorizontalRuleElement hr, ConverterOneNoteStyle style)
        {
            var oe = new XElement(Ns + "OE");
            oe.Add(builder.CreateT("\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500\u2500"));
            return oe;
        }

        private string ConvertInlineElements(List<InlineElement> elements, ConverterOneNoteStyle style)
        {
            if (elements == null || elements.Count == 0)
            {
                return string.Empty;
            }

            var parts = new List<string>();
            foreach (var element in elements)
            {
                parts.Add(ConvertInlineElement(element, style));
            }

            return string.Concat(parts);
        }

        private string ConvertInlineElement(InlineElement element, ConverterOneNoteStyle style)
        {
            if (element is TextElement text)
            {
                return EscapeHtml(text.Text);
            }

            if (element is BoldElement bold)
            {
                var content = ConvertInlineElements(bold.Children, style);
                return string.Format(CultureInfo.InvariantCulture,
                    "<span style='font-weight:bold'>{0}</span>", content);
            }

            if (element is ItalicElement italic)
            {
                var content = ConvertInlineElements(italic.Children, style);
                return string.Format(CultureInfo.InvariantCulture,
                    "<span style='font-style:italic'>{0}</span>", content);
            }

            if (element is StrikethroughElement strike)
            {
                var content = ConvertInlineElements(strike.Children, style);
                return string.Format(CultureInfo.InvariantCulture,
                    "<span style='text-decoration:line-through'>{0}</span>", content);
            }

            if (element is CodeInlineElement code)
            {
                var fontFamily = "Consolas";
                var bgColor = style != null && !string.IsNullOrEmpty(style.BackgroundColor)
                    ? style.BackgroundColor
                    : "#F0F0F0";
                return string.Format(CultureInfo.InvariantCulture,
                    "<span style='font-family:\"{0}\";background:{1}'>{2}</span>",
                    fontFamily, bgColor, EscapeHtml(code.Code));
            }

            if (element is LinkElement link)
            {
                return string.Format(CultureInfo.InvariantCulture,
                    "<a href=\"{0}\">{1}</a>",
                    EscapeHtml(link.Url), EscapeHtml(link.Text));
            }

            if (element is ImageElement image)
            {
                if (!string.IsNullOrEmpty(image.Data))
                {
                    return string.Format(CultureInfo.InvariantCulture,
                        "<img src=\"{0}\" alt=\"{1}\" />", image.Data, EscapeHtml(image.AltText));
                }

                return string.Format(CultureInfo.InvariantCulture,
                    "<img src=\"{0}\" alt=\"{1}\" />", EscapeHtml(image.Url), EscapeHtml(image.AltText));
            }

            if (element is MathInlineElement math)
            {
                return string.Format(CultureInfo.InvariantCulture,
                    "<span style='font-family:\"Cambria Math\"'>${0}$</span>",
                    EscapeHtml(math.Formula));
            }

            return string.Empty;
        }

        private string GetInlineContent(List<InlineElement> elements)
        {
            if (elements == null || elements.Count == 0)
            {
                return string.Empty;
            }

            var sb = new System.Text.StringBuilder();
            foreach (var element in elements)
            {
                if (element is TextElement text)
                {
                    sb.Append(text.Text);
                }
                else
                {
                    sb.Append(ConvertInlineElement(element, null));
                }
            }

            return sb.ToString();
        }

        private static double GetHeadingFontSize(int level)
        {
            switch (level)
            {
                case 1: return 24.0;
                case 2: return 20.0;
                case 3: return 18.0;
                case 4: return 16.0;
                case 5: return 14.0;
                case 6: return 12.0;
                default: return 24.0;
            }
        }

        private static ConverterOneNoteStyle CreateDerivedStyle(ConverterOneNoteStyle baseStyle)
        {
            if (baseStyle == null)
            {
                return new ConverterOneNoteStyle();
            }

            return new ConverterOneNoteStyle
            {
                FontFamily = baseStyle.FontFamily,
                FontSize = baseStyle.FontSize,
                IsBold = baseStyle.IsBold,
                IsItalic = baseStyle.IsItalic,
                IsStrikethrough = baseStyle.IsStrikethrough,
                ForegroundColor = baseStyle.ForegroundColor,
                HighlightColor = baseStyle.HighlightColor,
                BackgroundColor = baseStyle.BackgroundColor,
                LineHeight = baseStyle.LineHeight
            };
        }

        private static string EscapeHtml(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;");
        }
    }
}
