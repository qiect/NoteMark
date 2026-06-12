namespace NoteMark
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml.Linq;

    public class MarkdownRenderHandler
    {
        private static readonly Regex HeadingPattern = new Regex(@"^#{1,6}\s", RegexOptions.Compiled);
        private static readonly Regex UnorderedListPattern = new Regex(@"^[-*+]\s", RegexOptions.Compiled);
        private static readonly Regex OrderedListPattern = new Regex(@"^\d+\.\s", RegexOptions.Compiled);
        private static readonly Regex BlockquotePattern = new Regex(@"^>\s", RegexOptions.Compiled);
        private static readonly Regex CodeBlockPattern = new Regex(@"^```", RegexOptions.Compiled);
        private static readonly Regex HorizontalRulePattern = new Regex(@"^(-{3,}|\*{3,}|_{3,})$", RegexOptions.Compiled);
        private static readonly Regex TablePattern = new Regex(@"\|.+\|", RegexOptions.Compiled);
        private static readonly Regex BoldPattern = new Regex(@"\*\*.+\*\*", RegexOptions.Compiled);
        private static readonly Regex MathPattern = new Regex(@"\$\$.*\$\$", RegexOptions.Compiled);
        private static readonly Regex TaskListPattern = new Regex(@"^-\s\[[ x]\]", RegexOptions.Compiled);
        private static readonly Regex LinkPattern = new Regex(@"\[.+\]\(.+\)", RegexOptions.Compiled);
        private static readonly Regex ImagePattern = new Regex(@"!\[.*\]\(.*\)", RegexOptions.Compiled);
        private static readonly Regex InlineCodePattern = new Regex(@"`.+`", RegexOptions.Compiled);

        // Marker to identify rendered OE elements
        private const string RenderedMarker = "data-nm-rendered=\"1\"";

        private readonly OneNoteApiWrapper apiWrapper;
        private readonly ThemeManager themeManager;
        private readonly MarkdownParser parser;
        private readonly AppLogger logger;

        private bool isInSourceMode;

        public MarkdownRenderHandler(OneNoteApiWrapper apiWrapper, ThemeManager themeManager)
        {
            this.apiWrapper = apiWrapper ?? throw new ArgumentNullException("apiWrapper");
            this.themeManager = themeManager ?? throw new ArgumentNullException("themeManager");
            this.parser = new MarkdownParser();
            this.logger = AppLogger.Instance;
            this.isInSourceMode = false;
        }

        public bool IsInSourceMode
        {
            get { return isInSourceMode; }
        }

        /// <summary>
        /// F5 full page render - renders all unrendered Markdown lines
        /// </summary>
        public void HandleRenderSelection()
        {
            try
            {
                logger.Info("HandleRenderSelection: starting");

                var pageContent = apiWrapper.GetCurrentPageContent();
                if (string.IsNullOrEmpty(pageContent))
                {
                    logger.Warning("HandleRenderSelection: no page content");
                    return;
                }

                XElement root;
                var ns = ParsePageXml(pageContent, out root);
                if (ns == null || root == null) return;

                // Find the first Outline and its OEChildren
                var pageOutline = root.Element(ns + "Outline");
                if (pageOutline == null)
                {
                    logger.Warning("HandleRenderSelection: no Outline in page");
                    return;
                }

                var pageOeChildren = pageOutline.Element(ns + "OEChildren");
                if (pageOeChildren == null) return;

                // Render all unrendered OE elements
                var changed = RenderOeElements(pageOeChildren, ns);

                if (changed)
                {
                    var updatedXml = root.ToString(SaveOptions.DisableFormatting);
                    apiWrapper.UpdatePageContent(updatedXml);
                    logger.Info("HandleRenderSelection: page updated");
                }
            }
            catch (Exception ex)
            {
                logger.Error("HandleRenderSelection failed", ex);
            }
        }

        /// <summary>
        /// Enter key - render only the last OE element (the line just typed)
        /// </summary>
        public void HandleRealtimeRender()
        {
            try
            {
                if (isInSourceMode) return;

                var settings = AddInSettings.Instance;
                if (!settings.IsRealtimeRenderEnabled) return;

                var pageContent = apiWrapper.GetCurrentPageContent();
                if (string.IsNullOrEmpty(pageContent)) return;

                XElement root;
                var ns = ParsePageXml(pageContent, out root);
                if (ns == null || root == null) return;

                var pageOutline = root.Element(ns + "Outline");
                if (pageOutline == null) return;

                var pageOeChildren = pageOutline.Element(ns + "OEChildren");
                if (pageOeChildren == null) return;

                // Get the OE elements - after Enter, the last OE is the new empty line,
                // the one before it is the line just typed
                var oeElements = pageOeChildren.Elements(ns + "OE").ToList();
                if (oeElements.Count == 0) return;

                // Find the last non-empty OE (the line just typed before Enter)
                XElement targetOe = null;
                for (int i = oeElements.Count - 1; i >= 0; i--)
                {
                    var text = ExtractPlainTextFromOe(oeElements[i], ns);
                    if (!string.IsNullOrWhiteSpace(text))
                    {
                        targetOe = oeElements[i];
                        break;
                    }
                }

                if (targetOe == null) return;

                // Check if it needs rendering
                if (!NeedsRendering(targetOe, ns)) return;

                // Render just this one OE
                var renderedOe = RenderSingleOe(targetOe, ns);
                if (renderedOe != null)
                {
                    targetOe.ReplaceWith(renderedOe);
                    var updatedXml = root.ToString(SaveOptions.DisableFormatting);
                    apiWrapper.UpdatePageContent(updatedXml);
                    logger.Info("HandleRealtimeRender: single line rendered");
                }
            }
            catch (Exception ex)
            {
                logger.Error("HandleRealtimeRender failed", ex);
            }
        }

        public void HandleSourceModeToggle()
        {
            try
            {
                isInSourceMode = !isInSourceMode;
                logger.Info(string.Format("Source mode: {0}", isInSourceMode ? "ON" : "OFF"));
                if (!isInSourceMode) HandleRenderSelection();
            }
            catch (Exception ex)
            {
                logger.Error("HandleSourceModeToggle failed", ex);
            }
        }

        public void HandleExitSourceMode()
        {
            try
            {
                if (isInSourceMode)
                {
                    isInSourceMode = false;
                    HandleRenderSelection();
                }
            }
            catch (Exception ex)
            {
                logger.Error("HandleExitSourceMode failed", ex);
            }
        }

        /// <summary>
        /// Render all unrendered OE elements in an OEChildren container
        /// </summary>
        private bool RenderOeElements(XElement oeChildren, XNamespace ns)
        {
            var changed = false;
            var oeList = oeChildren.Elements(ns + "OE").ToList();

            foreach (var oe in oeList)
            {
                // Skip already rendered elements
                if (!NeedsRendering(oe, ns)) continue;

                var renderedOe = RenderSingleOe(oe, ns);
                if (renderedOe != null)
                {
                    oe.ReplaceWith(renderedOe);
                    changed = true;
                }
            }

            return changed;
        }

        /// <summary>
        /// Check if an OE element contains unrendered Markdown text
        /// </summary>
        private bool NeedsRendering(XElement oe, XNamespace ns)
        {
            // Check if already rendered (has our marker)
            foreach (var t in oe.Elements(ns + "T"))
            {
                var cdata = t.Nodes().OfType<XCData>().FirstOrDefault();
                if (cdata != null && cdata.Value.Contains(RenderedMarker))
                {
                    return false;
                }
            }

            // Extract plain text and check for Markdown syntax
            var text = ExtractPlainTextFromOe(oe, ns);
            if (string.IsNullOrEmpty(text)) return false;

            return IsMarkdownLine(text);
        }

        /// <summary>
        /// Render a single OE element from Markdown to styled OneNote XML
        /// </summary>
        private XElement RenderSingleOe(XElement originalOe, XNamespace ns)
        {
            var text = ExtractPlainTextFromOe(originalOe, ns);
            if (string.IsNullOrEmpty(text)) return null;

            if (!IsMarkdownLine(text)) return null;

            var theme = themeManager.GetCurrentTheme();
            var style = CreateRendererStyle(theme);

            // Parse as Markdown
            var doc = parser.Parse(text);
            if (doc == null || doc.Blocks == null || doc.Blocks.Count == 0) return null;

            // Render using OneNoteXmlRenderer
            var renderer = new OneNoteXmlRenderer();
            var renderedXml = renderer.Render(doc, style);
            if (string.IsNullOrEmpty(renderedXml)) return null;

            // Parse the rendered XML
            XElement renderedRoot;
            try
            {
                renderedRoot = XElement.Parse(renderedXml);
            }
            catch
            {
                return null;
            }

            var renderedNs = GetNamespace(renderedRoot);
            var renderedOutline = renderedRoot.Element(renderedNs + "Outline");
            if (renderedOutline == null) return null;

            var renderedOeChildren = renderedOutline.Element(renderedNs + "OEChildren");
            if (renderedOeChildren == null) return null;

            // Get the first rendered OE (single line = single OE)
            var renderedOe = renderedOeChildren.Elements(renderedNs + "OE").FirstOrDefault();
            if (renderedOe == null) return null;

            // Transform namespace and add rendered marker
            var newOe = TransformNamespace(renderedOe, renderedNs, ns);

            // Add rendered marker to the T element's CDATA
            var tElement = newOe.Element(ns + "T");
            if (tElement != null)
            {
                var cdata = tElement.Nodes().OfType<XCData>().FirstOrDefault();
                if (cdata != null)
                {
                    // Insert marker right after the opening span
                    var markerSpan = "<span " + RenderedMarker + " style=\"display:none\"></span>";
                    cdata.Value = markerSpan + cdata.Value;
                }
            }

            // Copy attributes from original OE (like quickStyleIndex)
            foreach (var attr in originalOe.Attributes())
            {
                if (newOe.Attribute(attr.Name) == null)
                {
                    newOe.Add(attr);
                }
            }

            return newOe;
        }

        private XNamespace ParsePageXml(string pageXml, out XElement root)
        {
            root = null;
            try
            {
                root = XElement.Parse(pageXml);
            }
            catch (Exception ex)
            {
                logger.Error("Failed to parse page XML", ex);
                return null;
            }

            return GetNamespace(root);
        }

        private XNamespace GetNamespace(XElement root)
        {
            var ns = root.GetNamespaceOfPrefix("one");
            if (ns == null || ns.NamespaceName.Length == 0)
            {
                ns = root.GetDefaultNamespace();
            }
            if (ns == null || ns.NamespaceName.Length == 0)
            {
                ns = OneNoteXmlBuilder.OneNoteNamespace;
            }
            return ns;
        }

        private string ExtractPlainTextFromOe(XElement oe, XNamespace ns)
        {
            var sb = new StringBuilder();

            // Get text from T elements
            foreach (var t in oe.Elements(ns + "T"))
            {
                var text = ExtractPlainTextFromCData(t);
                if (!string.IsNullOrEmpty(text))
                {
                    sb.Append(text);
                }
            }

            return sb.ToString().Trim();
        }

        private static string ExtractPlainTextFromCData(XElement tElement)
        {
            var cdata = tElement.Nodes().OfType<XCData>().FirstOrDefault();
            if (cdata != null)
            {
                return StripHtmlTags(cdata.Value);
            }
            return StripHtmlTags(tElement.Value);
        }

        private static string StripHtmlTags(string html)
        {
            if (string.IsNullOrEmpty(html)) return string.Empty;

            // Remove our rendered marker spans
            var result = Regex.Replace(html, @"<span\s+data-nm-rendered=""1""[^>]*></span>", string.Empty);

            // Remove all HTML tags
            result = Regex.Replace(result, @"<[^>]+>", string.Empty);

            // Decode HTML entities
            result = result.Replace("&amp;", "&")
                           .Replace("&lt;", "<")
                           .Replace("&gt;", ">")
                           .Replace("&quot;", "\"")
                           .Replace("&apos;", "'")
                           .Replace("&#39;", "'");

            return result.Trim();
        }

        private XElement TransformNamespace(XElement element, XNamespace fromNs, XNamespace toNs)
        {
            var newName = element.Name.Namespace == fromNs
                ? toNs + element.Name.LocalName
                : element.Name;

            var newElement = new XElement(newName);

            foreach (var attr in element.Attributes())
            {
                if (attr.IsNamespaceDeclaration && attr.Value == fromNs.NamespaceName)
                {
                    newElement.Add(new XAttribute(attr.Name, toNs.NamespaceName));
                }
                else
                {
                    newElement.Add(attr);
                }
            }

            foreach (var node in element.Nodes())
            {
                if (node is XElement childElement)
                {
                    newElement.Add(TransformNamespace(childElement, fromNs, toNs));
                }
                else
                {
                    newElement.Add(node);
                }
            }

            return newElement;
        }

        private RendererStyle CreateRendererStyle(Theme theme)
        {
            var style = new RendererStyle();
            if (theme != null && theme.Style != null)
            {
                var themeStyle = theme.Style;
                style.FontFamily = themeStyle.FontFamily ?? style.FontFamily;
                style.TextColor = themeStyle.TextColor ?? style.TextColor;
                style.BackgroundColor = themeStyle.BackgroundColor ?? style.BackgroundColor;
                style.CodeBackgroundColor = themeStyle.CodeBackgroundColor ?? style.CodeBackgroundColor;
            }
            return style;
        }

        private bool IsMarkdownLine(string text)
        {
            if (string.IsNullOrEmpty(text)) return false;

            var trimmed = text.Trim();
            if (string.IsNullOrEmpty(trimmed)) return false;

            if (HeadingPattern.IsMatch(trimmed)) return true;
            if (UnorderedListPattern.IsMatch(trimmed)) return true;
            if (OrderedListPattern.IsMatch(trimmed)) return true;
            if (BlockquotePattern.IsMatch(trimmed)) return true;
            if (CodeBlockPattern.IsMatch(trimmed)) return true;
            if (HorizontalRulePattern.IsMatch(trimmed)) return true;
            if (TaskListPattern.IsMatch(trimmed)) return true;
            if (ImagePattern.IsMatch(trimmed)) return true;
            if (TablePattern.IsMatch(trimmed)) return true;
            if (BoldPattern.IsMatch(trimmed)) return true;
            if (MathPattern.IsMatch(trimmed)) return true;
            if (LinkPattern.IsMatch(trimmed)) return true;
            if (InlineCodePattern.IsMatch(trimmed)) return true;

            return false;
        }
    }
}
