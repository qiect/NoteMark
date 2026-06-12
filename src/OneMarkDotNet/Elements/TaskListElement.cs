namespace OneMarkDotNet.Elements
{
    using System.Collections.Generic;

    public sealed class TaskItem
    {
        public string Content { get; set; }
        public bool IsChecked { get; set; }
        public List<TaskItem> Children { get; set; }

        public TaskItem()
        {
            Content = string.Empty;
            IsChecked = false;
            Children = new List<TaskItem>();
        }

        public TaskItem(string content, bool isChecked)
        {
            Content = content;
            IsChecked = isChecked;
            Children = new List<TaskItem>();
        }

        public TaskItem(string content, bool isChecked, List<TaskItem> children)
        {
            Content = content;
            IsChecked = isChecked;
            Children = children;
        }
    }

    public sealed class TaskListElement : BlockElement
    {
        public List<TaskItem> Items { get; set; }

        public TaskListElement()
        {
            ElementType = "TaskList";
            Items = new List<TaskItem>();
        }

        public TaskListElement(List<TaskItem> items)
        {
            ElementType = "TaskList";
            Items = items;
        }
    }
}
