using System.Diagnostics;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.WinForms;

namespace OneMarkDotNet.RenderingServices;

public sealed class WebView2Helper : IAsyncDisposable
{
    private WebView2? _webView;
    private TaskCompletionSource<string>? _messageTcs;
    private Action<string>? _onMessageReceived;
    private bool _isInitialized;

    public CoreWebView2? CoreWebView => _webView?.CoreWebView2;
    public bool IsInitialized => _isInitialized && _webView?.CoreWebView2 is not null;

    public static async Task EnsureWebView2RuntimeAsync()
    {
        try
        {
            var version = CoreWebView2Environment.GetAvailableBrowserVersionString();
            if (!string.IsNullOrEmpty(version))
                return;
        }
        catch (WebView2RuntimeNotFoundException)
        {
            await InstallWebView2RuntimeAsync();
        }
    }

    public async Task<WebView2> CreateWebViewAsync(Action<string>? onMessageReceived = null)
    {
        _onMessageReceived = onMessageReceived;

        _webView = new WebView2
        {
            Visible = false
        };

        var environment = await CoreWebView2Environment.CreateAsync();
        await _webView.EnsureCoreWebView2Async(environment);

        _webView.CoreWebView2.WebMessageReceived += OnWebMessageReceived;

        _isInitialized = true;
        return _webView;
    }

    public async Task<string> ExecuteScriptAsync(string script)
    {
        if (_webView?.CoreWebView2 is null)
            throw new InvalidOperationException("WebView2 is not initialized.");

        var result = await _webView.CoreWebView2.ExecuteScriptAsync(script);
        return result;
    }

    public async Task<byte[]> CaptureScreenshotAsync()
    {
        if (_webView?.CoreWebView2 is null)
            throw new InvalidOperationException("WebView2 is not initialized.");

        using var ms = new MemoryStream();
        await _webView.CoreWebView2.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, ms);
        return ms.ToArray();
    }

    public async Task<string> NavigateAndWaitForMessageAsync(string htmlContent)
    {
        _messageTcs = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);

        if (_webView?.CoreWebView2 is null)
            throw new InvalidOperationException("WebView2 is not initialized.");

        _webView.CoreWebView2.NavigateToString(htmlContent);

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        cts.Token.Register(() => _messageTcs.TrySetResult(string.Empty));

        return await _messageTcs.Task;
    }

    private void OnWebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        var message = e.TryGetWebMessageAsString() ?? e.WebMessageAsJson ?? string.Empty;

        _onMessageReceived?.Invoke(message);

        _messageTcs?.TrySetResult(message);
    }

    public async Task NavigateAsync(string htmlContent)
    {
        if (_webView?.CoreWebView2 is null)
            throw new InvalidOperationException("WebView2 is not initialized.");

        _webView.CoreWebView2.NavigateToString(htmlContent);
        await _webView.CoreWebView2.ExecuteScriptAsync("document.readyState === 'complete'");
    }

    public ValueTask DisposeAsync()
    {
        if (_webView is not null)
        {
            if (_webView.CoreWebView2 is not null)
            {
                _webView.CoreWebView2.WebMessageReceived -= OnWebMessageReceived;
            }

            _webView.Dispose();
            _webView = null;
        }

        _isInitialized = false;
        _messageTcs?.TrySetCanceled();
        _messageTcs = null;

        return ValueTask.CompletedTask;
    }

    private static async Task InstallWebView2RuntimeAsync()
    {
        var process = Process.Start(new ProcessStartInfo
        {
            FileName = "https://go.microsoft.com/fwlink/p/?LinkId=2124703",
            UseShellExecute = true
        });

        if (process is not null)
        {
            await process.WaitForExitAsync();
        }
    }
}
