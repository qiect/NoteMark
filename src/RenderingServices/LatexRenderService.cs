using System.Diagnostics;
using System.Text.Json;

namespace OneMarkDotNet.RenderingServices;

public sealed class LatexRenderService : IAsyncDisposable
{
    private WebView2Helper? _webViewHelper;

    public async Task<RenderResult> RenderLatexToImageAsync(string formula, int dpi = 150)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await EnsureWebViewInitializedAsync();

            var html = HtmlTemplateGenerator.GenerateLatexHtml(formula, isInline: false);
            var message = await _webViewHelper!.NavigateAndWaitForMessageAsync(html);

            if (string.IsNullOrEmpty(message))
            {
                stopwatch.Stop();
                return RenderResult.Failed("LaTeX rendering timed out or returned empty result.");
            }

            var imageData = await _webViewHelper.CaptureScreenshotAsync();

            stopwatch.Stop();
            return RenderResult.Succeeded(imageData, html, stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return RenderResult.Failed(ex.Message);
        }
    }

    public async Task<RenderResult> RenderLatexToTextAsync(string formula)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await EnsureWebViewInitializedAsync();

            var html = HtmlTemplateGenerator.GenerateLatexHtml(formula, isInline: true);
            var message = await _webViewHelper!.NavigateAndWaitForMessageAsync(html);

            if (string.IsNullOrEmpty(message))
            {
                stopwatch.Stop();
                return RenderResult.Failed("LaTeX rendering timed out or returned empty result.");
            }

            string? renderedHtml = null;
            try
            {
                using var doc = JsonDocument.Parse(message);
                if (doc.RootElement.TryGetProperty("html", out var htmlElement))
                {
                    renderedHtml = htmlElement.GetString();
                }
                else if (doc.RootElement.TryGetProperty("error", out var errorElement))
                {
                    stopwatch.Stop();
                    return RenderResult.Failed(errorElement.GetString() ?? "Unknown LaTeX error.");
                }
            }
            catch (JsonException)
            {
                renderedHtml = message;
            }

            var textContent = ConvertHtmlToTextFormula(renderedHtml ?? string.Empty);

            stopwatch.Stop();
            return RenderResult.Succeeded(htmlContent: textContent, renderTime: stopwatch.Elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            return RenderResult.Failed(ex.Message);
        }
    }

    private static string ConvertHtmlToTextFormula(string html)
    {
        var text = html;
        text = System.Text.RegularExpressions.Regex.Replace(text, @"<span[^>]*class=""katex-mathml""[^>]*>.*?</span>", "", System.Text.RegularExpressions.RegexOptions.Singleline);
        text = System.Text.RegularExpressions.Regex.Replace(text, @"<span[^>]*class=""katex-html""[^>]*>", "", System.Text.RegularExpressions.RegexOptions.Singleline);
        text = System.Text.RegularExpressions.Regex.Replace(text, @"</span>", "", System.Text.RegularExpressions.RegexOptions.Singleline);
        text = System.Text.RegularExpressions.Regex.Replace(text, @"<span[^>]*>", "", System.Text.RegularExpressions.RegexOptions.Singleline);
        text = System.Text.RegularExpressions.Regex.Replace(text, @"<m[a-z]+[^>]*>", "", System.Text.RegularExpressions.RegexOptions.Singleline);
        text = System.Text.RegularExpressions.Regex.Replace(text, @"</m[a-z]+>", "", System.Text.RegularExpressions.RegexOptions.Singleline);
        text = System.Text.RegularExpressions.Regex.Replace(text, @"<[^>]+>", "", System.Text.RegularExpressions.RegexOptions.Singleline);
        text = System.Text.RegularExpressions.Regex.Replace(text, @"\s+", " ").Trim();
        return text;
    }

    private async Task EnsureWebViewInitializedAsync()
    {
        if (_webViewHelper is not null && _webViewHelper.IsInitialized)
            return;

        _webViewHelper?.DisposeAsync().AsTask().Wait();

        _webViewHelper = new WebView2Helper();
        await WebView2Helper.EnsureWebView2RuntimeAsync();
        await _webViewHelper.CreateWebViewAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_webViewHelper is not null)
        {
            await _webViewHelper.DisposeAsync();
            _webViewHelper = null;
        }
    }
}
