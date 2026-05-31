namespace OneMarkDotNet.RenderingServices;

public sealed class RenderResult
{
    public required bool Success { get; init; }
    public byte[]? ImageData { get; init; }
    public string? HtmlContent { get; init; }
    public string? ErrorMessage { get; init; }
    public TimeSpan RenderTime { get; init; }

    public static RenderResult Succeeded(byte[]? imageData = null, string? htmlContent = null, TimeSpan? renderTime = null) => new()
    {
        Success = true,
        ImageData = imageData,
        HtmlContent = htmlContent,
        RenderTime = renderTime ?? TimeSpan.Zero
    };

    public static RenderResult Failed(string errorMessage) => new()
    {
        Success = false,
        ErrorMessage = errorMessage,
        RenderTime = TimeSpan.Zero
    };
}
