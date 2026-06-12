namespace NoteMark.Elements
{
    public sealed class MathBlockElement : BlockElement
    {
        public string Formula { get; set; }

        public MathBlockElement()
        {
            ElementType = "MathBlock";
            Formula = string.Empty;
        }

        public MathBlockElement(string formula)
        {
            ElementType = "MathBlock";
            Formula = formula;
        }
    }
}
