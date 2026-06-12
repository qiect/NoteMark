namespace OneMarkDotNet
{
    using System.Globalization;
    using System.Xml.Linq;

    public class OneNoteXmlBuilder
    {
        public const string OneNoteNamespace = "http://schemas.microsoft.com/office/onenote/2013/onenote";

        private static readonly XNamespace Ns = OneNoteNamespace;

        public XElement CreatePage()
        {
            var page = new XElement(Ns + "Page",
                new XAttribute("ID", string.Empty),
                new XAttribute(XNamespace.Xmlns + "one", OneNoteNamespace));
            return page;
        }

        public XElement CreatePage(string pageId)
        {
            var page = new XElement(Ns + "Page",
                new XAttribute("ID", pageId),
                new XAttribute(XNamespace.Xmlns + "one", OneNoteNamespace));
            return page;
        }

        public XElement CreateOutline()
        {
            return new XElement(Ns + "Outline");
        }

        public XElement CreateOutline(XElement oeChildren)
        {
            var outline = new XElement(Ns + "Outline");
            outline.Add(oeChildren);
            return outline;
        }

        public XElement CreateOE(string text, ConverterOneNoteStyle style)
        {
            var oe = new XElement(Ns + "OE");
            var t = CreateStyledT(text,
                style != null && style.IsBold,
                style != null && style.IsItalic,
                style != null && style.IsStrikethrough,
                style != null ? style.FontFamily : null,
                style != null ? style.FontSize : 0);
            oe.Add(t);
            return oe;
        }

        public XElement CreateOEChildren()
        {
            return new XElement(Ns + "OEChildren");
        }

        public XElement CreateOEChildren(XElement oe)
        {
            var children = new XElement(Ns + "OEChildren");
            children.Add(oe);
            return children;
        }

        public XElement CreateT(string text)
        {
            return new XElement(Ns + "T", new XCData(text));
        }

        public XElement CreateStyledT(string text, bool bold, bool italic, bool strikethrough,
            string fontFamily, double fontSize)
        {
            if (!bold && !italic && !strikethrough && string.IsNullOrEmpty(fontFamily) && fontSize <= 0)
            {
                return CreateT(text);
            }

            var styleParts = new System.Collections.Generic.List<string>();

            if (bold)
            {
                styleParts.Add("font-weight:bold");
            }

            if (italic)
            {
                styleParts.Add("font-style:italic");
            }

            if (strikethrough)
            {
                styleParts.Add("text-decoration:line-through");
            }

            if (!string.IsNullOrEmpty(fontFamily))
            {
                styleParts.Add(string.Format(CultureInfo.InvariantCulture,
                    "font-family:'{0}'", fontFamily));
            }

            if (fontSize > 0)
            {
                styleParts.Add(string.Format(CultureInfo.InvariantCulture,
                    "font-size:{0:0.##}pt", fontSize));
            }

            var styleAttr = string.Join(";", styleParts);
            var span = new XElement(Ns + "T",
                new XCData(string.Format(
                    CultureInfo.InvariantCulture,
                    "<span style='{0}'>{1}</span>",
                    styleAttr,
                    EscapeHtml(text))));

            return span;
        }

        public XElement CreateTable(int rows, int cols, string[][] data)
        {
            var table = new XElement(Ns + "Table",
                new XAttribute("bordersVisible", "true"));

            var columns = new XElement(Ns + "Columns");
            for (int c = 0; c < cols; c++)
            {
                columns.Add(new XElement(Ns + "Column",
                    new XAttribute("index", c.ToString(CultureInfo.InvariantCulture)),
                    new XAttribute("width", "100")));
            }

            table.Add(columns);

            for (int r = 0; r < rows; r++)
            {
                var row = new XElement(Ns + "Row");

                for (int c = 0; c < cols; c++)
                {
                    var cellText = (data != null && r < data.Length && c < data[r].Length)
                        ? data[r][c]
                        : string.Empty;

                    var cell = new XElement(Ns + "Cell",
                        new XElement(Ns + "OEChildren",
                            new XElement(Ns + "OE",
                                new XElement(Ns + "T", new XCData(cellText)))));
                    row.Add(cell);
                }

                table.Add(row);
            }

            return table;
        }

        public XElement CreateImage(string base64Data, int width, int height)
        {
            var image = new XElement(Ns + "Image",
                new XAttribute("format", "png"));

            if (width > 0 && height > 0)
            {
                image.Add(new XElement(Ns + "Size",
                    new XAttribute("width", width.ToString(CultureInfo.InvariantCulture)),
                    new XAttribute("height", height.ToString(CultureInfo.InvariantCulture)),
                    new XAttribute("isSetByUser", "true")));
            }

            image.Add(new XElement(Ns + "Data", base64Data));

            return image;
        }

        private static string EscapeHtml(string text)
        {
            if (string.IsNullOrEmpty(text))
            {
                return text;
            }

            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;");
        }
    }
}
