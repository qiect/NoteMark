namespace OneMarkDotNet
{
    using System;
    using System.IO;
    using System.Windows.Forms;

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
            try
            {
                using (var dialog = new OpenFileDialog())
                {
                    dialog.Title = "Import Markdown File";
                    dialog.Filter = "Markdown Files (*.md)|*.md|All Files (*.*)|*.*";
                    dialog.DefaultExt = "md";
                    dialog.Multiselect = false;

                    var result = dialog.ShowDialog();
                    if (result != DialogResult.OK)
                    {
                        return;
                    }

                    var filePath = dialog.FileName;
                    if (!File.Exists(filePath))
                    {
                        logger.Warning(string.Format("Import: file not found: {0}", filePath));
                        return;
                    }

                    var markdown = File.ReadAllText(filePath);
                    if (string.IsNullOrWhiteSpace(markdown))
                    {
                        logger.Warning("Import: file is empty");
                        return;
                    }

                    var importer = new MarkdownImporter();
                    importer.ImportFromText(markdown);
                    logger.Info(string.Format("Imported markdown from: {0}", filePath));
                }
            }
            catch (Exception ex)
            {
                logger.Error("HandleImportFromFile failed", ex);
            }
        }

        public void HandleExportToFile()
        {
            try
            {
                var pageContent = apiWrapper.GetCurrentPageContent();
                if (string.IsNullOrEmpty(pageContent))
                {
                    logger.Warning("ExportToFile: no page content available");
                    return;
                }

                using (var dialog = new SaveFileDialog())
                {
                    dialog.Title = "Export Markdown File";
                    dialog.Filter = "Markdown Files (*.md)|*.md|All Files (*.*)|*.*";
                    dialog.DefaultExt = "md";

                    var result = dialog.ShowDialog();
                    if (result != DialogResult.OK)
                    {
                        return;
                    }

                    var filePath = dialog.FileName;
                    var markdown = converter.ConvertToMarkdown(pageContent);
                    if (string.IsNullOrEmpty(markdown))
                    {
                        logger.Warning("ExportToFile: conversion produced empty markdown");
                        return;
                    }

                    var exporter = new MarkdownExporter();
                    var doc = parser.Parse(markdown);
                    exporter.ExportToFileAsync(doc, filePath).GetAwaiter().GetResult();
                    logger.Info(string.Format("Exported page to: {0}", filePath));
                }
            }
            catch (Exception ex)
            {
                logger.Error("HandleExportToFile failed", ex);
            }
        }
    }
}
