namespace OneMarkDotNet
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Text;

    public sealed class FrontMatterParser
    {
        private const string Delimiter = "---";

        public Tuple<Dictionary<string, object>, string> Parse(string text)
        {
            var metadata = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            var content = text ?? string.Empty;

            if (string.IsNullOrEmpty(content) || !content.StartsWith(Delimiter, StringComparison.Ordinal))
            {
                return Tuple.Create(metadata, content);
            }

            var firstDelimiterEnd = content.IndexOf('\n');
            if (firstDelimiterEnd < 0)
            {
                return Tuple.Create(metadata, content);
            }

            var afterFirstDelimiter = firstDelimiterEnd + 1;
            if (afterFirstDelimiter >= content.Length)
            {
                return Tuple.Create(metadata, content);
            }

            var closingDelimiterIndex = content.IndexOf(
                Environment.NewLine + Delimiter + Environment.NewLine,
                afterFirstDelimiter,
                StringComparison.Ordinal);

            if (closingDelimiterIndex < 0)
            {
                closingDelimiterIndex = content.IndexOf(
                    "\n" + Delimiter + "\n",
                    afterFirstDelimiter,
                    StringComparison.Ordinal);
            }

            if (closingDelimiterIndex < 0)
            {
                closingDelimiterIndex = content.IndexOf(
                    "\r\n" + Delimiter + "\r\n",
                    afterFirstDelimiter,
                    StringComparison.Ordinal);
            }

            if (closingDelimiterIndex < 0)
            {
                return Tuple.Create(metadata, content);
            }

            var frontMatterText = content.Substring(afterFirstDelimiter, closingDelimiterIndex - afterFirstDelimiter);

            var lineEndLength = content.Contains("\r\n") ? 2 : 1;
            var afterClosingDelimiter = closingDelimiterIndex + lineEndLength + Delimiter.Length + lineEndLength;
            if (afterClosingDelimiter > content.Length)
            {
                afterClosingDelimiter = content.Length;
            }

            var remainingContent = content.Substring(afterClosingDelimiter);

            var lines = frontMatterText.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None);
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine))
                {
                    continue;
                }

                var colonIndex = trimmedLine.IndexOf(':');
                if (colonIndex < 0)
                {
                    continue;
                }

                var key = trimmedLine.Substring(0, colonIndex).Trim();
                var value = trimmedLine.Substring(colonIndex + 1).Trim();

                if (string.IsNullOrEmpty(key))
                {
                    continue;
                }

                metadata[key] = InferType(value);
            }

            return Tuple.Create(metadata, remainingContent);
        }

        public string Serialize(Dictionary<string, object> metadata)
        {
            if (metadata == null || metadata.Count == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            sb.AppendLine(Delimiter);

            foreach (var kvp in metadata)
            {
                sb.Append(kvp.Key);
                sb.Append(": ");

                if (kvp.Value == null)
                {
                    sb.AppendLine();
                }
                else if (kvp.Value is bool boolValue)
                {
                    sb.AppendLine(boolValue ? "true" : "false");
                }
                else if (kvp.Value is int || kvp.Value is double || kvp.Value is float || kvp.Value is long)
                {
                    sb.AppendLine(kvp.Value.ToString());
                }
                else
                {
                    var strValue = kvp.Value.ToString();
                    if (strValue.Contains(" ") || strValue.Contains(":") || strValue.Contains("#") ||
                        strValue.Contains("\"") || strValue.Contains("'"))
                    {
                        sb.Append('"');
                        sb.Append(strValue.Replace("\"", "\\\""));
                        sb.AppendLine("\"");
                    }
                    else
                    {
                        sb.AppendLine(strValue);
                    }
                }
            }

            sb.AppendLine(Delimiter);
            return sb.ToString();
        }

        private static object InferType(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            if (value.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (value.Equals("false", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if ((value.StartsWith("\"") && value.EndsWith("\"")) ||
                (value.StartsWith("'") && value.EndsWith("'")))
            {
                return value.Substring(1, value.Length - 2);
            }

            if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out int intValue))
            {
                return intValue;
            }

            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out double doubleValue))
            {
                return doubleValue;
            }

            return value;
        }
    }
}
