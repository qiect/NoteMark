namespace NoteMark
{
    using System;
    using System.IO;
    using Newtonsoft.Json;

    public sealed class AddInSettings
    {
        private static readonly object InstanceLock = new object();
        private static AddInSettings instance;

        private static readonly string SettingsDirectory = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "NoteMark");

        private static readonly string SettingsFilePath = Path.Combine(
            SettingsDirectory, "settings.json");

        public bool IsRealtimeRenderEnabled { get; set; }
        public bool IsLineNumberEnabled { get; set; }
        public bool IsLatexToImage { get; set; }
        public bool IsSourceModeDefault { get; set; }
        public string CurrentThemeName { get; set; }

        private AddInSettings()
        {
            IsRealtimeRenderEnabled = true;
            IsLineNumberEnabled = true;
            IsLatexToImage = true;
            IsSourceModeDefault = false;
            CurrentThemeName = "dark";
        }

        public static AddInSettings Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (InstanceLock)
                    {
                        if (instance == null)
                        {
                            instance = new AddInSettings();
                            instance.Load();
                        }
                    }
                }

                return instance;
            }
        }

        public void Load()
        {
            try
            {
                if (!File.Exists(SettingsFilePath))
                {
                    return;
                }

                var json = File.ReadAllText(SettingsFilePath);
                if (string.IsNullOrWhiteSpace(json))
                {
                    return;
                }

                var loaded = JsonConvert.DeserializeObject<AddInSettings>(json);
                if (loaded != null)
                {
                    IsRealtimeRenderEnabled = loaded.IsRealtimeRenderEnabled;
                    IsLineNumberEnabled = loaded.IsLineNumberEnabled;
                    IsLatexToImage = loaded.IsLatexToImage;
                    IsSourceModeDefault = loaded.IsSourceModeDefault;
                    CurrentThemeName = loaded.CurrentThemeName ?? "dark";
                }
            }
            catch (IOException)
            {
                // Use defaults if file cannot be read
            }
            catch (JsonException)
            {
                // Use defaults if JSON is invalid
            }
        }

        public void Save()
        {
            try
            {
                if (!Directory.Exists(SettingsDirectory))
                {
                    Directory.CreateDirectory(SettingsDirectory);
                }

                var json = JsonConvert.SerializeObject(this, Formatting.Indented);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch (IOException)
            {
                // Silently fail if settings cannot be saved
            }
            catch (JsonException)
            {
                // Silently fail if serialization fails
            }
        }
    }
}
