namespace OneMarkDotNet
{
    using System.Collections.Generic;
    using OneMarkDotNet.Elements;

    public class OneNoteXmlConverter
    {
        private readonly HtmlToOneNoteConverter htmlConverter;
        private readonly OneNoteToMarkdownConverter markdownConverter;

        public OneNoteXmlConverter()
        {
            htmlConverter = new HtmlToOneNoteConverter();
            markdownConverter = new OneNoteToMarkdownConverter();
        }

        public string ConvertToOneNoteXml(MarkdownDocument doc, ConverterOneNoteStyle style)
        {
            if (doc == null)
            {
                return string.Empty;
            }

            var elements = doc.Blocks;
            if (elements == null || elements.Count == 0)
            {
                return string.Empty;
            }

            ApplyThemeToElements(elements, style);

            return htmlConverter.Convert(elements, style);
        }

        public string ConvertToMarkdown(string oneNoteXml)
        {
            if (string.IsNullOrWhiteSpace(oneNoteXml))
            {
                return string.Empty;
            }

            return markdownConverter.Convert(oneNoteXml);
        }

        public void ApplyThemeToElements(List<BlockElement> elements, ConverterOneNoteStyle style)
        {
            if (elements == null || style == null)
            {
                return;
            }

            foreach (var element in elements)
            {
                ApplyThemeToBlockElement(element, style);
            }
        }

        private void ApplyThemeToBlockElement(BlockElement element, ConverterOneNoteStyle style)
        {
            if (element is HeadingElement heading)
            {
                var headingStyle = CreateDerivedStyle(style);
                headingStyle.IsBold = true;
                ApplyThemeToInlineElements(heading.Children, headingStyle);
            }
            else if (element is ParagraphElement paragraph)
            {
                ApplyThemeToInlineElements(paragraph.Children, style);
            }
            else if (element is CodeBlockElement)
            {
                // Code blocks use monospace font and background color from theme
            }
            else if (element is TableElement)
            {
                // Tables inherit base style
            }
            else if (element is ListElement list)
            {
                ApplyThemeToListItems(list.Items, style);
            }
            else if (element is TaskListElement taskList)
            {
                ApplyThemeToTaskItems(taskList.Items, style);
            }
            else if (element is QuoteBlockElement quote)
            {
                var quoteStyle = CreateDerivedStyle(style);
                quoteStyle.IsItalic = true;
                if (quote.Blocks != null)
                {
                    foreach (var block in quote.Blocks)
                    {
                        ApplyThemeToBlockElement(block, quoteStyle);
                    }
                }
            }
            else if (element is MathBlockElement)
            {
                // Math blocks use specific font
            }
            else if (element is DiagramBlockElement)
            {
                // Diagram blocks use monospace font
            }
            else if (element is HorizontalRuleElement)
            {
                // Horizontal rules have no themed content
            }
        }

        private void ApplyThemeToInlineElements(List<InlineElement> elements, ConverterOneNoteStyle style)
        {
            if (elements == null || style == null)
            {
                return;
            }

            foreach (var element in elements)
            {
                if (element is BoldElement bold)
                {
                    ApplyThemeToInlineElements(bold.Children, style);
                }
                else if (element is ItalicElement italic)
                {
                    ApplyThemeToInlineElements(italic.Children, style);
                }
                else if (element is StrikethroughElement strike)
                {
                    ApplyThemeToInlineElements(strike.Children, style);
                }
                else if (element is CodeInlineElement)
                {
                    // Code inline uses monospace font and background from theme
                }
                else if (element is LinkElement)
                {
                    // Links use standard styling
                }
                else if (element is ImageElement)
                {
                    // Images have no text styling
                }
                else if (element is MathInlineElement)
                {
                    // Math inline uses specific font
                }
            }
        }

        private void ApplyThemeToListItems(List<ListItem> items, ConverterOneNoteStyle style)
        {
            if (items == null)
            {
                return;
            }

            foreach (var item in items)
            {
                ApplyThemeToListItems(item.Children, style);
            }
        }

        private void ApplyThemeToTaskItems(List<TaskItem> items, ConverterOneNoteStyle style)
        {
            if (items == null)
            {
                return;
            }

            foreach (var item in items)
            {
                ApplyThemeToTaskItems(item.Children, style);
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
    }
}
