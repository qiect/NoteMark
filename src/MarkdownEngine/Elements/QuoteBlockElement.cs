namespace OneMarkDotNet.MarkdownEngine.Elements;

public class QuoteBlockElement : BlockElement
{
    public override string ElementType => "QuoteBlock";
    public List<BlockElement> Children { get; set; } = [];
    public bool HasHeadingIcon { get; set; }
}
