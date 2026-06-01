using Markdig;
using NoteMark.MarkdownEngine.Elements;

namespace NoteMark.MarkdownEngine;

public class MarkdownParser
{
    private readonly MarkdownPipeline _pipeline;

    public MarkdownParser()
    {
        _pipeline = new MarkdownPipelineBuilder()
            .UsePipeTables()
            .UseTaskLists()
            .UseAutoLinks()
            .UseEmojiAndSmiley()
            .UseEmphasisExtras()
            .Use(new NoteMarkExtension())
            .Build();
    }

    public MarkdownDocument Parse(string markdown)
    {
        var doc = Markdig.Markdown.Parse(markdown, _pipeline);
        return MarkdownDocument.FromMarkdigDocument(doc);
    }

    public string ParseToHtml(string markdown)
    {
        return Markdig.Markdown.ToHtml(markdown, _pipeline);
    }
}
