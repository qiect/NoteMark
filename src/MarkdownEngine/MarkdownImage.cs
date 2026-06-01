namespace NoteMark.MarkdownEngine;

public sealed class MarkdownImage
{
    public string OriginalPath { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public string? LocalPath { get; set; }
    public string? Base64Data { get; set; }
    public string? MimeType { get; set; }
    public string? RemoteUrl { get; set; }
    public bool IsRemote => !string.IsNullOrEmpty(RemoteUrl);
    public bool IsBase64 => !string.IsNullOrEmpty(Base64Data);
}
