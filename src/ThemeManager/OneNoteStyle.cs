namespace OneMarkDotNet.ThemeManager;

public sealed class OneNoteStyle
{
    public string? FontFamily { get; init; }
    public double? FontSize { get; init; }
    public string? BackgroundColor { get; init; }
    public string? ForegroundColor { get; init; }
    public double? LineHeight { get; init; }
    public double? ParagraphMargin { get; init; }
    public bool? IsBold { get; init; }
    public bool? IsItalic { get; init; }
    public string? TextDecoration { get; init; }
    public BorderStyle? BorderStyle { get; init; }
    public string? MonospaceFont { get; init; }
}
