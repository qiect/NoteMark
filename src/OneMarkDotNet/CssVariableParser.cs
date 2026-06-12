using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace OneMarkDotNet
{
    public static class CssVariableParser
    {
        private static readonly HashSet<string> SupportedVariables = new HashSet<string>
        {
            "--font-family",
            "--bg-color",
            "--line-height",
            "--paragraph-margin",
            "--monospace",
            "--select-text-bg-color",
            "--select-text-font-color",
            "--blockquote-heading-icons",
            "--enable-heading-in-blockquote",
            "--enable-code-line-number",
            "--enable-latex-to-image",
            "--block-width-margin"
        };

        public static Dictionary<string, string> Parse(string cssContent)
        {
            Dictionary<string, string> variables = new Dictionary<string, string>();

            if (string.IsNullOrWhiteSpace(cssContent))
            {
                return variables;
            }

            // Remove CSS comments
            string cleaned = Regex.Replace(cssContent, @"/\*.*?\*/", string.Empty, RegexOptions.Singleline);

            // Find :root { ... } blocks
            MatchCollection rootMatches = Regex.Matches(cleaned, @":root\s*\{([^}]*)\}", RegexOptions.Singleline);

            foreach (Match rootMatch in rootMatches)
            {
                if (!rootMatch.Success)
                {
                    continue;
                }

                string rootContent = rootMatch.Groups[1].Value;

                // Extract --variable: value pairs
                MatchCollection varMatches = Regex.Matches(rootContent, @"(--[\w-]+)\s*:\s*([^;]+);?");

                foreach (Match varMatch in varMatches)
                {
                    if (!varMatch.Success)
                    {
                        continue;
                    }

                    string varName = varMatch.Groups[1].Value.Trim();
                    string varValue = varMatch.Groups[2].Value.Trim();

                    if (!SupportedVariables.Contains(varName))
                    {
                        continue;
                    }

                    // Strip quotes from font family values
                    if (varName == "--font-family" || varName == "--monospace")
                    {
                        varValue = varValue.Trim('"', '\'');
                    }

                    // Strip "px" suffix from pixel values
                    if (varName == "--paragraph-margin" && varValue.EndsWith("px"))
                    {
                        varValue = varValue.Substring(0, varValue.Length - 2).Trim();
                    }

                    variables[varName] = varValue;
                }
            }

            return variables;
        }
    }
}
