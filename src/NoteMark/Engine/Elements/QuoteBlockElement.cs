namespace NoteMark.Elements
{
    using System.Collections.Generic;

    public sealed class QuoteBlockElement : BlockElement
    {
        public List<BlockElement> Blocks { get; set; }

        public QuoteBlockElement()
        {
            ElementType = "QuoteBlock";
            Blocks = new List<BlockElement>();
        }

        public QuoteBlockElement(List<BlockElement> blocks)
        {
            ElementType = "QuoteBlock";
            Blocks = blocks;
        }
    }
}
