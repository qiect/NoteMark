namespace OneMarkDotNet.ThemeManager;

public sealed class BorderStyle
{
    public bool HasBorder { get; init; }
    public string? Color { get; init; }

    public static BorderStyle None => new() { HasBorder = false };

    public static BorderStyle With(string color) => new() { HasBorder = true, Color = color };
}
