using System.Collections.Concurrent;

namespace NoteMark.ThemeManager;

public sealed class ThemeManager
{
    private const string GlobalCssFileName = "__global.css";
    private const int MaxThemeCount = 10;

    private readonly ConcurrentDictionary<string, Theme> _themes = new(StringComparer.OrdinalIgnoreCase);
    private string _themesDirectory = string.Empty;

    public void LoadThemes(string directory)
    {
        _themesDirectory = directory;

        if (!Directory.Exists(directory))
            return;

        foreach (var filePath in Directory.EnumerateFiles(directory, "*.css"))
        {
            var fileName = Path.GetFileName(filePath);

            if (string.Equals(fileName, GlobalCssFileName, StringComparison.OrdinalIgnoreCase))
                continue;

            var theme = CreateTheme(filePath, fileName);
            _themes[theme.Name] = theme;
        }
    }

    public IReadOnlyList<Theme> GetThemeList()
    {
        return _themes.Values
            .OrderByDescending(t => t.LastModified)
            .Take(MaxThemeCount)
            .ToList();
    }

    public OneNoteStyle ApplyTheme(string themeName, string noteId)
    {
        if (!_themes.TryGetValue(themeName, out var theme))
            throw new KeyNotFoundException($"Theme '{themeName}' not found.");

        return CssStyleMapper.MapCssToOneNoteStyle(theme);
    }

    public void ReloadThemes()
    {
        _themes.Clear();

        if (!string.IsNullOrEmpty(_themesDirectory))
            LoadThemes(_themesDirectory);
    }

    public Dictionary<string, string> GetGlobalCssVariables()
    {
        if (string.IsNullOrEmpty(_themesDirectory))
            return [];

        var globalPath = Path.Combine(_themesDirectory, GlobalCssFileName);

        if (!File.Exists(globalPath))
            return [];

        var content = File.ReadAllText(globalPath);
        return CssVariableParser.ParseVariables(content);
    }

    public Dictionary<string, string> GetThemeCssVariables(string themeName)
    {
        if (!_themes.TryGetValue(themeName, out var theme))
            throw new KeyNotFoundException($"Theme '{themeName}' not found.");

        return new Dictionary<string, string>(theme.Variables);
    }

    private static Theme CreateTheme(string filePath, string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        var content = File.ReadAllText(filePath);
        var lastModified = File.GetLastWriteTimeUtc(filePath);
        var variables = CssVariableParser.ParseVariables(content);

        return new Theme
        {
            Name = name,
            FileName = fileName,
            FilePath = filePath,
            LastModified = lastModified,
            CssContent = content,
            Variables = variables
        };
    }
}
