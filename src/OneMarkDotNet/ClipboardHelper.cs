namespace OneMarkDotNet
{
    using System;
    using System.Text;
    using System.Threading;
    using System.Windows.Forms;

    public static class ClipboardHelper
    {
        public static string GetText()
        {
            string result = string.Empty;

            RunOnSTAThread(() =>
            {
                try
                {
                    if (Clipboard.ContainsText())
                    {
                        result = Clipboard.GetText();
                    }
                }
                catch
                {
                    result = string.Empty;
                }
            });

            return result;
        }

        public static void SetText(string text)
        {
            if (text == null)
            {
                text = string.Empty;
            }

            RunOnSTAThread(() =>
            {
                try
                {
                    Clipboard.SetText(text, TextDataFormat.UnicodeText);
                }
                catch
                {
                }
            });
        }

        public static void SetHtml(string html)
        {
            if (html == null)
            {
                html = string.Empty;
            }

            RunOnSTAThread(() =>
            {
                try
                {
                    var header = BuildHtmlClipboardHeader(html);
                    var fullData = header + html;

                    Clipboard.SetText(fullData, TextDataFormat.Html);
                }
                catch
                {
                }
            });
        }

        private static string BuildHtmlClipboardHeader(string html)
        {
            var encoding = Encoding.UTF8;
            var htmlBytes = encoding.GetBytes(html);

            var headerBuilder = new StringBuilder();
            headerBuilder.AppendLine("Version:1.0");
            headerBuilder.AppendLine("StartHTML:0000000000");
            headerBuilder.AppendLine("EndHTML:0000000000");
            headerBuilder.AppendLine("StartFragment:0000000000");
            headerBuilder.AppendLine("EndFragment:0000000000");

            var headerPrefix = headerBuilder.ToString();
            var headerPrefixBytes = encoding.GetBytes(headerPrefix);

            var startHtml = headerPrefixBytes.Length;
            var endHtml = startHtml + htmlBytes.Length;
            var startFragment = startHtml;
            var endFragment = endHtml;

            headerBuilder = new StringBuilder();
            headerBuilder.AppendLine("Version:1.0");
            headerBuilder.AppendLine(string.Format("StartHTML:{0:D10}", startHtml));
            headerBuilder.AppendLine(string.Format("EndHTML:{0:D10}", endHtml));
            headerBuilder.AppendLine(string.Format("StartFragment:{0:D10}", startFragment));
            headerBuilder.AppendLine(string.Format("EndFragment:{0:D10}", endFragment));

            return headerBuilder.ToString();
        }

        private static void RunOnSTAThread(Action action)
        {
            var thread = Thread.CurrentThread;
            if (thread.GetApartmentState() == ApartmentState.STA)
            {
                action();
                return;
            }

            Exception capturedException = null;
            var manualEvent = new ManualResetEvent(false);

            var staThread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    capturedException = ex;
                }
                finally
                {
                    manualEvent.Set();
                }
            });

            staThread.SetApartmentState(ApartmentState.STA);
            staThread.IsBackground = true;
            staThread.Start();

            manualEvent.WaitOne();

            if (capturedException != null)
            {
                throw capturedException;
            }
        }
    }
}
