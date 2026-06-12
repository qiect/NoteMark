namespace NoteMark
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;

    public class OneNoteToMarkdownConverter
    {
        public string Convert(string oneNoteXml)
        {
            if (string.IsNullOrWhiteSpace(oneNoteXml))
            {
                return string.Empty;
            }

            XDocument doc;
            try
            {
                doc = XDocument.Parse(oneNoteXml);
            }
            catch
            {
                return string.Empty;
            }

            var root = doc.Root;
            if (root == null)
            {
                return string.Empty;
            }

            // Dynamically resolve the namespace from the document
            XNamespace ns = root.GetDefaultNamespace();
            if (ns == null || ns.NamespaceName.Length == 0)
            {
                ns = root.GetNamespaceOfPrefix("one");
            }
            if (ns == null || ns.NamespaceName.Length == 0)
            {
                ns = OneNoteXmlBuilder.OneNoteNamespace;
            }

            var sb = new StringBuilder();

            var titleElement = root.Element(ns + "Title");
            if (titleElement != null)
            {
                var titleText = ExtractTextFromOE(titleElement.Element(ns + "OE"), ns);
                if (!string.IsNullOrEmpty(titleText))
                {
                    sb.AppendLine("# " + titleText);
                    sb.AppendLine();
                }
            }

            var outlines = root.Elements(ns + "Outline");
            foreach (var outline in outlines)
            {
                var oeChildren = outline.Element(ns + "OEChildren");
                if (oeChildren != null)
                {
                    ProcessOEChildren(oeChildren, sb, 0, ns);
                }
            }

            return sb.ToString().TrimEnd();
        }

        private void ProcessOEChildren(XElement oeChildren, StringBuilder sb, int indentLevel, XNamespace ns)
        {
            var oeElements = oeChildren.Elements(ns + "OE");
            foreach (var oe in oeElements)
            {
                ProcessOE(oe, sb, indentLevel, ns);
            }
        }

        private void ProcessOE(XElement oe, StringBuilder sb, int indentLevel, XNamespace ns)
        {
            var tableElement = oe.Element(ns + "Table");
            if (tableElement != null)
            {
                ProcessTable(tableElement, sb, ns);
                return;
            }

            var imageElement = oe.Element(ns + "Image");
            if (imageElement != null)
            {
                ProcessImage(imageElement, sb, ns);
                return;
            }

            var childOeChildren = oe.Element(ns + "OEChildren");
            var tElements = oe.Elements(ns + "T").ToList();

            if (tElements.Count > 0)
            {
                var text = ExtractStyledText(tElements);
                if (!string.IsNullOrEmpty(text))
                {
                    var headingLevel = DetectHeadingLevel(oe, ns);
                    var indent = new string(' ', indentLevel * 2);

                    if (headingLevel > 0)
                    {
                        var prefix = new string('#', headingLevel);
                        sb.AppendLine(indent + prefix + " " + text);
                    }
                    else
                    {
                        sb.AppendLine(indent + text);
                    }

                    sb.AppendLine();
                }
            }

            if (childOeChildren != null)
            {
                ProcessOEChildren(childOeChildren, sb, indentLevel + 1, ns);
            }
        }

        private void ProcessTable(XElement table, StringBuilder sb, XNamespace ns)
        {
            var rows = table.Elements(ns + "Row").ToList();
            if (rows.Count == 0)
            {
                return;
            }

            var tableData = new List<List<string>>();
            foreach (var row in rows)
            {
                var cells = row.Elements(ns + "Cell").ToList();
                var rowData = new List<string>();
                foreach (var cell in cells)
                {
                    var cellText = ExtractCellText(cell, ns);
                    rowData.Add(cellText.Trim());
                }
                tableData.Add(rowData);
            }

            if (tableData.Count == 0)
            {
                return;
            }

            var maxCols = tableData.Max(r => r.Count);

            for (int r = 0; r < tableData.Count; r++)
            {
                var row = tableData[r];
                while (row.Count < maxCols)
                {
                    row.Add(string.Empty);
                }

                sb.AppendLine("| " + string.Join(" | ", row) + " |");

                if (r == 0)
                {
                    var separator = new List<string>();
                    for (int c = 0; c < maxCols; c++)
                    {
                        separator.Add("---");
                    }
                    sb.AppendLine("| " + string.Join(" | ", separator) + " |");
                }
            }

            sb.AppendLine();
        }

        private void ProcessImage(XElement imageElement, StringBuilder sb, XNamespace ns)
        {
            var dataElement = imageElement.Element(ns + "Data");
            if (dataElement != null)
            {
                sb.AppendLine("![image](data:image/png;base64," + dataElement.Value + ")");
                sb.AppendLine();
            }
        }

        private string ExtractTextFromOE(XElement oe, XNamespace ns)
        {
            if (oe == null)
            {
                return string.Empty;
            }

            var tElements = oe.Elements(ns + "T");
            return ExtractStyledText(tElements.ToList());
        }

        private string ExtractStyledText(List<XElement> tElements)
        {
            if (tElements == null || tElements.Count == 0)
            {
                return string.Empty;
            }

            var sb = new StringBuilder();
            foreach (var t in tElements)
            {
                var cdata = t.Nodes().OfType<XCData>().FirstOrDefault();
                if (cdata != null)
                {
                    sb.Append(ParseCDataContent(cdata.Value));
                }
                else
                {
                    sb.Append(t.Value);
                }
            }

            return sb.ToString();
        }

        private string ParseCDataContent(string cdata)
        {
            if (string.IsNullOrEmpty(cdata))
            {
                return string.Empty;
            }

            var result = cdata;

            result = ProcessStyledSpans(result);

            result = ProcessLinks(result);

            result = UnescapeHtml(result);

            return result;
        }

        private string ProcessStyledSpans(string content)
        {
            var result = content;

            var boldItalicPattern = @"<span\s+style='[^']*font-weight:bold[^']*font-style:italic[^']*'>(.*?)</span>";
            result = Regex.Replace(result, boldItalicPattern, "***$1***", RegexOptions.Singleline);

            var italicBoldPattern = @"<span\s+style='[^']*font-style:italic[^']*font-weight:bold[^']*'>(.*?)</span>";
            result = Regex.Replace(result, italicBoldPattern, "***$1***", RegexOptions.Singleline);

            var boldPattern = @"<span\s+style='[^']*font-weight:bold[^']*'>(.*?)</span>";
            result = Regex.Replace(result, boldPattern, "**$1**", RegexOptions.Singleline);

            var italicPattern = @"<span\s+style='[^']*font-style:italic[^']*'>(.*?)</span>";
            result = Regex.Replace(result, italicPattern, "*$1*", RegexOptions.Singleline);

            var strikethroughPattern = @"<span\s+style='[^']*text-decoration:line-through[^']*'>(.*?)</span>";
            result = Regex.Replace(result, strikethroughPattern, "~~$1~~", RegexOptions.Singleline);

            var codePattern = @"<span\s+style='[^']*font-family:[^']*'>(.*?)</span>";
            result = Regex.Replace(result, codePattern, "`$1`", RegexOptions.Singleline);

            var genericSpanPattern = @"<span\s+style='[^']*'>(.*?)</span>";
            result = Regex.Replace(result, genericSpanPattern, "$1", RegexOptions.Singleline);

            return result;
        }

        private string ProcessLinks(string content)
        {
            var linkPattern = @"<a\s+href=""([^""]*)"">(.*?)</a>";
            return Regex.Replace(content, linkPattern, "[$2]($1)", RegexOptions.Singleline);
        }

        private string ExtractCellText(XElement cell, XNamespace ns)
        {
            var oeChildren = cell.Element(ns + "OEChildren");
            if (oeChildren == null)
            {
                return string.Empty;
            }

            var parts = new List<string>();
            foreach (var oe in oeChildren.Elements(ns + "OE"))
            {
                var tElements = oe.Elements(ns + "T").ToList();
                if (tElements.Count > 0)
                {
                    parts.Add(ExtractStyledText(tElements));
                }
            }

            return string.Join(" ", parts);
        }

        private int DetectHeadingLevel(XElement oe, XNamespace ns)
        {
            var quickStyleIndex = oe.Attribute("quickStyleIndex");
            if (quickStyleIndex != null)
            {
                if (int.TryParse(quickStyleIndex.Value, out int index))
                {
                    if (index >= 0 && index <= 5)
                    {
                        return index + 1;
                    }
                }
            }

            var tElements = oe.Elements(ns + "T").ToList();
            if (tElements.Count == 0)
            {
                return 0;
            }

            foreach (var t in tElements)
            {
                var cdata = t.Nodes().OfType<XCData>().FirstOrDefault();
                if (cdata != null)
                {
                    var fontSizeMatch = Regex.Match(cdata.Value, @"font-size:(\d+(?:\.\d+)?)pt");
                    if (fontSizeMatch.Success)
                    {
                        var fontSize = double.Parse(fontSizeMatch.Groups[1].Value, CultureInfo.InvariantCulture);
                        return FontSizeToHeadingLevel(fontSize);
                    }
                }
            }

            return 0;
        }

        private static int FontSizeToHeadingLevel(double fontSize)
        {
            if (fontSize >= 24.0) return 1;
            if (fontSize >= 20.0) return 2;
            if (fontSize >= 18.0) return 3;
            if (fontSize >= 16.0) return 4;
            if (fontSize >= 14.0) return 5;
            if (fontSize >= 12.0) return 6;
            return 0;
        }

        private static string UnescapeHtml(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            return text
                .Replace("&quot;", "\"")
                .Replace("&gt;", ">")
                .Replace("&lt;", "<")
                .Replace("&amp;", "&");
        }
    }
}
