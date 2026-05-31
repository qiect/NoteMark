namespace OneMarkDotNet.ThemeManager;

public sealed class Theme
{
    private const string KeyFontFamily = "--font-family";
    private const string KeyBgColor = "--bg-color";
    private const string KeyMonospace = "--monospace";
    private const string KeyCodeFontFamily = "--code-font-family";
    private const string KeyCodeFontColor = "--code-font-color";
    private const string KeyCodeBgColor = "--code-bg-color";
    private const string KeyHeadingFontFamily = "--heading-font-family";
    private const string KeyHeadingFontColor = "--heading-font-color";
    private const string KeyQuoteFontColor = "--quote-font-color";
    private const string KeyFontColor = "--font-color";

    public required string Name { get; init; }
    public required string FileName { get; init; }
    public required string FilePath { get; init; }
    public DateTime LastModified { get; init; }
    public required string CssContent { get; init; }
    public Dictionary<string, string> Variables { get; init; } = [];

    public string? FontFamily => GetValue(KeyFontFamily);
    public string? FontColor => GetValue(KeyFontColor);
    public string? CodeFontFamily => GetValue(KeyCodeFontFamily) ?? GetValue(KeyMonospace);
    public string? CodeFontColor => GetValue(KeyCodeFontColor);
    public string? CodeBackgroundColor => GetValue(KeyCodeBgColor);
    public string? HeadingFontFamily => GetValue(KeyHeadingFontFamily) ?? GetValue(KeyFontFamily);
    public string? HeadingFontColor => GetValue(KeyHeadingFontColor);
    public string? QuoteFontColor => GetValue(KeyQuoteFontColor);

    private string? GetValue(string key)
    {
        return Variables.TryGetValue(key, out var value) ? value : null;
    }
}
