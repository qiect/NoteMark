namespace OneMarkDotNet.Elements
{
    using System.Collections.Generic;

    public abstract class BlockElement
    {
        public string ElementType { get; protected set; }
        public List<InlineElement> Children { get; set; } = new List<InlineElement>();
    }
}
