using System.Text.Json;
using System.Text.Json.Serialization;

namespace OneMarkDotNet.AddIn;

public sealed class AddInSettings
{
    private static readonly Lazy<AddInSettings> _instance = new(() => new AddInSettings());
    public static AddInSettings Instance => _instance.Value;

    private readonly string _settingsDirectory;
    private readonly string _settingsFilePath;
    private readonly object _lock = new();

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public bool IsRealtimeRenderEnabled { get; set; } = true;
    public string CurrentThemeName { get; set; } = "default";
    public bool IsLineNumberEnabled { get; set; } = false;
    public bool IsLatexToImage { get; set; } = true;
    public bool IsSourceModeDefault { get; set; } = false;
    public HashSet<string> SourceModeBlocks { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    private AddInSettings()
    {
        _settingsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OneMarkDotNet");
        _settingsFilePath = Path.Combine(_settingsDirectory, "settings.json");
    }

    public void LoadSettings()
    {
        lock (_lock)
        {
            try
            {
                if (!File.Exists(_settingsFilePath))
                {
                    SaveSettings();
                    return;
                }

                var json = File.ReadAllText(_settingsFilePath);
                var loaded = JsonSerializer.Deserialize<SettingsData>(json, JsonOptions);
                if (loaded is null) return;

                IsRealtimeRenderEnabled = loaded.IsRealtimeRenderEnabled ?? true;
                CurrentThemeName = loaded.CurrentThemeName ?? "default";
                IsLineNumberEnabled = loaded.IsLineNumberEnabled ?? false;
                IsLatexToImage = loaded.IsLatexToImage ?? true;
                IsSourceModeDefault = loaded.IsSourceModeDefault ?? false;
                SourceModeBlocks = loaded.SourceModeBlocks is not null
                    ? new HashSet<string>(loaded.SourceModeBlocks, StringComparer.OrdinalIgnoreCase)
                    : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            }
            catch (Exception ex)
            {
                AppLogger.Instance.LogError("Failed to load settings", ex);
            }
        }
    }

    public void SaveSettings()
    {
        lock (_lock)
        {
            try
            {
                Directory.CreateDirectory(_settingsDirectory);

                var data = new SettingsData
                {
                    IsRealtimeRenderEnabled = IsRealtimeRenderEnabled,
                    CurrentThemeName = CurrentThemeName,
                    IsLineNumberEnabled = IsLineNumberEnabled,
                    IsLatexToImage = IsLatexToImage,
                    IsSourceModeDefault = IsSourceModeDefault,
                    SourceModeBlocks = SourceModeBlocks.ToList()
                };

                var json = JsonSerializer.Serialize(data, JsonOptions);
                File.WriteAllText(_settingsFilePath, json);
            }
            catch (Exception ex)
            {
                AppLogger.Instance.LogError("Failed to save settings", ex);
            }
        }
    }

    public bool IsSourceMode(string blockId)
    {
        lock (_lock)
        {
            return SourceModeBlocks.Contains(blockId);
        }
    }

    public void SetSourceMode(string blockId, bool isSourceMode)
    {
        lock (_lock)
        {
            if (isSourceMode)
                SourceModeBlocks.Add(blockId);
            else
                SourceModeBlocks.Remove(blockId);
        }
    }

    public string GetThemesDirectory()
    {
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OneMarkDotNet", "themes");
    }

    private sealed class SettingsData
    {
        public bool? IsRealtimeRenderEnabled { get; init; }
        public string? CurrentThemeName { get; init; }
        public bool? IsLineNumberEnabled { get; init; }
        public bool? IsLatexToImage { get; init; }
        public bool? IsSourceModeDefault { get; init; }
        public List<string>? SourceModeBlocks { get; init; }
    }
}
