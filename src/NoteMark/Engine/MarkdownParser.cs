namespace NoteMark
{
    using Markdig;
    using NoteMark.Elements;

    public sealed class MarkdownParser
    {
        private readonly MarkdownPipeline _pipeline;

        public MarkdownParser()
        {
            _pipeline = new MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .UseEmojiAndSmiley()
                .UseYamlFrontMatter()
                .UseMathBlocks()
                .UseDiagrams()
                .Build();
        }

        public MarkdownDocument Parse(string markdown)
        {
            var markdigDoc = Markdig.Markdown.Parse(markdown, _pipeline);
            return MarkdownDocument.FromMarkdigDocument(markdigDoc);
        }

        public string ToHtml(string markdown)
        {
            return Markdig.Markdown.ToHtml(markdown, _pipeline);
        }
    }
}
