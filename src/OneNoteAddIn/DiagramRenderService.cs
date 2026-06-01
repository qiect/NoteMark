using System.Diagnostics;
using NoteMark.RenderingServices;

namespace NoteMark.AddIn;

public sealed class DiagramRenderService : IDisposable
{
    private WebView2Helper? _webViewHelper;

    public async Task<RenderResult> RenderDiagramAsync(string content, DiagramType type, int width = 800, int height = 600)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            await EnsureWebViewInitializedAsync();

            var html = type switch
            {
                DiagramType.Mermaid => HtmlTemplateGenerator.GenerateMermaidHtml(content),
                DiagramType.Flowchart => HtmlTemplateGenerator.GenerateFlowchartHtml(content),
                DiagramType.Sequence => HtmlTemplateGenerator.GenerateSequenceHtml(content),
                DiagramType.Mindmap => HtmlTemplateGenerator.GenerateMindmapHtml(content),
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };

            var message = await _webViewHelper!.NavigateAndWaitForMessageAsync(html);

            if (string.IsNullOrEmpty(message))
            {
                stopwatch.Stop();
                return RenderResult.Failed("Rendering timed out or returned empty result.");
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

    private async Task EnsureWebViewInitializedAsync()
    {
        if (_webViewHelper is not null && _webViewHelper.IsInitialized)
            return;

        _webViewHelper?.Dispose();

        _webViewHelper = new WebView2Helper();
        await WebView2Helper.EnsureWebView2RuntimeAsync();
        await _webViewHelper.CreateWebViewAsync();
    }

    public void Dispose()
    {
        _webViewHelper?.Dispose();
        _webViewHelper = null;
    }
}
