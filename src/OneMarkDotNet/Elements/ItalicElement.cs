namespace OneMarkDotNet.Elements
{
    using System.Collections.Generic;

    public sealed class ItalicElement : InlineElement
    {
        public List<InlineElement> Children { get; set; }

        public ItalicElement()
        {
            ElementType = "Italic";
            Children = new List<InlineElement>();
        }

        public ItalicElement(List<InlineElement> children)
        {
            ElementType = "Italic";
            Children = children;
        }
    }
}
