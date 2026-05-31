namespace OneMarkDotNet.MarkdownEngine.Elements;

public class ListElement : BlockElement
{
    public override string ElementType => "List";
    public List<ListItem> Items { get; set; } = [];
    public bool IsOrdered { get; set; }
    public bool IsTaskList { get; set; }
}

public class ListItem
{
    public List<InlineElement> Content { get; set; } = [];
    public bool IsChecked { get; set; }
    public List<ListElement> NestedLists { get; set; } = [];
}
