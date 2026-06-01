using System.Text;

namespace OneMarkDotNet.ImportExport;

public sealed class FrontMatterParser
{
    static readonly string[] Separator = { "---" };

    public (Dictionary<string, object> FrontMatter, string Body) Parse(string markdown)
    {
        var frontMatter = new Dictionary<string, object>();

        if (string.IsNullOrEmpty(markdown) || !markdown.StartsWith("---"))
            return (frontMatter, markdown);

        var firstLineEnd = markdown.IndexOf('\n');
        if (firstLineEnd < 0)
            return (frontMatter, markdown);

        var remaining = markdown.Substring(firstLineEnd + 1);
        var closingIndex = remaining.IndexOf("\n---\n", StringComparison.Ordinal);
        if (closingIndex < 0)
            closingIndex = remaining.IndexOf("\r\n---\r\n", StringComparison.Ordinal);
        if (closingIndex < 0)
            closingIndex = remaining.IndexOf("\n---", StringComparison.Ordinal);
        if (closingIndex < 0)
            return (frontMatter, markdown);

        var yamlBlock = remaining.Substring(0, closingIndex);
        var bodyStart = closingIndex + 3;
        var body = bodyStart < remaining.Length ? remaining.Substring(bodyStart) : string.Empty;
        body = body.TrimStart('\r', '\n');

        foreach (var line in yamlBlock.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed))
                continue;

            if (trimmed.StartsWith("-"))
            {
                continue;
            }

            var colonIndex = trimmed.IndexOf(':');
            if (colonIndex < 0)
                continue;

            var key = trimmed.Substring(0, colonIndex).Trim();
            var valueStr = trimmed.Substring(colonIndex + 1).Trim();

            if (key is "tags" or "categories")
            {
                var list = ParseList(valueStr, yamlBlock);
                frontMatter[key] = list;
            }
            else
            {
                frontMatter[key] = ParseScalar(valueStr);
            }
        }

        return (frontMatter, body);
    }

    List<string> ParseList(string inlineValue, string yamlBlock)
    {
        var list = new List<string>();

        if (!string.IsNullOrEmpty(inlineValue) && inlineValue != "[]" && inlineValue != "")
        {
            foreach (var item in inlineValue.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(x => x.Trim()))
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

        foreach (var line in lines)
        {
            var trimmed = line.TrimStart();
            if (trimmed.StartsWith("- "))
            {
                inList = true;
                var item = trimmed.Substring(2).Trim('"', '\'', ' ');
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

        if (value.StartsWith("\"") && value.EndsWith("\"") && value.Length >= 2)
            return value.Substring(1, value.Length - 2);

        if (value.StartsWith("'") && value.EndsWith("'") && value.Length >= 2)
            return value.Substring(1, value.Length - 2);

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

        foreach (var kv in frontMatter)
        {
            switch (kv.Value)
            {
                case IList<string> list:
                    sb.AppendLine($"{kv.Key}:");
                    foreach (var item in list)
                        sb.AppendLine($"  - {item}");
                    break;
                case IList<object> objList:
                    sb.AppendLine($"{kv.Key}:");
                    foreach (var item in objList)
                        sb.AppendLine($"  - {item}");
                    break;
                case DateTime dt:
                    sb.AppendLine($"{kv.Key}: {dt:yyyy-MM-dd}");
                    break;
                case bool b:
                    sb.AppendLine($"{kv.Key}: {(b ? "true" : "false")}");
                    break;
                case string s when s.Contains(':') || s.Contains('#') || s.StartsWith(" ") || s.StartsWith("\""):
                    sb.AppendLine($"{kv.Key}: \"{s}\"");
                    break;
                default:
                    sb.AppendLine($"{kv.Key}: {kv.Value}");
                    break;
            }
        }

        sb.AppendLine("---");
        return sb.ToString();
    }
}
