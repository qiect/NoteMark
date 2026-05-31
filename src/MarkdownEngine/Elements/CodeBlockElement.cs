namespace OneMarkDotNet.MarkdownEngine.Elements;

public class CodeBlockElement : BlockElement
{
    public override string ElementType => "CodeBlock";
    public string Language { get; set; } = "";
    public required string Code { get; set; }
    public List<int> LineNumbers { get; set; } = [];
}
