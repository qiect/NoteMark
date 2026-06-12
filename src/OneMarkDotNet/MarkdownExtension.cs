namespace OneMarkDotNet
{
    using System;
    using System.Collections.Generic;
    using Markdig;
    using Markdig.Helpers;
    using Markdig.Parsers;
    using Markdig.Parsers.Inlines;
    using Markdig.Renderers;
    using Markdig.Syntax;
    using Markdig.Syntax.Inlines;

    public sealed class MathBlock : Block
    {
        public string Content { get; set; }

        public MathBlock(BlockParser parser) : base(parser)
        {
            Content = string.Empty;
        }
    }

    public sealed class MathInline : Inline
    {
        public string Content { get; set; }

        public MathInline(string content) : base()
        {
            Content = content;
        }
    }

    public sealed class MathBlockParser : BlockParser
    {
        private const string OpeningIdentifier = "$$";

        public MathBlockParser()
        {
            OpeningCharacters = new[] { '$' };
        }

        public override BlockState TryOpen(BlockProcessor processor)
        {
            if (processor.IsCodeIndent)
            {
                return BlockState.None;
            }

            var line = processor.Line;
            var start = line.Start;
            var end = line.End;

            var count = 0;
            while (start <= end && line[start] == '$')
            {
                count++;
                start++;
            }

            if (count < 2)
            {
                return BlockState.None;
            }

            var mathBlock = new MathBlock(this)
            {
                Column = processor.Column,
                Span = new SourceSpan(line.Start, start - 1)
            };

            processor.NewBlocks.Push(mathBlock);

            var remainingText = line.Text;
            var remainingLength = end - start + 1;
            var remainingContent = remainingLength > 0
                ? remainingText.Substring(start, remainingLength).Trim()
                : string.Empty;

            if (!string.IsNullOrEmpty(remainingContent))
            {
                mathBlock.Content = remainingContent + Environment.NewLine;
            }

            line.Start = end + 1;

            return BlockState.ContinueDiscard;
        }

        public override BlockState TryContinue(BlockProcessor processor, Block block)
        {
            var line = processor.Line;
            var start = line.Start;
            var end = line.End;

            var text = line.Text;
            var sliceLength = end - start + 1;
            var sliceText = sliceLength > 0 ? text.Substring(start, sliceLength).Trim() : string.Empty;

            if (sliceText.StartsWith("$$"))
            {
                var contentBeforeClosing = sliceText.Length > 2 ? sliceText.Substring(2) : string.Empty;
                if (!string.IsNullOrWhiteSpace(contentBeforeClosing))
                {
                    var mathBlock = (MathBlock)block;
                    mathBlock.Content += contentBeforeClosing;
                }

                block.UpdateSpanEnd(end);
                return BlockState.BreakDiscard;
            }

            var mathBlock2 = (MathBlock)block;
            if (!string.IsNullOrEmpty(sliceText))
            {
                mathBlock2.Content += sliceText + Environment.NewLine;
            }
            else
            {
                mathBlock2.Content += Environment.NewLine;
            }

            return BlockState.ContinueDiscard;
        }
    }

    public sealed class MathInlineParser : InlineParser
    {
        public MathInlineParser()
        {
            OpeningCharacters = new[] { '$' };
        }

        public override bool Match(InlineProcessor processor, ref StringSlice slice)
        {
            var match = slice.CurrentChar;
            if (match != '$')
            {
                return false;
            }

            var startPosition = slice.Start;
            var nextChar = slice.PeekChar(1);

            if (nextChar == '$')
            {
                return false;
            }

            if (char.IsWhiteSpace(nextChar))
            {
                return false;
            }

            var endPosition = -1;
            var searchPos = slice.Start + 1;
            var text = slice.Text;

            while (searchPos < text.Length)
            {
                if (text[searchPos] == '$')
                {
                    var charBefore = searchPos > 0 ? text[searchPos - 1] : '\0';
                    if (!char.IsWhiteSpace(charBefore))
                    {
                        endPosition = searchPos;
                        break;
                    }
                }

                searchPos++;
            }

            if (endPosition < 0)
            {
                return false;
            }

            var contentStart = startPosition + 1;
            var contentEnd = endPosition - 1;
            var content = text.Substring(contentStart, contentEnd - contentStart + 1);

            if (string.IsNullOrEmpty(content))
            {
                return false;
            }

            var inline = new MathInline(content);
            inline.Span = new SourceSpan(startPosition, endPosition);
            inline.Line = processor.LineIndex;

            processor.Inline = inline;
            slice.Start = endPosition + 1;

            return true;
        }
    }

    public sealed class MathExtension : IMarkdownExtension
    {
        public void Setup(MarkdownPipelineBuilder pipeline)
        {
            if (!pipeline.BlockParsers.Contains<MathBlockParser>())
            {
                pipeline.BlockParsers.InsertBefore<FencedCodeBlockParser>(new MathBlockParser());
            }

            var inlineParser = pipeline.InlineParsers.Find<EmphasisInlineParser>();
            if (inlineParser != null)
            {
                pipeline.InlineParsers.InsertBefore<EmphasisInlineParser>(new MathInlineParser());
            }
            else
            {
                pipeline.InlineParsers.Add(new MathInlineParser());
            }
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
        }
    }

    public sealed class DiagramExtension : IMarkdownExtension
    {
        private static readonly HashSet<string> DiagramLanguages = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "mermaid", "flow", "flowchart", "sequence", "mindmap"
        };

        public void Setup(MarkdownPipelineBuilder pipeline)
        {
        }

        public void Setup(MarkdownPipeline pipeline, IMarkdownRenderer renderer)
        {
        }

        internal static bool IsDiagramLanguage(string language)
        {
            return !string.IsNullOrEmpty(language) && DiagramLanguages.Contains(language);
        }
    }

    public static class MarkdownPipelineBuilderExtensions
    {
        public static MarkdownPipelineBuilder UseMathBlocks(this MarkdownPipelineBuilder builder)
        {
            if (!builder.Extensions.Contains<MathExtension>())
            {
                builder.Extensions.Add(new MathExtension());
            }

            return builder;
        }

        public static MarkdownPipelineBuilder UseDiagrams(this MarkdownPipelineBuilder builder)
        {
            if (!builder.Extensions.Contains<DiagramExtension>())
            {
                builder.Extensions.Add(new DiagramExtension());
            }

            return builder;
        }
    }
}
