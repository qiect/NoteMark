namespace OneMarkDotNet.Elements
{
    public sealed class TextElement : InlineElement
    {
        public string Text { get; set; }

        public TextElement()
        {
            ElementType = "Text";
            Text = string.Empty;
        }

        public TextElement(string text)
        {
            ElementType = "Text";
            Text = text;
        }
    }
}
