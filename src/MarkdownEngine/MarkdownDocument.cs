using System.Text;
using Markdig.Extensions.Mathematics;
using Markdig.Extensions.TaskLists;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using NoteMark.MarkdownEngine.Elements;

using MarkdigTable = Markdig.Extensions.Tables;

namespace NoteMark.MarkdownEngine;

public class MarkdownDocument
{
    public List<BlockElement> Blocks { get; set; } = [];

    public string Title { get; set; } = string.Empty;
    public string RawContent { get; set; } = string.Empty;
    public Dictionary<string, object> FrontMatter { get; set; } = [];
    public List<MarkdownImage> Images { get; set; } = [];
    public string? SourceFilePath { get; set; }
    public DateTime? CreatedDate { get; set; }
    public DateTime? ModifiedDate { get; set; }
    public List<string> Tags { get; set; } = [];
    public string? Author { get; set; }

    public static MarkdownDocument Parse(string markdown)
    {
        return new MarkdownParser().Parse(markdown);
    }

    public static MarkdownDocument FromMarkdigDocument(Markdig.Syntax.MarkdownDocument doc)
    {
        var document = new MarkdownDocument();
        foreach (var block in doc)
        {
            var element = ConvertBlock(block);
            if (element is not null)
                document.Blocks.Add(element);
        }
        return document;
    }

    private static BlockElement? ConvertBlock(Block block)
    {
        return block switch
        {
            HeadingBlock h => ConvertHeading(h),
            ParagraphBlock p => ConvertParagraph(p),
            MathBlock m => ConvertMathBlock(m),
            FencedCodeBlock c => ConvertFencedCodeBlock(c),
            QuoteBlock q => ConvertQuoteBlock(q),
            MarkdigTable.Table t => ConvertTable(t),
            ListBlock l => ConvertList(l),
            ThematicBreakBlock => new HorizontalRuleElement(),
            _ => null
        };
    }

    private static HeadingElement ConvertHeading(HeadingBlock heading)
    {
        var element = new HeadingElement { Level = heading.Level };
        if (heading.Inline is not null)
            element.Inlines = ConvertInlines(heading.Inline);
        return element;
    }

    private static ParagraphElement ConvertParagraph(ParagraphBlock paragraph)
    {
        var element = new ParagraphElement();
        if (paragraph.Inline is not null)
            element.Inlines = ConvertInlines(paragraph.Inline);
        return element;
    }

    private static BlockElement ConvertFencedCodeBlock(FencedCodeBlock code)
    {
        var language = code.Info ?? "";
        var codeText = code.Lines.ToString();
        var diagramType = GetDiagramType(language);

        if (diagramType is not null)
        {
            return new DiagramBlockElement
            {
                DiagramType = diagramType.Value,
                Content = codeText
            };
        }

        return new CodeBlockElement
        {
            Language = language,
            Code = codeText,
            LineNumbers = Enumerable.Range(code.Line + 1, code.Lines.Count).ToList()
        };
    }

    private static DiagramType? GetDiagramType(string language)
    {
        return language.ToLowerInvariant() switch
        {
            "mermaid" => DiagramType.Mermaid,
            "flow" => DiagramType.Flow,
            "sequence" => DiagramType.Sequence,
            "mindmap" => DiagramType.Mindmap,
            _ => null
        };
    }

    private static QuoteBlockElement ConvertQuoteBlock(QuoteBlock quote)
    {
        var element = new QuoteBlockElement();
        foreach (var child in quote)
        {
            var childElement = ConvertBlock(child);
            if (childElement is not null)
                element.Children.Add(childElement);
        }

        if (element.Children.FirstOrDefault() is HeadingElement)
            element.HasHeadingIcon = true;

        return element;
    }

    private static TableElement ConvertTable(MarkdigTable.Table table)
    {
        var element = new TableElement();

        foreach (var columnDef in table.ColumnDefinitions)
        {
            element.Alignments.Add(columnDef.Alignment switch
            {
                MarkdigTable.TableColumnAlign.Center => TableColumnAlign.Center,
                MarkdigTable.TableColumnAlign.Right => TableColumnAlign.Right,
                _ => TableColumnAlign.Left
            });
        }

        var isFirstRow = true;
        foreach (MarkdigTable.TableRow row in table)
        {
            var cells = new List<List<InlineElement>>();
            foreach (MarkdigTable.TableCell cell in row)
            {
                var cellInlines = new List<InlineElement>();
                foreach (var block in cell)
                {
                    if (block is ParagraphBlock p && p.Inline is not null)
                        cellInlines.AddRange(ConvertInlines(p.Inline));
                }
                cells.Add(cellInlines);
            }

            if (isFirstRow)
            {
                element.Headers = cells;
                isFirstRow = false;
            }
            else
            {
                element.Rows.Add(new TableRow { Cells = cells });
            }
        }

        return element;
    }

    private static BlockElement ConvertList(ListBlock list)
    {
        var isOrdered = list.IsOrdered;
        var items = new List<ListItem>();
        var isTaskList = false;

        foreach (ListItemBlock item in list)
        {
            var listItem = new ListItem();
            foreach (var block in item)
            {
                if (block is ParagraphBlock p)
                {
                    if (p.Inline is not null)
                    {
                        foreach (var inline in p.Inline)
                        {
                            if (inline is TaskList taskItem)
                            {
                                isTaskList = true;
                                listItem.IsChecked = taskItem.Checked;
                                continue;
                            }
                            var converted = ConvertInline(inline);
                            if (converted is not null)
                                listItem.Content.Add(converted);
                        }
                    }
                }
                else if (block is ListBlock nestedList)
                {
                    var nestedElement = ConvertList(nestedList);
                    if (nestedElement is ListElement listElement)
                        listItem.NestedLists.Add(listElement);
                }
            }
            items.Add(listItem);
        }

        if (isTaskList)
        {
            return new TaskListElement
            {
                Items = items.Select(i => new TaskListItem
                {
                    IsChecked = i.IsChecked,
                    Content = i.Content
                }).ToList()
            };
        }

        return new ListElement
        {
            Items = items,
            IsOrdered = isOrdered,
            IsTaskList = false
        };
    }

    private static MathBlockElement ConvertMathBlock(MathBlock math)
    {
        return new MathBlockElement
        {
            Formula = math.Lines.ToString(),
            IsInline = false
        };
    }

    private static List<InlineElement> ConvertInlines(ContainerInline container)
    {
        var result = new List<InlineElement>();
        foreach (var inline in container)
        {
            var element = ConvertInline(inline);
            if (element is not null)
                result.Add(element);
        }
        return result;
    }

    private static InlineElement? ConvertInline(Inline inline)
    {
        return inline switch
        {
            LiteralInline literal => new TextElement { Text = literal.Content.ToString() },
            EmphasisInline emphasis => ConvertEmphasis(emphasis),
            CodeInline code => new CodeInlineElement { Code = code.Content.ToString() },
            LinkInline link => ConvertLink(link),
            AutolinkInline autolink => new LinkElement
            {
                Url = autolink.Url ?? "",
                Text = autolink.Url ?? ""
            },
            MathInline math => new MathInlineElement { Formula = math.Content.ToString() },
            LineBreakInline => new TextElement { Text = "\n" },
            _ => null
        };
    }

    private static InlineElement ConvertEmphasis(EmphasisInline emphasis)
    {
        var children = ConvertInlines(emphasis);

        if (emphasis.DelimiterChar == '~')
            return new StrikethroughElement { Inlines = children };

        if (emphasis.DelimiterCount >= 2)
            return new BoldElement { Inlines = children };

        return new ItalicElement { Inlines = children };
    }

    private static InlineElement ConvertLink(LinkInline link)
    {
        if (link.IsImage)
        {
            return new ImageElement
            {
                Url = link.Url ?? "",
                Alt = GetInlineText(link),
                Title = link.Title ?? ""
            };
        }

        return new LinkElement
        {
            Url = link.Url ?? "",
            Text = GetInlineText(link),
            Title = link.Title ?? ""
        };
    }

    private static string GetInlineText(ContainerInline container)
    {
        var sb = new StringBuilder();
        foreach (var inline in container)
        {
            if (inline is LiteralInline literal)
                sb.Append(literal.Content.ToString());
        }
        return sb.ToString();
    }
}
