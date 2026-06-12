namespace OneMarkDotNet.Elements
{
    public sealed class MathInlineElement : InlineElement
    {
        public string Formula { get; set; }

        public MathInlineElement()
        {
            ElementType = "MathInline";
            Formula = string.Empty;
        }

        public MathInlineElement(string formula)
        {
            ElementType = "MathInline";
            Formula = formula;
        }
    }
}
