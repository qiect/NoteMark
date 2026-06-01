namespace NoteMark.MarkdownEngine.Elements;

public abstract class BlockElement
{
    public abstract string ElementType { get; }
}

public class ParagraphElement : BlockElement
{
    public override string ElementType => "Paragraph";
    public List<InlineElement> Inlines { get; set; } = [];
}

public class HorizontalRuleElement : BlockElement
{
    public override string ElementType => "HorizontalRule";
}

public class TaskListElement : BlockElement
{
    public override string ElementType => "TaskList";
    public List<TaskListItem> Items { get; set; } = [];
}

public class TaskListItem
{
    public bool IsChecked { get; set; }
    public List<InlineElement> Content { get; set; } = [];
}
