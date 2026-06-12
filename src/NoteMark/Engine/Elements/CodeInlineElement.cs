namespace NoteMark.Elements
{
    public sealed class CodeInlineElement : InlineElement
    {
        public string Code { get; set; }

        public CodeInlineElement()
        {
            ElementType = "CodeInline";
            Code = string.Empty;
        }

        public CodeInlineElement(string code)
        {
            ElementType = "CodeInline";
            Code = code;
        }
    }
}
