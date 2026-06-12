namespace NoteMark
{
    using System;

    public class ConverterOneNoteStyle
    {
        public string FontFamily { get; set; }
        public double FontSize { get; set; }
        public bool IsBold { get; set; }
        public bool IsItalic { get; set; }
        public bool IsStrikethrough { get; set; }
        public string ForegroundColor { get; set; }
        public string HighlightColor { get; set; }
        public string BackgroundColor { get; set; }
        public double LineHeight { get; set; }

        public ConverterOneNoteStyle()
        {
            FontFamily = "Calibri";
            FontSize = 11.0;
            IsBold = false;
            IsItalic = false;
            IsStrikethrough = false;
            ForegroundColor = "#000000";
            HighlightColor = "transparent";
            BackgroundColor = "transparent";
            LineHeight = 1.2;
        }

        public ConverterOneNoteStyle Merge(ConverterOneNoteStyle other)
        {
            if (other == null)
            {
                return this;
            }

            var result = new ConverterOneNoteStyle
            {
                FontFamily = !string.IsNullOrEmpty(other.FontFamily) ? other.FontFamily : FontFamily,
                FontSize = other.FontSize > 0 ? other.FontSize : FontSize,
                IsBold = other.IsBold || IsBold,
                IsItalic = other.IsItalic || IsItalic,
                IsStrikethrough = other.IsStrikethrough || IsStrikethrough,
                ForegroundColor = !string.IsNullOrEmpty(other.ForegroundColor) && other.ForegroundColor != "#000000"
                    ? other.ForegroundColor
                    : ForegroundColor,
                HighlightColor = !string.IsNullOrEmpty(other.HighlightColor) && other.HighlightColor != "transparent"
                    ? other.HighlightColor
                    : HighlightColor,
                BackgroundColor = !string.IsNullOrEmpty(other.BackgroundColor) && other.BackgroundColor != "transparent"
                    ? other.BackgroundColor
                    : BackgroundColor,
                LineHeight = other.LineHeight > 0 ? other.LineHeight : LineHeight
            };

            return result;
        }
    }
}
