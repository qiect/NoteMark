namespace NoteMark.Elements
{
    using System.Collections.Generic;

    public sealed class StrikethroughElement : InlineElement
    {
        public List<InlineElement> Children { get; set; }

        public StrikethroughElement()
        {
            ElementType = "Strikethrough";
            Children = new List<InlineElement>();
        }

        public StrikethroughElement(List<InlineElement> children)
        {
            ElementType = "Strikethrough";
            Children = children;
        }
    }
}
