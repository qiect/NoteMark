namespace NoteMark
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Reflection;
    using System.Text;

    public static class OneMarkRibbon
    {
        private const string RibbonXmlResourceName = "NoteMark.Ribbon.xml";

        public static string GetThemeMenuXml(List<Theme> themes)
        {
            if (themes == null || themes.Count == 0)
            {
                return "<menu xmlns=\"http://schemas.microsoft.com/office/2009/07/customui\">" +
                       "<button id=\"NoThemesLabel\" label=\"(No themes available)\" enabled=\"false\" />" +
                       "</menu>";
            }

            var sb = new StringBuilder();
            sb.Append("<menu xmlns=\"http://schemas.microsoft.com/office/2009/07/customui\">");

            foreach (var theme in themes)
            {
                var themeName = EscapeXmlAttribute(theme.Name ?? "unknown");
                var buttonId = EscapeXmlAttribute("Theme_" + (theme.Name ?? "unknown"));
                sb.AppendFormat(
                    "<button id=\"{0}\" label=\"{1}\" onAction=\"OnThemeMenuClick\" tag=\"{2}\" />",
                    buttonId,
                    themeName,
                    themeName);
            }

            sb.Append("<menuSeparator id=\"ThemeSeparator\" />");
            sb.AppendFormat(
                "<button id=\"OpenThemeDirButton\" label=\"打开主题目录\" onAction=\"OnOpenThemeDirClick\" />");
            sb.AppendFormat(
                "<button id=\"ReloadThemesButton\" label=\"刷新主题\" onAction=\"OnReloadThemesClick\" />");
            sb.Append("</menu>");

            return sb.ToString();
        }

        public static string LoadRibbonXml()
        {
            var assembly = Assembly.GetExecutingAssembly();

            using (var stream = assembly.GetManifestResourceStream(RibbonXmlResourceName))
            {
                if (stream == null)
                {
                    var resourceNames = assembly.GetManifestResourceNames();
                    foreach (var name in resourceNames)
                    {
                        if (name.EndsWith("Ribbon.xml", StringComparison.OrdinalIgnoreCase))
                        {
                            using (var altStream = assembly.GetManifestResourceStream(name))
                            {
                                if (altStream != null)
                                {
                                    return ReadStream(altStream);
                                }
                            }
                        }
                    }

                    return string.Empty;
                }

                return ReadStream(stream);
            }
        }

        private static string ReadStream(Stream stream)
        {
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }

        private static string EscapeXmlAttribute(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return value
                .Replace("&", "&amp;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace(" ", "_");
        }
    }
}
