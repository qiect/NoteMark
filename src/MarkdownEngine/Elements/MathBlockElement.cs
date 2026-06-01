namespace NoteMark.MarkdownEngine.Elements;

public class MathBlockElement : BlockElement
{
    public override string ElementType => "MathBlock";
    public required string Formula { get; set; }
    public bool IsInline { get; set; }
}
