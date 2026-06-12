namespace NoteMark.Elements
{
    public sealed class ImageElement : InlineElement
    {
        public string AltText { get; set; }
        public string Url { get; set; }
        public string Data { get; set; }

        public ImageElement()
        {
            ElementType = "Image";
            AltText = string.Empty;
            Url = string.Empty;
            Data = string.Empty;
        }

        public ImageElement(string altText, string url)
        {
            ElementType = "Image";
            AltText = altText;
            Url = url;
            Data = string.Empty;
        }

        public ImageElement(string altText, string url, string data)
        {
            ElementType = "Image";
            AltText = altText;
            Url = url;
            Data = data;
        }
    }
}
