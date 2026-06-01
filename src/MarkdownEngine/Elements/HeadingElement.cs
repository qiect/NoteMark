namespace NoteMark.MarkdownEngine.Elements;

public class HeadingElement : BlockElement
{
    public override string ElementType => "Heading";
    public required int Level { get; set; }
    public List<InlineElement> Inlines { get; set; } = [];
}
