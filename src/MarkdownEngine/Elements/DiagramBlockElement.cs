namespace NoteMark.MarkdownEngine.Elements;

public enum DiagramType
{
    Mermaid,
    Flow,
    Sequence,
    Mindmap
}

public class DiagramBlockElement : BlockElement
{
    public override string ElementType => "DiagramBlock";
    public required DiagramType DiagramType { get; set; }
    public required string Content { get; set; }
}
