namespace OneMarkDotNet
{
    using System;
    using System.Text.RegularExpressions;

    public class MarkdownRenderHandler
    {
        private static readonly Regex HeadingPattern = new Regex(@"^#{1,6}\s", RegexOptions.Compiled);
        private static readonly Regex UnorderedListPattern = new Regex(@"^[-*+]\s", RegexOptions.Compiled);
        private static readonly Regex OrderedListPattern = new Regex(@"^\d+\.\s", RegexOptions.Compiled);
        private static readonly Regex BlockquotePattern = new Regex(@"^>\s", RegexOptions.Compiled);
        private static readonly Regex CodeBlockPattern = new Regex(@"^```", RegexOptions.Compiled);
        private static readonly Regex HorizontalRulePattern = new Regex(@"^(-{3,}|\*{3,}|_{3,})$", RegexOptions.Compiled);
        private static readonly Regex TablePattern = new Regex(@"\|.+\|", RegexOptions.Compiled);
        private static readonly Regex BoldPattern = new Regex(@"\*\*.+\*\*", RegexOptions.Compiled);
        private static readonly Regex MathPattern = new Regex(@"\$\$.*\$\$", RegexOptions.Compiled);
        private static readonly Regex TaskListPattern = new Regex(@"^-\s\[[ x]\]", RegexOptions.Compiled);
        private static readonly Regex LinkPattern = new Regex(@"\[.+\]\(.+\)", RegexOptions.Compiled);
        private static readonly Regex ImagePattern = new Regex(@"!\[.*\]\(.*\)", RegexOptions.Compiled);
        private static readonly Regex InlineCodePattern = new Regex(@"`.+`", RegexOptions.Compiled);

        private readonly OneNoteApiWrapper apiWrapper;
        private readonly ThemeManager themeManager;
        private readonly MarkdownParser parser;
        private readonly OneNoteXmlConverter converter;
        private readonly OneNotePageUpdater pageUpdater;
        private readonly AppLogger logger;

        private bool isInSourceMode;

        public MarkdownRenderHandler(OneNoteApiWrapper apiWrapper, ThemeManager themeManager)
        {
            this.apiWrapper = apiWrapper ?? throw new ArgumentNullException("apiWrapper");
            this.themeManager = themeManager ?? throw new ArgumentNullException("themeManager");
            this.parser = new MarkdownParser();
            this.converter = new OneNoteXmlConverter();
            this.pageUpdater = new OneNotePageUpdater(apiWrapper);
            this.logger = AppLogger.Instance;
            this.isInSourceMode = false;
        }

        public bool IsInSourceMode
        {
            get { return isInSourceMode; }
        }

        public void HandleRenderSelection()
        {
            try
            {
                var pageContent = apiWrapper.GetCurrentPageContent();
                if (string.IsNullOrEmpty(pageContent))
                {
                    logger.Warning("HandleRenderSelection: no page content available");
                    return;
                }

                var markdown = converter.ConvertToMarkdown(pageContent);
                if (string.IsNullOrEmpty(markdown))
                {
                    logger.Warning("HandleRenderSelection: conversion produced empty markdown");
                    return;
                }

                if (!ContainsMarkdownSyntax(markdown))
                {
                    logger.Info("HandleRenderSelection: no Markdown syntax detected, skipping");
                    return;
                }

                RenderMarkdownToPage(markdown);
            }
            catch (Exception ex)
            {
                logger.Error("HandleRenderSelection failed", ex);
            }
        }

        public void HandleSourceModeToggle()
        {
            try
            {
                isInSourceMode = !isInSourceMode;
                logger.Info(string.Format("Source mode toggled: {0}", isInSourceMode ? "ON" : "OFF"));

                if (!isInSourceMode)
                {
                    HandleRenderSelection();
                }
            }
            catch (Exception ex)
            {
                logger.Error("HandleSourceModeToggle failed", ex);
            }
        }

        private void RenderMarkdownToPage(string markdown)
        {
            var doc = parser.Parse(markdown);
            if (doc == null || doc.Blocks == null || doc.Blocks.Count == 0)
            {
                logger.Warning("RenderMarkdownToPage: parsed document is empty");
                return;
            }

            var theme = themeManager.GetCurrentTheme();
            var style = CreateStyleFromTheme(theme);

            var oneNoteXml = converter.ConvertToOneNoteXml(doc, style);
            if (string.IsNullOrEmpty(oneNoteXml))
            {
                logger.Warning("RenderMarkdownToPage: conversion produced empty XML");
                return;
            }

            var pageId = apiWrapper.GetCurrentPageId();
            if (!string.IsNullOrEmpty(pageId))
            {
                pageUpdater.ReplacePage(pageId, oneNoteXml);
                logger.Info("RenderMarkdownToPage: page updated successfully");
            }
            else
            {
                logger.Warning("RenderMarkdownToPage: no current page ID");
            }
        }

        private ConverterOneNoteStyle CreateStyleFromTheme(Theme theme)
        {
            var style = new ConverterOneNoteStyle();

            if (theme != null && theme.Style != null)
            {
                var themeStyle = theme.Style;
                style.FontFamily = themeStyle.FontFamily ?? style.FontFamily;
                style.ForegroundColor = themeStyle.TextColor ?? style.ForegroundColor;
                style.BackgroundColor = themeStyle.BackgroundColor ?? style.BackgroundColor;
                style.HighlightColor = themeStyle.CodeBackgroundColor ?? style.HighlightColor;
            }

            return style;
        }

        private bool ContainsMarkdownSyntax(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return false;
            }

            var lines = text.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);

            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();

                if (string.IsNullOrEmpty(trimmedLine))
                {
                    continue;
                }

                if (HeadingPattern.IsMatch(trimmedLine)) return true;
                if (UnorderedListPattern.IsMatch(trimmedLine)) return true;
                if (OrderedListPattern.IsMatch(trimmedLine)) return true;
                if (BlockquotePattern.IsMatch(trimmedLine)) return true;
                if (CodeBlockPattern.IsMatch(trimmedLine)) return true;
                if (HorizontalRulePattern.IsMatch(trimmedLine)) return true;
                if (TaskListPattern.IsMatch(trimmedLine)) return true;
                if (ImagePattern.IsMatch(trimmedLine)) return true;
                if (TablePattern.IsMatch(trimmedLine)) return true;
                if (BoldPattern.IsMatch(trimmedLine)) return true;
                if (MathPattern.IsMatch(trimmedLine)) return true;
                if (LinkPattern.IsMatch(trimmedLine)) return true;
                if (InlineCodePattern.IsMatch(trimmedLine)) return true;
            }

            return false;
        }
    }
}
