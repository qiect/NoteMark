namespace OneMarkDotNet
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    public class ThemeManager
    {
        private const int MaxThemes = 10;
        private const string ThemesDirectoryName = "themes";
        private const string GlobalCssFileName = "__global.css";
        private const string AppThemesDirectoryName = "Themes";

        private static readonly Lazy<ThemeManager> _instance = new Lazy<ThemeManager>(() => new ThemeManager(), true);

        public static ThemeManager Instance
        {
            get { return _instance.Value; }
        }

        private List<Theme> _themes;
        private Dictionary<string, string> _globalVariables;
        private string _currentThemeName;
        private bool _loaded;

        private ThemeManager()
        {
            _themes = new List<Theme>();
            _globalVariables = new Dictionary<string, string>();
            _currentThemeName = string.Empty;
            _loaded = false;
        }

        public List<Theme> GetThemes()
        {
            EnsureLoaded();
            return _themes.ToList();
        }

        public Theme GetTheme(string name)
        {
            EnsureLoaded();
            if (string.IsNullOrWhiteSpace(name))
            {
                return null;
            }

            return _themes.FirstOrDefault(t => string.Equals(t.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        public void ReloadThemes()
        {
            _loaded = false;
            EnsureLoaded();
        }

        public string CurrentThemeName
        {
            get { return _currentThemeName; }
            set { _currentThemeName = value; }
        }

        public Theme GetCurrentTheme()
        {
            EnsureLoaded();
            if (!string.IsNullOrWhiteSpace(_currentThemeName))
            {
                Theme currentTheme = GetTheme(_currentThemeName);
                if (currentTheme != null)
                {
                    return currentTheme;
                }
            }

            return _themes.FirstOrDefault();
        }

        private void EnsureLoaded()
        {
            if (_loaded)
            {
                return;
            }

            _loaded = true;
            LoadThemes();
        }

        private static string GetDllDirectory()
        {
            try
            {
                var codeBase = typeof(ThemeManager).Assembly.CodeBase;
                var uri = new Uri(codeBase);
                return Path.GetDirectoryName(uri.LocalPath) ?? string.Empty;
            }
            catch
            {
                return AppDomain.CurrentDomain.BaseDirectory;
            }
        }

        private void LoadThemes()
        {
            _themes.Clear();
            _globalVariables.Clear();

            // Load global variables first
            LoadGlobalVariables();

            // Load default themes from DLL directory (not OneNote's BaseDirectory)
            string dllDir = GetDllDirectory();
            string appThemesDir = Path.Combine(dllDir, AppThemesDirectoryName);
            LoadThemesFromDirectory(appThemesDir, true);

            // Load user themes from %APPDATA%
            string appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OneMarkDotNet", ThemesDirectoryName);
            LoadThemesFromDirectory(appDataDir, false);

            // Sort by name for consistent ordering, take max
            _themes = _themes
                .OrderBy(t => t.Name)
                .Take(MaxThemes)
                .ToList();
        }

        private void LoadGlobalVariables()
        {
            string dllDir = GetDllDirectory();
            string appGlobalCss = Path.Combine(dllDir, AppThemesDirectoryName, GlobalCssFileName);
            if (File.Exists(appGlobalCss))
            {
                try
                {
                    string content = File.ReadAllText(appGlobalCss);
                    _globalVariables = CssVariableParser.Parse(content);
                }
                catch
                {
                    // Skip unreadable files
                }
            }

            string appDataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "OneMarkDotNet", ThemesDirectoryName);
            string appDataGlobalCss = Path.Combine(appDataDir, GlobalCssFileName);
            if (File.Exists(appDataGlobalCss))
            {
                try
                {
                    string content = File.ReadAllText(appDataGlobalCss);
                    Dictionary<string, string> appDataGlobals = CssVariableParser.Parse(content);
                    foreach (KeyValuePair<string, string> kvp in appDataGlobals)
                    {
                        _globalVariables[kvp.Key] = kvp.Value;
                    }
                }
                catch
                {
                    // Skip unreadable files
                }
            }
        }

        private void LoadThemesFromDirectory(string directory, bool isDefault)
        {
            if (!Directory.Exists(directory))
            {
                return;
            }

            string[] cssFiles;
            try
            {
                cssFiles = Directory.GetFiles(directory, "*.css");
            }
            catch
            {
                return;
            }

            foreach (string cssFile in cssFiles)
            {
                string fileName = Path.GetFileNameWithoutExtension(cssFile);

                if (string.Equals(fileName, "__global", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!isDefault && _themes.Any(t => string.Equals(t.Name, fileName, StringComparison.OrdinalIgnoreCase)))
                {
                    _themes.RemoveAll(t => string.Equals(t.Name, fileName, StringComparison.OrdinalIgnoreCase));
                }
                else if (isDefault && _themes.Any(t => string.Equals(t.Name, fileName, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                try
                {
                    string content = File.ReadAllText(cssFile);
                    Dictionary<string, string> themeVariables = CssVariableParser.Parse(content);

                    Dictionary<string, string> mergedVariables = new Dictionary<string, string>(_globalVariables);
                    foreach (KeyValuePair<string, string> kvp in themeVariables)
                    {
                        mergedVariables[kvp.Key] = kvp.Value;
                    }

                    Theme theme = new Theme(fileName, mergedVariables);
                    _themes.Add(theme);
                }
                catch
                {
                    // Skip files that cannot be read or parsed
                }
            }
        }
    }
}
