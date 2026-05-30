namespace OneMarkDotNet.ThemeManager;

public sealed class Theme
{
    public required string Name { get; init; }
    public required string FileName { get; init; }
    public required string FilePath { get; init; }
    public DateTime LastModified { get; init; }
    public required string CssContent { get; init; }
    public Dictionary<string, string> Variables { get; init; } = [];
}
