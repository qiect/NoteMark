namespace NoteMark.MarkdownEngine.Elements;

public enum TableColumnAlign
{
    Left,
    Center,
    Right
}

public class TableElement : BlockElement
{
    public override string ElementType => "Table";
    public List<List<InlineElement>> Headers { get; set; } = [];
    public List<TableRow> Rows { get; set; } = [];
    public List<TableColumnAlign?> Alignments { get; set; } = [];
}

public class TableRow
{
    public List<List<InlineElement>> Cells { get; set; } = [];
}
