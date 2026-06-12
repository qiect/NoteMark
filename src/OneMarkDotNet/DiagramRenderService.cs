namespace OneMarkDotNet
{
    using System;
    using System.Diagnostics;
    using System.Threading.Tasks;

    public sealed class DiagramRenderService : IDisposable
    {
        private WebView2Helper webViewHelper;
        private bool disposed;
        private readonly object syncLock = new object();

        public async Task<byte[]> RenderDiagramAsync(string code, string diagramType)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(code))
            {
                throw new ArgumentException("Diagram code cannot be null or empty.", nameof(code));
            }

            var stopwatch = Stopwatch.StartNew();
            byte[] imageData = null;
            string errorMessage = null;

            try
            {
                var helper = GetOrCreateWebView2Helper();
                var renderCompleted = new TaskCompletionSource<bool>();

                Action<string> messageHandler = null;
                messageHandler = (message) =>
                {
                    if (message == "render-complete")
                    {
                        renderCompleted.SetResult(true);
                    }
                    else if (message.StartsWith("render-error:"))
                    {
                        renderCompleted.SetException(new InvalidOperationException(
                            "Diagram rendering failed: " + message.Substring("render-error:".Length)));
                    }
                };

                helper.MessageReceived += messageHandler;

                try
                {
                    var html = HtmlTemplateGenerator.GenerateMermaidTemplate(code);
                    await helper.NavigateToStringAsync(html);

                    var delayTask = Task.Delay(60000);
                    var completedTask = await Task.WhenAny(renderCompleted.Task, delayTask);

                    if (completedTask == delayTask)
                    {
                        throw new TimeoutException("Diagram rendering timed out after 60 seconds.");
                    }

                    await renderCompleted.Task;

                    await Task.Delay(500);

                    imageData = await helper.CaptureScreenshotAsync();
                }
                finally
                {
                    helper.MessageReceived -= messageHandler;
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }

            stopwatch.Stop();

            if (imageData != null)
            {
                return imageData;
            }

            throw new InvalidOperationException(
                "Failed to render diagram to image" +
                (string.IsNullOrEmpty(errorMessage) ? "." : ": " + errorMessage));
        }

        private WebView2Helper GetOrCreateWebView2Helper()
        {
            lock (syncLock)
            {
                if (webViewHelper == null)
                {
                    webViewHelper = new WebView2Helper();
                    webViewHelper.InitializeAsync().GetAwaiter().GetResult();
                }

                return webViewHelper;
            }
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(DiagramRenderService));
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;

            lock (syncLock)
            {
                if (webViewHelper != null)
                {
                    webViewHelper.Dispose();
                    webViewHelper = null;
                }
            }
        }
    }
}
