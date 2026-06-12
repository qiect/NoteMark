namespace OneMarkDotNet
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;
    using OneMarkDotNet.Elements;

    public sealed class MarkdownExporter
    {
        private readonly FrontMatterParser _frontMatterParser;
        private readonly OneNoteToMarkdownConverter _oneNoteConverter;

        public MarkdownExporter()
        {
            _frontMatterParser = new FrontMatterParser();
            _oneNoteConverter = new OneNoteToMarkdownConverter();
        }

        public async Task ExportToFileAsync(MarkdownDocument doc, string path)
        {
            if (doc == null)
            {
                throw new ArgumentNullException(nameof(doc));
            }

            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            var markdown = DocumentToMarkdown(doc);

            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using (var writer = new StreamWriter(path, false, new UTF8Encoding(false)))
            {
                await writer.WriteAsync(markdown).ConfigureAwait(false);
            }
        }

        public void ExportToClipboard(MarkdownDocument doc)
        {
            if (doc == null)
            {
                throw new ArgumentNullException(nameof(doc));
            }

            var markdown = DocumentToMarkdown(doc);
            ClipboardHelper.SetText(markdown);
        }

        public string ExportFromOneNotePage(string oneNoteXml)
        {
            if (string.IsNullOrEmpty(oneNoteXml))
            {
                return string.Empty;
            }

            return _oneNoteConverter.Convert(oneNoteXml);
        }

        public string DocumentToMarkdown(MarkdownDocument doc)
        {
            if (doc == null)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();

            if (doc.Metadata != null && doc.Metadata.Count > 0)
            {
                var metadata = new Dictionary<string, object>();
                foreach (var kvp in doc.Metadata)
                {
                    metadata[kvp.Key] = kvp.Value;
                }

                var frontMatter = _frontMatterParser.Serialize(metadata);
                if (!string.IsNullOrEmpty(frontMatter))
                {
                    sb.Append(frontMatter);
                    sb.AppendLine();
                }
            }

            for (var i = 0; i < doc.Blocks.Count; i++)
            {
                RenderBlock(sb, doc.Blocks[i]);
            }

            return sb.ToString();
        }

        private void RenderBlock(StringBuilder sb, BlockElement block)
        {
            if (block is HeadingElement heading)
            {
                RenderHeading(sb, heading);
            }
            else if (block is ParagraphElement paragraph)
            {
                RenderParagraph(sb, paragraph);
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
                RenderList(sb, list, 0);
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
                sb.AppendLine("---");
                sb.AppendLine();
            }
            else if (block is MathBlockElement math)
            {
                sb.Append("$$");
                sb.Append(math.Formula);
                sb.AppendLine("$$");
                sb.AppendLine();
            }
        }

        private void RenderHeading(StringBuilder sb, HeadingElement heading)
        {
            var prefix = new string('#', Math.Max(1, Math.Min(heading.Level, 6)));
            sb.Append(prefix);
            sb.Append(' ');
            RenderInlines(sb, heading.Children);
            sb.AppendLine();
            sb.AppendLine();
        }

        private void RenderParagraph(StringBuilder sb, ParagraphElement paragraph)
        {
            RenderInlines(sb, paragraph.Children);
            sb.AppendLine();
            sb.AppendLine();
        }

        private void RenderCodeBlock(StringBuilder sb, CodeBlockElement codeBlock)
        {
            sb.Append("```");
            if (!string.IsNullOrEmpty(codeBlock.Language))
            {
                sb.Append(codeBlock.Language);
            }

            sb.AppendLine();
            sb.AppendLine(codeBlock.Code);
            sb.AppendLine("```");
            sb.AppendLine();
        }

        private void RenderDiagram(StringBuilder sb, DiagramBlockElement diagram)
        {
            sb.Append("```");
            sb.Append(diagram.DiagramType.ToString().ToLowerInvariant());
            sb.AppendLine();
            sb.AppendLine(diagram.Code);
            sb.AppendLine("```");
            sb.AppendLine();
        }

        private void RenderList(StringBuilder sb, ListElement list, int indentLevel)
        {
            for (var i = 0; i < list.Items.Count; i++)
            {
                var item = list.Items[i];
                var indent = new string(' ', indentLevel * 2);

                if (list.IsOrdered)
                {
                    sb.Append(indent);
                    sb.Append(i + 1);
                    sb.Append(". ");
                }
                else
                {
                    sb.Append(indent);
                    sb.Append("- ");
                }

                sb.AppendLine(item.Content);

                if (item.Children.Count > 0)
                {
                    var innerList = new ListElement(list.IsOrdered, item.Children);
                    RenderList(sb, innerList, indentLevel + 1);
                }
            }

            if (indentLevel == 0)
            {
                sb.AppendLine();
            }
        }

        private void RenderTaskList(StringBuilder sb, TaskListElement taskList)
        {
            foreach (var item in taskList.Items)
            {
                sb.Append("- [");
                sb.Append(item.IsChecked ? "x" : " ");
                sb.Append("] ");
                sb.AppendLine(item.Content);
            }

            sb.AppendLine();
        }

        private void RenderQuoteBlock(StringBuilder sb, QuoteBlockElement quote)
        {
            var innerSb = new StringBuilder();
            foreach (var block in quote.Blocks)
            {
                RenderBlock(innerSb, block);
            }

            var innerText = innerSb.ToString().TrimEnd(Environment.NewLine.ToCharArray());
            var lines = innerText.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
            foreach (var line in lines)
            {
                sb.Append("> ");
                sb.AppendLine(line);
            }

            sb.AppendLine();
        }

        private void RenderTable(StringBuilder sb, TableElement table)
        {
            if (table.Rows.Count == 0)
            {
                return;
            }

            var maxCols = table.ColumnCount;
            if (maxCols == 0)
            {
                maxCols = table.Rows.Max(r => r.Length);
            }

            for (var rowIndex = 0; rowIndex < table.Rows.Count; rowIndex++)
            {
                var row = table.Rows[rowIndex];
                sb.Append("| ");
                for (var colIndex = 0; colIndex < maxCols; colIndex++)
                {
                    if (colIndex > 0)
                    {
                        sb.Append(" | ");
                    }

                    sb.Append(colIndex < row.Length ? row[colIndex] : string.Empty);
                }

                sb.AppendLine(" |");

                if (rowIndex == 0 && table.HeaderRowCount > 0)
                {
                    sb.Append("| ");
                    for (var colIndex = 0; colIndex < maxCols; colIndex++)
                    {
                        if (colIndex > 0)
                        {
                            sb.Append(" | ");
                        }

                        sb.Append("---");
                    }

                    sb.AppendLine(" |");
                }
            }

            sb.AppendLine();
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
                sb.Append(text.Text);
            }
            else if (inline is BoldElement bold)
            {
                sb.Append("**");
                RenderInlines(sb, bold.Children);
                sb.Append("**");
            }
            else if (inline is ItalicElement italic)
            {
                sb.Append("*");
                RenderInlines(sb, italic.Children);
                sb.Append("*");
            }
            else if (inline is StrikethroughElement strike)
            {
                sb.Append("~~");
                RenderInlines(sb, strike.Children);
                sb.Append("~~");
            }
            else if (inline is CodeInlineElement code)
            {
                sb.Append('`');
                sb.Append(code.Code);
                sb.Append('`');
            }
            else if (inline is LinkElement link)
            {
                sb.Append('[');
                sb.Append(link.Text);
                sb.Append("](");
                sb.Append(link.Url);
                sb.Append(')');
            }
            else if (inline is ImageElement image)
            {
                sb.Append("![");
                sb.Append(image.AltText);
                sb.Append("](");
                sb.Append(image.Url);
                sb.Append(')');
            }
            else if (inline is MathInlineElement math)
            {
                sb.Append('$');
                sb.Append(math.Formula);
                sb.Append('$');
            }
        }
    }
}
