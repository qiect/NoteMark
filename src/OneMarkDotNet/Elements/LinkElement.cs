namespace OneMarkDotNet.Elements
{
    public sealed class LinkElement : InlineElement
    {
        public string Text { get; set; }
        public string Url { get; set; }

        public LinkElement()
        {
            ElementType = "Link";
            Text = string.Empty;
            Url = string.Empty;
        }

        public LinkElement(string text, string url)
        {
            ElementType = "Link";
            Text = text;
            Url = url;
        }
    }
}
