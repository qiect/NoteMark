using System;

namespace OneMarkDotNet
{
    public class OneNoteStyle
    {
        public string FontFamily { get; set; }
        public string BackgroundColor { get; set; }
        public double LineHeight { get; set; }
        public double ParagraphMargin { get; set; }
        public string MonospaceFont { get; set; }
        public string CodeBackgroundColor { get; set; }
        public string TextColor { get; set; }
        public bool BlockquoteHeadingIcons { get; set; }
        public bool EnableHeadingInBlockquote { get; set; }
        public bool EnableCodeLineNumber { get; set; }
        public bool EnableLatexToImage { get; set; }
        public string BlockBorderColor { get; set; }

        public OneNoteStyle()
        {
            FontFamily = "Segoe UI";
            BackgroundColor = "#ffffff";
            LineHeight = 1.6;
            ParagraphMargin = 8.0;
            MonospaceFont = "Consolas";
            CodeBackgroundColor = "#f5f5f5";
            TextColor = "#333333";
            BlockquoteHeadingIcons = false;
            EnableHeadingInBlockquote = false;
            EnableCodeLineNumber = true;
            EnableLatexToImage = true;
            BlockBorderColor = "#cccccc";
        }

        public OneNoteStyle Merge(OneNoteStyle other)
        {
            if (other == null)
            {
                return this;
            }

            OneNoteStyle result = new OneNoteStyle
            {
                FontFamily = other.FontFamily ?? FontFamily,
                BackgroundColor = other.BackgroundColor ?? BackgroundColor,
                LineHeight = other.LineHeight != default(double) ? other.LineHeight : LineHeight,
                ParagraphMargin = other.ParagraphMargin != default(double) ? other.ParagraphMargin : ParagraphMargin,
                MonospaceFont = other.MonospaceFont ?? MonospaceFont,
                CodeBackgroundColor = other.CodeBackgroundColor ?? CodeBackgroundColor,
                TextColor = other.TextColor ?? TextColor,
                BlockquoteHeadingIcons = other.BlockquoteHeadingIcons,
                EnableHeadingInBlockquote = other.EnableHeadingInBlockquote,
                EnableCodeLineNumber = other.EnableCodeLineNumber,
                EnableLatexToImage = other.EnableLatexToImage,
                BlockBorderColor = other.BlockBorderColor ?? BlockBorderColor
            };

            return result;
        }
    }
}
