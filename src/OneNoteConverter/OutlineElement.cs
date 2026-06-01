namespace NoteMark.OneNoteConverter;

public enum ElementType
{
    Text,
    Heading,
    CodeBlock,
    Quote,
    Table,
    List,
    Image,
    Math,
    Diagram
}

public sealed record OutlineElement
{
    public ElementType Type { get; init; }
    public string Content { get; init; } = string.Empty;
    public OneNoteStyle Style { get; init; } = new();
    public int Level { get; init; }
    public List<OutlineElement> Children { get; init; } = [];
}

public sealed record OneNoteStyle
{
    public string? FontFamily { get; init; }
    public string? FontColor { get; init; }
    public string? HighlightColor { get; init; }
    public double? FontSize { get; init; }
    public bool Bold { get; init; }
    public bool Italic { get; init; }
    public bool Underline { get; init; }
    public bool Strikethrough { get; init; }
    public bool Superscript { get; init; }
    public bool Subscript { get; init; }

    public static OneNoteStyle Default { get; } = new();

    public bool IsEmpty =>
        FontFamily is null &&
        FontColor is null &&
        HighlightColor is null &&
        FontSize is null &&
        !Bold &&
        !Italic &&
        !Underline &&
        !Strikethrough &&
        !Superscript &&
        !Subscript;
}
