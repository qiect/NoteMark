namespace OneMarkDotNet
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Markdig.Extensions.Tables;
    using Markdig.Extensions.TaskLists;
    using Markdig.Syntax;
    using Markdig.Syntax.Inlines;
    using OneMarkDotNet.Elements;

    public sealed class MarkdownDocument
    {
        public List<BlockElement> Blocks { get; set; }
        public Dictionary<string, string> Metadata { get; set; }

        public MarkdownDocument()
        {
            Blocks = new List<BlockElement>();
            Metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        }

        public static MarkdownDocument FromMarkdigDocument(Markdig.Syntax.MarkdownDocument doc)
        {
            var document = new MarkdownDocument();

            foreach (var block in doc)
            {
                var element = ConvertBlock(block);
                if (element != null)
                {
                    document.Blocks.Add(element);
                }
            }

            return document;
        }

        internal static BlockElement ConvertBlock(Block block)
        {
            if (block is HeadingBlock heading)
            {
                return ConvertHeading(heading);
            }

            if (block is ParagraphBlock paragraph)
            {
                return ConvertParagraph(paragraph);
            }

            if (block is FencedCodeBlock fencedCode)
            {
                return ConvertFencedCode(fencedCode);
            }

            if (block is ListBlock listBlock)
            {
                return ConvertList(listBlock);
            }

            if (block is QuoteBlock quoteBlock)
            {
                return ConvertQuoteBlock(quoteBlock);
            }

            if (block is Table table)
            {
                return ConvertTable(table);
            }

            if (block is ThematicBreakBlock)
            {
                return new HorizontalRuleElement();
            }

            if (block is MathBlock mathBlock)
            {
                return new MathBlockElement(mathBlock.Content);
            }

            if (block is HtmlBlock htmlBlock)
            {
                var para = new ParagraphElement();
                para.Children.Add(new TextElement(htmlBlock.Lines.ToString()));
                return para;
            }

            return null;
        }

        private static HeadingElement ConvertHeading(HeadingBlock heading)
        {
            var element = new HeadingElement(heading.Level);
            if (heading.Inline != null)
            {
                foreach (var inline in heading.Inline)
                {
                    var inlineElement = ConvertInline(inline);
                    if (inlineElement != null)
                    {
                        element.Children.Add(inlineElement);
                    }
                }
            }

            return element;
        }

        private static ParagraphElement ConvertParagraph(ParagraphBlock paragraph)
        {
            var element = new ParagraphElement();
            if (paragraph.Inline != null)
            {
                foreach (var inline in paragraph.Inline)
                {
                    var inlineElement = ConvertInline(inline);
                    if (inlineElement != null)
                    {
                        element.Children.Add(inlineElement);
                    }
                }
            }

            return element;
        }

        private static BlockElement ConvertFencedCode(FencedCodeBlock fencedCode)
        {
            var language = fencedCode.Info ?? string.Empty;
            var lines = new List<string>();
            foreach (var line in fencedCode.Lines)
            {
                lines.Add(line.ToString());
            }

            var code = string.Join(Environment.NewLine, lines);

            var diagramLanguages = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "mermaid", "flow", "flowchart", "sequence", "mindmap"
            };

            if (diagramLanguages.Contains(language))
            {
                return new DiagramBlockElement(DiagramBlockElement.ParseDiagramType(language), code);
            }

            return new CodeBlockElement(language, code);
        }

        private static BlockElement ConvertList(ListBlock listBlock)
        {
            var isTaskList = false;
            var taskItems = new List<TaskItem>();
            var listItems = new List<ListItem>();

            foreach (var child in listBlock)
            {
                if (child is ListItemBlock listItemBlock)
                {
                    var contentBuilder = new List<InlineElement>();
                    var subBlocks = new List<BlockElement>();

                    foreach (var subBlock in listItemBlock)
                    {
                        if (subBlock is ParagraphBlock para)
                        {
                            if (para.Inline != null)
                            {
                                foreach (var inline in para.Inline)
                                {
                                    if (inline is TaskList taskListInline)
                                    {
                                        isTaskList = true;
                                        continue;
                                    }

                                    var inlineElement = ConvertInline(inline);
                                    if (inlineElement != null)
                                    {
                                        contentBuilder.Add(inlineElement);
                                    }
                                }
                            }
                        }
                        else
                        {
                            var subElement = ConvertBlock(subBlock);
                            if (subElement != null)
                            {
                                subBlocks.Add(subElement);
                            }
                        }
                    }

                    var contentText = string.Join(string.Empty, contentBuilder.Select(i =>
                        i is TextElement t ? t.Text : string.Empty));

                    if (isTaskList)
                    {
                        var isChecked = false;
                        foreach (var inline in listItemBlock.Descendants())
                        {
                            if (inline is TaskList taskInline)
                            {
                                isChecked = taskInline.Checked;
                                break;
                            }
                        }

                        taskItems.Add(new TaskItem(contentText, isChecked));
                    }
                    else
                    {
                        listItems.Add(new ListItem(contentText));
                    }
                }
            }

            if (isTaskList)
            {
                return new TaskListElement(taskItems);
            }

            return new ListElement(listBlock.IsOrdered, listItems);
        }

        private static QuoteBlockElement ConvertQuoteBlock(QuoteBlock quoteBlock)
        {
            var blocks = new List<BlockElement>();
            foreach (var child in quoteBlock)
            {
                var element = ConvertBlock(child);
                if (element != null)
                {
                    blocks.Add(element);
                }
            }

            return new QuoteBlockElement(blocks);
        }

        private static TableElement ConvertTable(Table table)
        {
            var rows = new List<string[]>();
            var columnCount = table.ColumnDefinitions.Count;
            var headerRowCount = 0;

            foreach (var rowObj in table)
            {
                if (rowObj is TableRow row)
                {
                    if (row.IsHeader)
                    {
                        headerRowCount++;
                    }

                    var cells = new string[columnCount];
                    for (var i = 0; i < columnCount; i++)
                    {
                        cells[i] = string.Empty;
                    }

                    for (var cellIndex = 0; cellIndex < row.Count && cellIndex < columnCount; cellIndex++)
                    {
                        var cellBlock = row[cellIndex];
                        var cellContent = new List<string>();
                        if (cellBlock is TableCell cell)
                        {
                            foreach (var block in cell)
                            {
                                if (block is ParagraphBlock para && para.Inline != null)
                                {
                                    foreach (var inline in para.Inline)
                                    {
                                        cellContent.Add(ExtractText(inline));
                                    }
                                }
                            }
                        }

                        cells[cellIndex] = string.Join(" ", cellContent);
                    }

                    rows.Add(cells);
                }
            }

            return new TableElement(rows, headerRowCount, columnCount);
        }

        internal static InlineElement ConvertInline(Inline inline)
        {
            if (inline is LiteralInline literal)
            {
                return new TextElement(literal.Content.ToString());
            }

            if (inline is EmphasisInline emphasis)
            {
                var children = new List<InlineElement>();
                foreach (var child in emphasis)
                {
                    var childElement = ConvertInline(child);
                    if (childElement != null)
                    {
                        children.Add(childElement);
                    }
                }

                if (emphasis.DelimiterCount == 2)
                {
                    if (emphasis.DelimiterChar == '~')
                    {
                        return new StrikethroughElement(children);
                    }

                    return new BoldElement(children);
                }

                if (emphasis.DelimiterChar == '~')
                {
                    return new StrikethroughElement(children);
                }

                return new ItalicElement(children);
            }

            if (inline is CodeInline codeInline)
            {
                return new CodeInlineElement(codeInline.Content.ToString());
            }

            if (inline is LinkInline linkInline)
            {
                if (linkInline.IsImage)
                {
                    var altText = linkInline.Label ?? string.Empty;
                    var url = linkInline.Url ?? string.Empty;
                    return new ImageElement(altText, url);
                }

                var textBuilder = new List<string>();
                foreach (var child in linkInline)
                {
                    textBuilder.Add(ExtractText(child));
                }

                var linkText = string.Join(string.Empty, textBuilder);
                return new LinkElement(linkText, linkInline.Url ?? string.Empty);
            }

            if (inline is LineBreakInline lineBreak)
            {
                if (lineBreak.IsHard)
                {
                    return new TextElement(Environment.NewLine);
                }

                return new TextElement(" ");
            }

            if (inline is HtmlInline htmlInline)
            {
                return new TextElement(htmlInline.Tag);
            }

            if (inline is MathInline mathInline)
            {
                return new MathInlineElement(mathInline.Content);
            }

            return null;
        }

        private static string ExtractText(Inline inline)
        {
            if (inline is LiteralInline literal)
            {
                return literal.Content.ToString();
            }

            if (inline is CodeInline codeInline)
            {
                return codeInline.Content.ToString();
            }

            if (inline is EmphasisInline emphasis)
            {
                var parts = new List<string>();
                foreach (var child in emphasis)
                {
                    parts.Add(ExtractText(child));
                }

                return string.Join(string.Empty, parts);
            }

            if (inline is LinkInline linkInline)
            {
                if (linkInline.IsImage)
                {
                    return linkInline.Label ?? string.Empty;
                }

                var parts = new List<string>();
                foreach (var child in linkInline)
                {
                    parts.Add(ExtractText(child));
                }

                return string.Join(string.Empty, parts);
            }

            if (inline is LineBreakInline lineBreak)
            {
                if (lineBreak.IsHard)
                {
                    return Environment.NewLine;
                }

                return " ";
            }

            if (inline is HtmlInline htmlInline)
            {
                return htmlInline.Tag;
            }

            if (inline is MathInline mathInline)
            {
                return "$" + mathInline.Content + "$";
            }

            return string.Empty;
        }
    }
}
