namespace OneMarkDotNet
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Threading.Tasks;
    using OneMarkDotNet.Elements;

    public sealed class ImageInfo
    {
        public string Url { get; set; }
        public string AltText { get; set; }
        public byte[] Data { get; set; }

        public ImageInfo()
        {
            Url = string.Empty;
            AltText = string.Empty;
            Data = new byte[0];
        }

        public ImageInfo(string url, string altText, byte[] data)
        {
            Url = url;
            AltText = altText;
            Data = data ?? new byte[0];
        }
    }

    public sealed class ImageHandler
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        public List<ImageInfo> ExtractImages(MarkdownDocument doc)
        {
            var images = new List<ImageInfo>();
            if (doc == null)
            {
                return images;
            }

            foreach (var block in doc.Blocks)
            {
                ExtractImagesFromBlock(block, images);
            }

            return images;
        }

        public void Base64ToLocalFile(string base64Data, string outputPath)
        {
            if (string.IsNullOrEmpty(base64Data))
            {
                throw new ArgumentNullException(nameof(base64Data));
            }

            if (string.IsNullOrEmpty(outputPath))
            {
                throw new ArgumentNullException(nameof(outputPath));
            }

            var data = base64Data;

            if (data.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                var base64Index = data.IndexOf(";base64,", StringComparison.OrdinalIgnoreCase);
                if (base64Index >= 0)
                {
                    data = data.Substring(base64Index + ";base64,".Length);
                }
            }

            var bytes = Convert.FromBase64String(data);

            var directory = Path.GetDirectoryName(outputPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(outputPath, bytes);
        }

        public string LocalFileToBase64(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
            {
                throw new ArgumentNullException(nameof(filePath));
            }

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Image file not found.", filePath);
            }

            var bytes = File.ReadAllBytes(filePath);
            return Convert.ToBase64String(bytes);
        }

        public async Task<byte[]> DownloadRemoteImageAsync(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                throw new ArgumentNullException(nameof(url));
            }

            var response = await HttpClient.GetAsync(url).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsByteArrayAsync().ConfigureAwait(false);
        }

        private void ExtractImagesFromBlock(BlockElement block, List<ImageInfo> images)
        {
            if (block is ParagraphElement || block is HeadingElement)
            {
                foreach (var inline in block.Children)
                {
                    ExtractImagesFromInline(inline, images);
                }
            }
            else if (block is QuoteBlockElement quote)
            {
                foreach (var innerBlock in quote.Blocks)
                {
                    ExtractImagesFromBlock(innerBlock, images);
                }
            }
        }

        private void ExtractImagesFromInline(InlineElement inline, List<ImageInfo> images)
        {
            if (inline is ImageElement image)
            {
                byte[] data = new byte[0];
                if (!string.IsNullOrEmpty(image.Data))
                {
                    try
                    {
                        var base64Data = image.Data;
                        if (base64Data.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
                        {
                            var base64Index = base64Data.IndexOf(";base64,", StringComparison.OrdinalIgnoreCase);
                            if (base64Index >= 0)
                            {
                                base64Data = base64Data.Substring(base64Index + ";base64,".Length);
                            }
                        }

                        data = Convert.FromBase64String(base64Data);
                    }
                    catch
                    {
                        data = new byte[0];
                    }
                }

                images.Add(new ImageInfo(image.Url, image.AltText, data));
            }
            else if (inline is BoldElement bold)
            {
                foreach (var child in bold.Children)
                {
                    ExtractImagesFromInline(child, images);
                }
            }
            else if (inline is ItalicElement italic)
            {
                foreach (var child in italic.Children)
                {
                    ExtractImagesFromInline(child, images);
                }
            }
            else if (inline is StrikethroughElement strike)
            {
                foreach (var child in strike.Children)
                {
                    ExtractImagesFromInline(child, images);
                }
            }
        }
    }
}
