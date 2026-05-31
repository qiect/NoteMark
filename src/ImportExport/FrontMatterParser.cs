using System.Text;

namespace OneMarkDotNet.ImportExport;

public sealed class FrontMatterParser
{
    static readonly string[] Separator = ["---"];

    public (Dictionary<string, object> FrontMatter, string Body) Parse(string markdown)
    {
        var frontMatter = new Dictionary<string, object>();

        if (string.IsNullOrEmpty(markdown) || !markdown.StartsWith("---"))
            return (frontMatter, markdown);

        var span = markdown.AsSpan();
        var firstLineEnd = span.IndexOf('\n');
        if (firstLineEnd < 0)
            return (frontMatter, markdown);

        var remaining = span[(firstLineEnd + 1)..];
        var closingIndex = remaining.IndexOf("\n---\n");
        if (closingIndex < 0)
            closingIndex = remaining.IndexOf("\r\n---\r\n");
        if (closingIndex < 0)
            closingIndex = remaining.IndexOf("\n---");
        if (closingIndex < 0)
            return (frontMatter, markdown);

        var yamlBlock = remaining[..closingIndex].ToString();
        var bodyStart = closingIndex + 3;
        var body = bodyStart < remaining.Length ? remaining[bodyStart..].ToString() : string.Empty;
        body = body.TrimStart('\r', '\n');

        foreach (var line in yamlBlock.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed))
                continue;

            if (trimmed.StartsWith('-'))
            {
                continue;
            }

            var colonIndex = trimmed.IndexOf(':');
            if (colonIndex < 0)
                continue;

            var key = trimmed[..colonIndex].Trim();
            var valueSpan = trimmed[(colonIndex + 1)..].Trim();

            if (key is "tags" or "categories")
            {
                var list = ParseList(valueSpan.ToString(), yamlBlock);
                frontMatter[key] = list;
            }
            else
            {
                frontMatter[key] = ParseScalar(valueSpan.ToString());
            }
        }

        return (frontMatter, body);
    }

    List<string> ParseList(string inlineValue, string yamlBlock)
    {
        var list = new List<string>();

        if (!string.IsNullOrEmpty(inlineValue) && inlineValue != "[]" && inlineValue != "")
        {
            foreach (var item in inlineValue.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                var cleaned = item.Trim('[', ']', ' ', '"', '\'');
                if (!string.IsNullOrEmpty(cleaned))
                    list.Add(cleaned);
            }

            if (list.Count > 0)
                return list;
        }

        var lines = yamlBlock.Split('\n');
        var inList = false;
        var listKey = inlineValue.Length == 0 ? "tags" : "tags";

        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("- "))
            {
                inList = true;
                var item = trimmed[2..].Trim('"', '\'', ' ');
                if (!string.IsNullOrEmpty(item))
                    list.Add(item);
            }
            else if (inList && !string.IsNullOrEmpty(trimmed))
            {
                break;
            }
        }

        return list;
    }

    static object ParseScalar(string value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (value.StartsWith('"') && value.EndsWith('"'))
            return value[1..^1];

        if (value.StartsWith('\'') && value.EndsWith('\''))
            return value[1..^1];

        if (bool.TryParse(value, out var boolVal))
            return boolVal;

        if (int.TryParse(value, out var intVal))
            return intVal;

        if (double.TryParse(value, System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out var doubleVal))
            return doubleVal;

        if (DateTime.TryParse(value, out var dateVal))
            return dateVal;

        return value;
    }

    public string Serialize(Dictionary<string, object> frontMatter)
    {
        if (frontMatter.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        sb.AppendLine("---");

        foreach (var (key, value) in frontMatter)
        {
            switch (value)
            {
                case IList<string> list:
                    sb.AppendLine($"{key}:");
                    foreach (var item in list)
                        sb.AppendLine($"  - {item}");
                    break;
                case IList<object> objList:
                    sb.AppendLine($"{key}:");
                    foreach (var item in objList)
                        sb.AppendLine($"  - {item}");
                    break;
                case DateTime dt:
                    sb.AppendLine($"{key}: {dt:yyyy-MM-dd}");
                    break;
                case bool b:
                    sb.AppendLine($"{key}: {(b ? "true" : "false")}");
                    break;
                case string s when s.Contains(':') || s.Contains('#') || s.StartsWith(' ') || s.StartsWith('"'):
                    sb.AppendLine($"{key}: \"{s}\"");
                    break;
                default:
                    sb.AppendLine($"{key}: {value}");
                    break;
            }
        }

        sb.AppendLine("---");
        return sb.ToString();
    }
}
