using System.Xml.Linq;
using OneMarkDotNet.MarkdownEngine;
using OneMarkDotNet.ThemeManager;

namespace OneMarkDotNet.OneNoteConverter;

public sealed class OneNoteXmlConverter
{
    private const string OneNoteNamespace = "http://schemas.microsoft.com/office/onenote/2013/onenote";

    private readonly HtmlToOneNoteConverter _htmlConverter = new();
    private readonly OneNoteToMarkdownConverter _markdownConverter = new();
    private readonly OneNoteXmlBuilder _builder = new();

    public string ConvertToOneNoteXml(MarkdownDocument document, Theme theme)
    {
        var html = new MarkdownParser().ParseToHtml(document.RawContent);
        var outlineElements = _htmlConverter.ConvertHtmlToOutlineElements(html);
        ApplyThemeToElements(outlineElements, theme);

        var title = document.Title ?? "Untitled";
        var outlineXml = _builder.BuildOutline(outlineElements);
        var pageXml = _builder.BuildPage(title, outlineXml);

        return pageXml;
    }

    public string ConvertToOneNoteXml(string markdown, Theme theme)
    {
        var document = MarkdownDocument.Parse(markdown);
        return ConvertToOneNoteXml(document, theme);
    }

    public MarkdownDocument ConvertFromOneNoteXml(string xml)
    {
        var markdown = _markdownConverter.ConvertToMarkdown(xml);
        return MarkdownDocument.Parse(markdown);
    }

    public string ConvertFromOneNoteXmlToMarkdown(string xml)
    {
        return _markdownConverter.ConvertToMarkdown(xml);
    }

    public List<OutlineElement> ConvertToOneNoteElements(MarkdownDocument document, Theme theme)
    {
        var html = new MarkdownParser().ParseToHtml(document.RawContent);
        var elements = _htmlConverter.ConvertHtmlToOutlineElements(html);
        ApplyThemeToElements(elements, theme);
        return elements;
    }

    public string BuildPageFromElements(string title, IEnumerable<OutlineElement> elements)
    {
        var outlineXml = _builder.BuildOutline(elements);
        return _builder.BuildPage(title, outlineXml);
    }

    private static void ApplyThemeToElements(List<OutlineElement> elements, Theme theme)
    {
        foreach (var element in elements)
        {
            ApplyThemeToElement(element, theme);
            if (element.Children.Count > 0)
            {
                ApplyThemeToElements(element.Children, theme);
            }
        }
    }

    private static void ApplyThemeToElement(OutlineElement element, Theme theme)
    {
        if (element.Type == ElementType.CodeBlock)
        {
            element = element with
            {
                Style = element.Style with
                {
                    FontFamily = theme.CodeFontFamily ?? "Consolas",
                    FontColor = theme.CodeFontColor,
                    HighlightColor = theme.CodeBackgroundColor ?? "#F5F5F5"
                }
            };
        }
        else if (element.Type == ElementType.Heading)
        {
            element = element with
            {
                Style = element.Style with
                {
                    FontFamily = theme.HeadingFontFamily ?? theme.FontFamily ?? "Calibri",
                    FontColor = theme.HeadingFontColor
                }
            };
        }
        else if (element.Type == ElementType.Quote)
        {
            element = element with
            {
                Style = element.Style with
                {
                    FontColor = theme.QuoteFontColor ?? "#666666",
                    FontFamily = theme.FontFamily ?? "Calibri"
                }
            };
        }
        else
        {
            element = element with
            {
                Style = element.Style with
                {
                    FontFamily = theme.FontFamily ?? "Calibri",
                    FontColor = theme.FontColor
                }
            };
        }
    }
}
