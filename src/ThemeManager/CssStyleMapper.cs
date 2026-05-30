using System.Globalization;

namespace OneMarkDotNet.ThemeManager;

public static class CssStyleMapper
{
    private static readonly HashSet<string> OneNoteSupportedProperties =
    [
        "color", "background", "background-color",
        "font-family", "font-size", "font-weight", "font-style",
        "text-align", "text-decoration", "line-height",
        "border", "border-color", "border-style", "border-width",
        "padding", "padding-left", "padding-right", "padding-top", "padding-bottom",
        "margin", "margin-left", "margin-right", "margin-top", "margin-bottom",
        "width"
    ];

    public static OneNoteStyle MapCssToOneNoteStyle(Theme theme)
    {
        var vars = theme.Variables;

        string? fontFamily = GetValue(vars, "--font-family");
        string? bgColor = GetValue(vars, "--bg-color");
        double? lineHeight = ParseDouble(GetValue(vars, "--line-height"));
        double? paragraphMargin = ParseDouble(GetValue(vars, "--paragraph-margin"));
        string? monospace = GetValue(vars, "--monospace");

        string? selectBgColor = GetValue(vars, "--select-text-bg-color");
        string? selectFontColor = GetValue(vars, "--select-text-font-color");

        return new OneNoteStyle
        {
            FontFamily = fontFamily,
            BackgroundColor = bgColor,
            ForegroundColor = selectFontColor,
            LineHeight = lineHeight,
            ParagraphMargin = paragraphMargin,
            MonospaceFont = monospace,
            BorderStyle = MapBorder(vars),
            TextDecoration = selectBgColor is not null ? $"background: {selectBgColor}" : null
        };
    }

    public static bool IsPropertySupported(string cssProperty)
    {
        return OneNoteSupportedProperties.Contains(cssProperty, StringComparer.OrdinalIgnoreCase);
    }

    private static BorderStyle? MapBorder(Dictionary<string, string> vars)
    {
        var blockWidthMargin = GetValue(vars, "--block-width-margin");
        if (blockWidthMargin is null)
            return null;

        return BorderStyle.With(blockWidthMargin);
    }

    private static string? GetValue(Dictionary<string, string> vars, string key)
    {
        return vars.TryGetValue(key, out var value) ? value : null;
    }

    private static double? ParseDouble(string? value)
    {
        if (value is null)
            return null;

        var normalized = value.Trim()
            .Replace("px", "", StringComparison.OrdinalIgnoreCase)
            .Replace("em", "", StringComparison.OrdinalIgnoreCase)
            .Replace("rem", "", StringComparison.OrdinalIgnoreCase)
            .Replace("%", "", StringComparison.OrdinalIgnoreCase)
            .Trim();

        return double.TryParse(normalized, NumberStyles.Float, CultureInfo.InvariantCulture, out var result)
            ? result
            : null;
    }
}
