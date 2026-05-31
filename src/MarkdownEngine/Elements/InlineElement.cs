namespace OneMarkDotNet.MarkdownEngine.Elements;

public abstract class InlineElement;

public class TextElement : InlineElement
{
    public required string Text { get; set; }
}

public class BoldElement : InlineElement
{
    public List<InlineElement> Inlines { get; set; } = [];
}

public class ItalicElement : InlineElement
{
    public List<InlineElement> Inlines { get; set; } = [];
}

public class StrikethroughElement : InlineElement
{
    public List<InlineElement> Inlines { get; set; } = [];
}

public class CodeInlineElement : InlineElement
{
    public required string Code { get; set; }
}

public class LinkElement : InlineElement
{
    public required string Url { get; set; }
    public string Text { get; set; } = "";
    public string Title { get; set; } = "";
}

public class ImageElement : InlineElement
{
    public required string Url { get; set; }
    public string Alt { get; set; } = "";
    public string Title { get; set; } = "";
}

public class MathInlineElement : InlineElement
{
    public required string Formula { get; set; }
}
