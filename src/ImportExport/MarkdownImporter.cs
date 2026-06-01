using Markdig;
using OneMarkDotNet.MarkdownEngine;

namespace OneMarkDotNet.ImportExport;

public sealed class MarkdownImporter
{
    readonly FrontMatterParser _frontMatterParser = new();
    readonly ImageHandler _imageHandler = new();
    readonly MarkdownPipeline _pipeline = new MarkdownPipelineBuilder()
        .UseYamlFrontMatter()
        .UseAdvancedExtensions()
        .Build();

    public async Task<MarkdownDocument> ImportFromFile(string filePath)
    {
        if (!File.Exists(filePath))
            throw new FileNotFoundException($"Markdown file not found: {filePath}");

        var markdown = File.ReadAllText(filePath);
        var document = ImportFromText(markdown);
        document.SourceFilePath = Path.GetFullPath(filePath);

        var baseDirectory = Path.GetDirectoryName(Path.GetFullPath(filePath)) ?? string.Empty;
        document.Images = _imageHandler.ExtractImagesFromMarkdown(markdown, baseDirectory);

        return document;
    }

    public async Task<MarkdownDocument> ImportFromClipboard()
    {
        var markdown = await ClipboardHelper.GetText();
        return ImportFromText(markdown);
    }

    public MarkdownDocument ImportFromText(string markdownText)
    {
        var (frontMatter, body) = _frontMatterParser.Parse(markdownText);

        var document = new MarkdownDocument
        {
            RawContent = body,
            FrontMatter = frontMatter
        };

        ApplyFrontMatterToDocument(document);

        var parsed = Markdig.Markdown.Parse(body, _pipeline);
        document.Title = ExtractTitle(parsed) ?? ExtractTitleFromContent(body);

        return document;
    }

    void ApplyFrontMatterToDocument(MarkdownDocument document)
    {
        if (document.FrontMatter.TryGetValue("title", out var title))
            document.Title = title.ToString() ?? string.Empty;

        if (document.FrontMatter.TryGetValue("date", out var date))
        {
            if (date is DateTime dt)
                document.CreatedDate = dt;
            else if (DateTime.TryParse(date.ToString(), out var parsed))
                document.CreatedDate = parsed;
        }

        if (document.FrontMatter.TryGetValue("tags", out var tags))
        {
            document.Tags = tags switch
            {
                List<string> list => list,
                IList<string> iList => iList.ToList(),
                string s => s.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(x => x.Trim()).ToList(),
                _ => new List<string>()
            };
        }

        if (document.FrontMatter.TryGetValue("author", out var author))
            document.Author = author.ToString();
    }

    static string? ExtractTitle(Markdig.Syntax.MarkdownDocument parsed)
    {
        foreach (var block in parsed)
        {
            if (block is Markdig.Syntax.HeadingBlock heading && heading.Level == 1)
            {
                if (heading.Inline?.FirstChild is Markdig.Syntax.Inlines.LiteralInline literal)
                    return literal.Content.ToString();
            }
        }

        return null;
    }

    static string ExtractTitleFromContent(string body)
    {
        var lines = body.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("# "))
                return trimmed.Substring(2).Trim();
        }

        var firstLine = lines.FirstOrDefault()?.Trim() ?? string.Empty;
        return firstLine.Length > 100 ? firstLine.Substring(0, 100) : firstLine;
    }
}
