using System.Buffers;
using System.Text;
using System.Text.RegularExpressions;
using OneMarkDotNet.MarkdownEngine;

namespace OneMarkDotNet.ImportExport;

public sealed class ImageHandler
{
    static readonly HashSet<string> SupportedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".png", ".jpg", ".jpeg", ".gif", ".svg", ".bmp"
    };

    static readonly Dictionary<string, string> ExtensionToMimeType = new(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".gif"] = "image/gif",
        [".svg"] = "image/svg+xml",
        [".bmp"] = "image/bmp"
    };

    private static readonly Regex ImagePatternRegex =
        new Regex(@"!\[([^\]]*)\]\(([^)]+)\)", RegexOptions.Compiled);

    private static readonly Regex Base64ImagePatternRegex =
        new Regex(@"data:image/([a-zA-Z+]+);base64,([A-Za-z0-9+/=]+)", RegexOptions.Compiled);

    private static readonly Regex OneNoteImagePatternRegex =
        new Regex(@"<one:Image[^>]*>.*?<one:Data>([^<]+)</one:Data>.*?</one:Image>",
            RegexOptions.Singleline | RegexOptions.Compiled);

    public List<MarkdownImage> ExtractImagesFromMarkdown(string markdown, string baseDirectory)
    {
        var images = new List<MarkdownImage>();
        var matches = ImagePatternRegex.Matches(markdown);

        foreach (Match match in matches)
        {
            var altText = match.Groups[1].Value;
            var path = match.Groups[2].Value;

            var image = new MarkdownImage
            {
                AltText = altText,
                OriginalPath = path
            };

            if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                image.RemoteUrl = path;
            }
            else if (path.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
            {
                var base64Match = Base64ImagePatternRegex.Match(path);
                if (base64Match.Success)
                {
                    image.Base64Data = base64Match.Groups[2].Value;
                    image.MimeType = $"image/{base64Match.Groups[1].Value}";
                }
            }
            else
            {
                var resolvedPath = ResolveRelativePath(path, baseDirectory);
                image.LocalPath = resolvedPath;
                image.MimeType = GetMimeType(resolvedPath);
            }

            images.Add(image);
        }

        return images;
    }

    public async Task<string> ConvertLocalImagesToBase64(string markdown, string baseDirectory)
    {
        var result = markdown;
        var matches = ImagePatternRegex.Matches(markdown);

        foreach (Match match in matches)
        {
            var path = match.Groups[2].Value;

            if (path.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("https://", StringComparison.OrdinalIgnoreCase) ||
                path.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
                continue;

            var resolvedPath = ResolveRelativePath(path, baseDirectory);
            if (!File.Exists(resolvedPath))
                continue;

            var mimeType = GetMimeType(resolvedPath);
            var base64 = await ReadFileAsBase64Async(resolvedPath);
            var newDataUri = $"data:{mimeType};base64,{base64}";
            var altText = match.Groups[1].Value;
            var replacement = $"![{altText}]({newDataUri})";
            result = result.Replace(match.Value, replacement);
        }

        return result;
    }

    public async Task<(string Markdown, List<MarkdownImage> SavedImages)> ConvertBase64ToLocalFiles(
        string markdown, string outputDirectory)
    {
        var result = markdown;
        var savedImages = new List<MarkdownImage>();
        var assetsDir = Path.Combine(outputDirectory, "assets");
        Directory.CreateDirectory(assetsDir);

        var matches = ImagePatternRegex.Matches(markdown);
        var imageIndex = 0;

        foreach (Match match in matches)
        {
            var path = match.Groups[2].Value;
            var altText = match.Groups[1].Value;

            if (!path.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
                continue;

            var base64Match = Base64ImagePatternRegex.Match(path);
            if (!base64Match.Success)
                continue;

            var format = base64Match.Groups[1].Value;
            var base64Data = base64Match.Groups[2].Value;
            var extension = format switch
            {
                "png" => ".png",
                "jpeg" or "jpg" => ".jpg",
                "gif" => ".gif",
                "svg+xml" => ".svg",
                "bmp" => ".bmp",
                _ => ".png"
            };

            var fileName = string.IsNullOrEmpty(altText)
                ? $"image_{imageIndex:D3}{extension}"
                : $"{SanitizeFileName(altText)}{extension}";

            var filePath = Path.Combine(assetsDir, fileName);
            await WriteBase64AsFileAsync(base64Data, filePath);

            var relativePath = $"assets/{fileName}";
            var replacement = $"![{altText}]({relativePath})";
            result = result.Replace(match.Value, replacement);

            savedImages.Add(new MarkdownImage
            {
                AltText = altText,
                OriginalPath = path,
                LocalPath = filePath,
                Base64Data = base64Data,
                MimeType = $"image/{format}"
            });

            imageIndex++;
        }

        return (result, savedImages);
    }

    public async Task<byte[]?> DownloadRemoteImage(string url)
    {
        try
        {
            using var httpClient = new HttpClient();
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            return await httpClient.GetByteArrayAsync(url);
        }
        catch (Exception)
        {
            return null;
        }
    }

    public string GetImageFormat(string filePath)
    {
        var extension = Path.GetExtension(filePath).ToLowerInvariant();
        return extension switch
        {
            ".png" => "png",
            ".jpg" or ".jpeg" => "jpeg",
            ".gif" => "gif",
            ".svg" => "svg",
            ".bmp" => "bmp",
            _ => DetectImageFormatFromHeader(filePath)
        };
    }

    public string GetMimeType(string filePath)
    {
        var extension = Path.GetExtension(filePath);
        return ExtensionToMimeType.TryGetValue(extension, out var mime) ? mime : "application/octet-stream";
    }

    public List<MarkdownImage> ExtractOneNoteImagesFromXml(string xml)
    {
        var images = new List<MarkdownImage>();
        var matches = OneNoteImagePatternRegex.Matches(xml);

        foreach (Match match in matches)
        {
            var base64Data = match.Groups[1].Value;
            var format = DetectFormatFromBase64(base64Data);
            var mimeType = $"image/{format}";

            images.Add(new MarkdownImage
            {
                Base64Data = base64Data,
                MimeType = mimeType,
                OriginalPath = $"onenote-embed://{Guid.NewGuid()}"
            });
        }

        return images;
    }

    static string ResolveRelativePath(string path, string baseDirectory)
    {
        if (Path.IsPathRooted(path))
            return path;

        return Path.GetFullPath(Path.Combine(baseDirectory, path));
    }

    static async Task<string> ReadFileAsBase64Async(string filePath)
    {
        using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read,
            FileShare.Read, bufferSize: 81920, useAsync: true);
        var buffer = ArrayPool<byte>.Shared.Rent(81920);
        try
        {
            using var ms = new MemoryStream();
            int bytesRead;
            while ((bytesRead = await fs.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await ms.WriteAsync(buffer, 0, bytesRead);
            }

            return Convert.ToBase64String(ms.ToArray());
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    static async Task WriteBase64AsFileAsync(string base64Data, string filePath)
    {
        var bytes = Convert.FromBase64String(base64Data);
        using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write,
            FileShare.None, bufferSize: 81920, useAsync: true);
        await fs.WriteAsync(bytes, 0, bytes.Length);
    }

    static string DetectImageFormatFromHeader(string filePath)
    {
        try
        {
            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 8, useAsync: false);
            var header = new byte[8];
            var read = fs.Read(header, 0, header.Length);

            if (read >= 8 && header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47)
                return "png";
            if (read >= 2 && header[0] == 0xFF && header[1] == 0xD8)
                return "jpeg";
            if (read >= 4 && header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46)
                return "gif";
            if (read >= 2 && header[0] == 0x42 && header[1] == 0x4D)
                return "bmp";

            return "unknown";
        }
        catch
        {
            return "unknown";
        }
    }

    static string DetectFormatFromBase64(string base64Data)
    {
        try
        {
            var sliceLen = Math.Min(base64Data.Length, 32);
            var bytes = Convert.FromBase64String(base64Data.Substring(0, sliceLen));
            if (bytes.Length >= 8 && bytes[0] == 0x89 && bytes[1] == 0x50 && bytes[2] == 0x4E && bytes[3] == 0x47)
                return "png";
            if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xD8)
                return "jpeg";
            if (bytes.Length >= 4 && bytes[0] == 0x47 && bytes[1] == 0x49 && bytes[2] == 0x46)
                return "gif";
            if (bytes.Length >= 2 && bytes[0] == 0x42 && bytes[1] == 0x4D)
                return "bmp";
        }
        catch
        {
        }

        return "png";
    }

    static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var result = new StringBuilder(name.Length);
        foreach (var c in name)
        {
            if (Array.IndexOf(invalid, c) >= 0)
                result.Append('_');
            else
                result.Append(c);
        }

        return result.ToString();
    }
}
