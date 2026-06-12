namespace NoteMark
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Windows.Forms;
    using System.Xml.Linq;

    public class ExportHandler
    {
        private readonly OneNoteApiWrapper apiWrapper;
        private readonly OneNoteXmlConverter converter;
        private readonly MarkdownParser parser;
        private readonly AppLogger logger;

        public ExportHandler(OneNoteApiWrapper apiWrapper)
        {
            this.apiWrapper = apiWrapper ?? throw new ArgumentNullException("apiWrapper");
            this.converter = new OneNoteXmlConverter();
            this.parser = new MarkdownParser();
            this.logger = AppLogger.Instance;
        }

        public void HandleExportToClipboard()
        {
            try
            {
                var pageContent = apiWrapper.GetCurrentPageContent();
                if (string.IsNullOrEmpty(pageContent))
                {
                    logger.Warning("ExportToClipboard: no page content available");
                    return;
                }

                var markdown = converter.ConvertToMarkdown(pageContent);
                if (string.IsNullOrEmpty(markdown))
                {
                    logger.Warning("ExportToClipboard: conversion produced empty markdown");
                    return;
                }

                ClipboardHelper.SetText(markdown);
                logger.Info("Exported page to clipboard");
            }
            catch (Exception ex)
            {
                logger.Error("HandleExportToClipboard failed", ex);
            }
        }

        public void HandleImportFromFile()
        {
            string filePath = null;

            // Show dialog on STA thread to avoid COM deadlock
            var thread = new Thread(() =>
            {
                using (var dialog = new OpenFileDialog())
                {
                    dialog.Title = "导入 Markdown 文件";
                    dialog.Filter = "Markdown Files (*.md)|*.md|All Files (*.*)|*.*";
                    dialog.DefaultExt = "md";
                    dialog.Multiselect = false;

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        filePath = dialog.FileName;
                    }
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
            {
                return;
            }

            try
            {
                var markdown = File.ReadAllText(filePath);
                if (string.IsNullOrWhiteSpace(markdown))
                {
                    return;
                }

                // Parse markdown
                var doc = parser.Parse(markdown);
                if (doc == null || doc.Blocks == null || doc.Blocks.Count == 0)
                {
                    return;
                }

                // Get current page XML
                var currentXml = apiWrapper.GetCurrentPageContent();
                if (string.IsNullOrEmpty(currentXml))
                {
                    logger.Warning("Import: no current page content");
                    return;
                }

                // Parse the current page
                XElement root;
                try
                {
                    root = XElement.Parse(currentXml);
                }
                catch (Exception ex)
                {
                    logger.Error("Import: failed to parse page XML", ex);
                    return;
                }

                var ns = root.GetNamespaceOfPrefix("one");
                if (ns == null || ns.NamespaceName.Length == 0)
                {
                    ns = root.GetDefaultNamespace();
                }
                if (ns == null || ns.NamespaceName.Length == 0)
                {
                    ns = OneNoteXmlBuilder.OneNoteNamespace;
                }

                // Render markdown to OneNote XML
                var theme = ThemeManager.Instance.GetCurrentTheme();
                var style = CreateRendererStyle(theme);
                var renderer = new OneNoteXmlRenderer();
                var renderedXml = renderer.Render(doc, style);

                if (string.IsNullOrEmpty(renderedXml))
                {
                    logger.Warning("Import: renderer produced empty content");
                    return;
                }

                // Parse rendered XML
                XElement renderedRoot;
                try
                {
                    renderedRoot = XElement.Parse(renderedXml);
                }
                catch (Exception ex)
                {
                    logger.Error("Import: failed to parse rendered XML", ex);
                    return;
                }

                var renderedNs = renderedRoot.GetNamespaceOfPrefix("one");
                if (renderedNs == null || renderedNs.NamespaceName.Length == 0)
                {
                    renderedNs = renderedRoot.GetDefaultNamespace();
                }
                if (renderedNs == null || renderedNs.NamespaceName.Length == 0)
                {
                    renderedNs = OneNoteXmlBuilder.OneNoteNamespace;
                }

                // Get rendered OEChildren content
                var renderedOutline = renderedRoot.Element(renderedNs + "Outline");
                if (renderedOutline == null)
                {
                    logger.Warning("Import: no Outline in rendered content");
                    return;
                }

                var renderedOeChildren = renderedOutline.Element(renderedNs + "OEChildren");
                if (renderedOeChildren == null)
                {
                    logger.Warning("Import: no OEChildren in rendered content");
                    return;
                }

                // Find the first Outline in the page and replace its OEChildren content
                var pageOutline = root.Element(ns + "Outline");
                if (pageOutline == null)
                {
                    logger.Warning("Import: no Outline in page");
                    return;
                }

                var pageOeChildren = pageOutline.Element(ns + "OEChildren");
                if (pageOeChildren == null)
                {
                    pageOeChildren = new XElement(ns + "OEChildren");
                    pageOutline.Add(pageOeChildren);
                }

                pageOeChildren.Elements(ns + "OE").Remove();

                foreach (var oe in renderedOeChildren.Elements(renderedNs + "OE"))
                {
                    pageOeChildren.Add(TransformNamespace(oe, renderedNs, ns));
                }

                var updatedXml = root.ToString(SaveOptions.DisableFormatting);
                apiWrapper.UpdatePageContent(updatedXml);
                logger.Info(string.Format("Imported markdown from: {0}", filePath));
            }
            catch (Exception ex)
            {
                logger.Error("HandleImportFromFile failed", ex);
            }
        }

        public void HandleExportToFile()
        {
            var pageContent = apiWrapper.GetCurrentPageContent();
            if (string.IsNullOrEmpty(pageContent))
            {
                logger.Warning("ExportToFile: no page content available");
                return;
            }

            string filePath = null;

            var thread = new Thread(() =>
            {
                using (var dialog = new SaveFileDialog())
                {
                    dialog.Title = "导出 Markdown 文件";
                    dialog.Filter = "Markdown Files (*.md)|*.md|All Files (*.*)|*.*";
                    dialog.DefaultExt = "md";

                    if (dialog.ShowDialog() == DialogResult.OK)
                    {
                        filePath = dialog.FileName;
                    }
                }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (string.IsNullOrEmpty(filePath))
            {
                return;
            }

            try
            {
                var markdown = converter.ConvertToMarkdown(pageContent);
                if (string.IsNullOrEmpty(markdown))
                {
                    logger.Warning("ExportToFile: conversion produced empty markdown");
                    return;
                }

                File.WriteAllText(filePath, markdown);
                logger.Info(string.Format("Exported page to: {0}", filePath));
            }
            catch (Exception ex)
            {
                logger.Error("HandleExportToFile failed", ex);
            }
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
    }
}
