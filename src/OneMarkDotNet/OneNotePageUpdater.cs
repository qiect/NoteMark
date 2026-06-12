namespace OneMarkDotNet
{
    using System;
    using System.Runtime.InteropServices;

    public class OneNotePageUpdater
    {
        private readonly OneNoteApiWrapper apiWrapper;

        public OneNotePageUpdater(OneNoteApiWrapper apiWrapper)
        {
            this.apiWrapper = apiWrapper ?? throw new ArgumentNullException("apiWrapper");
        }

        public void ReplaceSelectedText(string pageId, string selectedText, string newXml)
        {
            try
            {
                if (string.IsNullOrEmpty(pageId) || string.IsNullOrEmpty(newXml))
                {
                    return;
                }

                var currentXml = apiWrapper.GetCurrentPageContent();
                if (string.IsNullOrEmpty(currentXml))
                {
                    return;
                }

                if (currentXml.Contains(selectedText))
                {
                    var updatedXml = currentXml.Replace(selectedText, newXml);
                    apiWrapper.UpdatePageContent(updatedXml);
                }
            }
            catch (COMException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void AppendContent(string pageId, string xmlContent)
        {
            try
            {
                if (string.IsNullOrEmpty(pageId) || string.IsNullOrEmpty(xmlContent))
                {
                    return;
                }

                var currentXml = apiWrapper.GetCurrentPageContent();
                if (string.IsNullOrEmpty(currentXml))
                {
                    return;
                }

                var insertPoint = currentXml.LastIndexOf("</Outline>", StringComparison.Ordinal);
                if (insertPoint < 0)
                {
                    insertPoint = currentXml.LastIndexOf("</Page>", StringComparison.Ordinal);
                }

                if (insertPoint >= 0)
                {
                    var updatedXml = currentXml.Insert(insertPoint, xmlContent);
                    apiWrapper.UpdatePageContent(updatedXml);
                }
            }
            catch (COMException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }
        }

        public void ReplacePage(string pageId, string xmlContent)
        {
            try
            {
                if (string.IsNullOrEmpty(pageId) || string.IsNullOrEmpty(xmlContent))
                {
                    return;
                }

                apiWrapper.UpdatePageContent(xmlContent);
            }
            catch (COMException)
            {
                throw;
            }
            catch (Exception)
            {
                throw;
            }
        }
    }
}
