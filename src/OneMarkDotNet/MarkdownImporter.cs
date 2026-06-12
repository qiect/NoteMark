namespace OneMarkDotNet
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;

    public sealed class MarkdownImporter
    {
        private readonly MarkdownParser _parser;
        private readonly FrontMatterParser _frontMatterParser;

        public MarkdownImporter()
        {
            _parser = new MarkdownParser();
            _frontMatterParser = new FrontMatterParser();
        }

        public async Task<MarkdownDocument> ImportFromFileAsync(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                throw new ArgumentNullException(nameof(path));
            }

            if (!File.Exists(path))
            {
                throw new FileNotFoundException("Markdown file not found.", path);
            }

            string text;
            using (var reader = new StreamReader(path, Encoding.UTF8))
            {
                text = await reader.ReadToEndAsync().ConfigureAwait(false);
            }

            return ImportFromText(text);
        }

        public MarkdownDocument ImportFromClipboard()
        {
            var text = ClipboardHelper.GetText();
            return ImportFromText(text);
        }

        public MarkdownDocument ImportFromText(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return new MarkdownDocument();
            }

            var parseResult = _frontMatterParser.Parse(text);
            var metadata = parseResult.Item1;
            var content = parseResult.Item2;

            var doc = _parser.Parse(content);

            foreach (var kvp in metadata)
            {
                doc.Metadata[kvp.Key] = kvp.Value?.ToString() ?? string.Empty;
            }

            return doc;
        }
    }
}
