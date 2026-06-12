namespace OneMarkDotNet.Elements
{
    using System.Collections.Generic;

    public sealed class ListItem
    {
        public string Content { get; set; }
        public List<ListItem> Children { get; set; }

        public ListItem()
        {
            Content = string.Empty;
            Children = new List<ListItem>();
        }

        public ListItem(string content)
        {
            Content = content;
            Children = new List<ListItem>();
        }

        public ListItem(string content, List<ListItem> children)
        {
            Content = content;
            Children = children;
        }
    }

    public sealed class ListElement : BlockElement
    {
        public bool IsOrdered { get; set; }
        public List<ListItem> Items { get; set; }

        public ListElement()
        {
            ElementType = "List";
            IsOrdered = false;
            Items = new List<ListItem>();
        }

        public ListElement(bool isOrdered, List<ListItem> items)
        {
            ElementType = "List";
            IsOrdered = isOrdered;
            Items = items;
        }
    }
}
