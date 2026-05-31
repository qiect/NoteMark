using System.Text;
using System.Text.RegularExpressions;

namespace OneMarkDotNet.ThemeManager;

public static partial class CssVariableParser
{
    private static readonly string[] SupportedVariables =
    [
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
    ];

    private static readonly HashSet<string> SupportedSet = [.. SupportedVariables];

    [GeneratedRegex(@":root\s*\{([^}]*)\}", RegexOptions.Singleline)]
    private static partial Regex RootBlockRegex();

    [GeneratedRegex(@"--([\w-]+)\s*:\s*([^;]+);?", RegexOptions.Singleline)]
    private static partial Regex VariableRegex();

    public static Dictionary<string, string> ParseVariables(string cssContent)
    {
        var variables = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match rootMatch in RootBlockRegex().Matches(cssContent))
        {
            var blockContent = rootMatch.Groups[1].Value;

            foreach (Match varMatch in VariableRegex().Matches(blockContent))
            {
                var name = string.Concat("--", varMatch.Groups[1].Value.Trim());
                var value = varMatch.Groups[2].Value.Trim();

                if (SupportedSet.Contains(name, StringComparer.OrdinalIgnoreCase))
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

        foreach (var (name, value) in variables)
        {
            sb.Append("  ").Append(name).Append(": ").Append(value).AppendLine(";");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }
}
