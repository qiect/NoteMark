namespace OneMarkDotNet.Elements
{
    using System.Collections.Generic;

    public sealed class BoldElement : InlineElement
    {
        public List<InlineElement> Children { get; set; }

        public BoldElement()
        {
            ElementType = "Bold";
            Children = new List<InlineElement>();
        }

        public BoldElement(List<InlineElement> children)
        {
            ElementType = "Bold";
            Children = children;
        }
    }
}
