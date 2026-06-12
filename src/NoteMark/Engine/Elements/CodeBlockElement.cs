namespace NoteMark.Elements
{
    public sealed class CodeBlockElement : BlockElement
    {
        public string Language { get; set; }
        public string Code { get; set; }

        public CodeBlockElement()
        {
            ElementType = "CodeBlock";
            Language = string.Empty;
            Code = string.Empty;
        }

        public CodeBlockElement(string language, string code)
        {
            ElementType = "CodeBlock";
            Language = language;
            Code = code;
        }
    }
}
