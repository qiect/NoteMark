using Markdig;
using Markdig.Extensions.Mathematics;
using Markdig.Renderers;
using Markdig.Renderers.Html;
using Markdig.Syntax;

using MarkdigRenderer = Markdig.Renderers.IMarkdownRenderer;

namespace NoteMark.MarkdownEngine;

public class NoteMarkMathExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        if (!pipeline.Extensions.Contains<MathExtension>())
            pipeline.Extensions.Add(new MathExtension());
    }

    public void Setup(MarkdownPipeline pipeline, MarkdigRenderer renderer)
    {
        if (renderer is HtmlRenderer htmlRenderer)
        {
            htmlRenderer.ObjectRenderers.Insert(0, new MathBlockHtmlRenderer());
            htmlRenderer.ObjectRenderers.Insert(0, new MathInlineHtmlRenderer());
        }
    }

    private sealed class MathBlockHtmlRenderer : HtmlObjectRenderer<MathBlock>
    {
        protected override void Write(HtmlRenderer renderer, MathBlock obj)
        {
            renderer.Write("<div class=\"math\">");
            renderer.WriteEscape(obj.Lines.ToString());
            renderer.Write("</div>");
        }
    }

    private sealed class MathInlineHtmlRenderer : HtmlObjectRenderer<MathInline>
    {
        protected override void Write(HtmlRenderer renderer, MathInline obj)
        {
            renderer.Write("<span class=\"math inline\">");
            renderer.WriteEscape(obj.Content.ToString());
            renderer.Write("</span>");
        }
    }
}

public class NoteMarkDiagramExtension : IMarkdownExtension
{
    private static readonly HashSet<string> DiagramLanguages =
        new(StringComparer.OrdinalIgnoreCase) { "mermaid", "flow", "sequence", "mindmap" };

    public void Setup(MarkdownPipelineBuilder pipeline) { }

    public void Setup(MarkdownPipeline pipeline, MarkdigRenderer renderer)
    {
        if (renderer is HtmlRenderer htmlRenderer)
        {
            htmlRenderer.ObjectRenderers.Insert(0, new DiagramBlockHtmlRenderer());
        }
    }

    private sealed class DiagramBlockHtmlRenderer : HtmlObjectRenderer<FencedCodeBlock>
    {
        protected override void Write(HtmlRenderer renderer, FencedCodeBlock obj)
        {
            var language = obj.Info ?? "";
            if (DiagramLanguages.Contains(language))
            {
                var diagramType = language.ToLowerInvariant();
                renderer.Write($"<div class=\"diagram\" data-type=\"{diagramType}\">");
                renderer.WriteEscape(obj.Lines.ToString());
                renderer.Write("</div>");
                return;
            }

            renderer.Write("<pre><code");
            if (!string.IsNullOrEmpty(language))
                renderer.Write($" class=\"language-{language}\"");
            renderer.Write(">");
            renderer.WriteEscape(obj.Lines.ToString());
            renderer.Write("</code></pre>");
        }
    }
}

public class QuoteHeadingIconExtension : IMarkdownExtension
{
    public void Setup(MarkdownPipelineBuilder pipeline) { }

    public void Setup(MarkdownPipeline pipeline, MarkdigRenderer renderer)
    {
        if (renderer is HtmlRenderer htmlRenderer)
        {
            htmlRenderer.ObjectRenderers.Insert(0, new QuoteBlockWithIconRenderer());
        }
    }

    private sealed class QuoteBlockWithIconRenderer : HtmlObjectRenderer<QuoteBlock>
    {
        protected override void Write(HtmlRenderer renderer, QuoteBlock obj)
        {
            var hasHeadingIcon = obj.FirstOrDefault() is HeadingBlock;
            renderer.Write(hasHeadingIcon
                ? "<blockquote class=\"has-heading-icon\">\n"
                : "<blockquote>\n");
            renderer.WriteChildren(obj);
            renderer.Write("</blockquote>\n");
        }
    }
}

public class NoteMarkExtension : IMarkdownExtension
{
    private readonly IMarkdownExtension[] _extensions =
    [
        new NoteMarkMathExtension(),
        new NoteMarkDiagramExtension(),
        new QuoteHeadingIconExtension()
    ];

    public void Setup(MarkdownPipelineBuilder pipeline)
    {
        foreach (var ext in _extensions)
            ext.Setup(pipeline);
    }

    public void Setup(MarkdownPipeline pipeline, MarkdigRenderer renderer)
    {
        foreach (var ext in _extensions)
            ext.Setup(pipeline, renderer);
    }
}
