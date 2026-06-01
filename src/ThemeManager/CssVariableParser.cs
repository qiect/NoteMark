using System.Text;
using System.Text.RegularExpressions;

namespace OneMarkDotNet.ThemeManager;

public static class CssVariableParser
{
    private static readonly string[] SupportedVariables =
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

    private static readonly HashSet<string> SupportedSet = new(SupportedVariables, StringComparer.OrdinalIgnoreCase);

    private static readonly Regex RootBlockRegex =
        new Regex(@":root\s*\{([^}]*)\}", RegexOptions.Singleline | RegexOptions.Compiled);

    private static readonly Regex VariableRegex =
        new Regex(@"--([\w-]+)\s*:\s*([^;]+);?", RegexOptions.Singleline | RegexOptions.Compiled);

    public static Dictionary<string, string> ParseVariables(string cssContent)
    {
        var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match rootMatch in RootBlockRegex.Matches(cssContent))
        {
            var blockContent = rootMatch.Groups[1].Value;

            foreach (Match varMatch in VariableRegex.Matches(blockContent))
            {
                var name = string.Concat("--", varMatch.Groups[1].Value.Trim());
                var value = varMatch.Groups[2].Value.Trim();

                if (SupportedSet.Contains(name))
                {
                    variables[name] = value;
                }
            }
        }

        return variables;
    }

    public static string SerializeVariables(Dictionary<string, string> variables)
    {
        if (variables.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine(":root {");

        foreach (var kv in variables)
        {
            sb.Append("  ").Append(kv.Key).Append(": ").Append(kv.Value).AppendLine(";");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }
}
