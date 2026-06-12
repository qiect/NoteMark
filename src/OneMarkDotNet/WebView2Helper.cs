namespace OneMarkDotNet
{
    using System;
    using System.IO;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Windows.Forms;
    using Microsoft.Web.WebView2.Core;
    using Microsoft.Web.WebView2.WinForms;

    public sealed class WebView2Helper : IDisposable
    {
        private Form hostForm;
        private WebView2 webView;
        private TaskCompletionSource<string> navigateCompletionSource;
        private TaskCompletionSource<byte[]> screenshotCompletionSource;
        private bool disposed;

        public event Action<string> MessageReceived;

        public WebView2Helper()
        {
            hostForm = new Form
            {
                Width = 1200,
                Height = 800,
                ShowInTaskbar = false,
                FormBorderStyle = FormBorderStyle.FixedToolWindow,
                StartPosition = FormStartPosition.Manual,
                Left = -32000,
                Top = -32000
            };

            webView = new WebView2
            {
                Dock = DockStyle.Fill
            };

            hostForm.Controls.Add(webView);
            webView.WebMessageReceived += OnWebMessageReceived;
        }

        public async Task InitializeAsync()
        {
            ThrowIfDisposed();

            var initTask = new TaskCompletionSource<bool>();

            hostForm.Shown += async (s, e) =>
            {
                try
                {
                    await webView.EnsureCoreWebView2Async(null);
                    initTask.SetResult(true);
                }
                catch (Exception ex)
                {
                    initTask.SetException(ex);
                }
            };

            hostForm.Show();

            await initTask.Task;
        }

        public async Task NavigateToStringAsync(string html)
        {
            ThrowIfDisposed();

            if (webView.CoreWebView2 == null)
            {
                throw new InvalidOperationException("WebView2 is not initialized. Call InitializeAsync first.");
            }

            navigateCompletionSource = new TaskCompletionSource<string>();

            webView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;
            webView.CoreWebView2.NavigateToString(html);

            await navigateCompletionSource.Task;

            webView.CoreWebView2.NavigationCompleted -= OnNavigationCompleted;
        }

        public async Task<string> ExecuteScriptAsync(string script)
        {
            ThrowIfDisposed();

            if (webView.CoreWebView2 == null)
            {
                throw new InvalidOperationException("WebView2 is not initialized. Call InitializeAsync first.");
            }

            var result = await webView.CoreWebView2.ExecuteScriptAsync(script);
            return result;
        }

        public async Task<byte[]> CaptureScreenshotAsync()
        {
            ThrowIfDisposed();

            if (webView.CoreWebView2 == null)
            {
                throw new InvalidOperationException("WebView2 is not initialized. Call InitializeAsync first.");
            }

            screenshotCompletionSource = new TaskCompletionSource<byte[]>();

            using (var memoryStream = new MemoryStream())
            {
                await webView.CoreWebView2.CapturePreviewAsync(
                    CoreWebView2CapturePreviewImageFormat.Png,
                    memoryStream);

                return memoryStream.ToArray();
            }
        }

        private void OnNavigationCompleted(object sender, CoreWebView2NavigationCompletedEventArgs e)
        {
            if (navigateCompletionSource != null)
            {
                if (e.IsSuccess)
                {
                    navigateCompletionSource.SetResult(e.NavigationId.ToString());
                }
                else
                {
                    navigateCompletionSource.SetException(
                        new InvalidOperationException("Navigation failed with error: " + e.WebErrorStatus));
                }
            }
        }

        private void OnWebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            var message = e.TryGetWebMessageAsString();
            var handler = MessageReceived;
            if (handler != null)
            {
                handler(message);
            }
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(WebView2Helper));
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;

            if (webView != null)
            {
                webView.WebMessageReceived -= OnWebMessageReceived;
                webView.Dispose();
                webView = null;
            }

            if (hostForm != null)
            {
                hostForm.Close();
                hostForm.Dispose();
                hostForm = null;
            }
        }
    }
}
