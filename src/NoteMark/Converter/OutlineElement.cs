namespace NoteMark
{
    using System.Collections.Generic;

    public enum OutlineElementType
    {
        Paragraph,
        Heading,
        List,
        Table,
        CodeBlock,
        QuoteBlock,
        Image,
        HorizontalRule
    }

    public class OutlineElement
    {
        public OutlineElementType Type { get; set; }
        public string Content { get; set; }
        public int Level { get; set; }
        public ConverterOneNoteStyle Style { get; set; }
        public List<OutlineElement> Children { get; set; }

        public OutlineElement()
        {
            Type = OutlineElementType.Paragraph;
            Content = string.Empty;
            Level = 0;
            Style = new ConverterOneNoteStyle();
            Children = new List<OutlineElement>();
        }

        public OutlineElement(OutlineElementType type)
        {
            Type = type;
            Content = string.Empty;
            Level = 0;
            Style = new ConverterOneNoteStyle();
            Children = new List<OutlineElement>();
        }

        public OutlineElement(OutlineElementType type, string content)
        {
            Type = type;
            Content = content;
            Level = 0;
            Style = new ConverterOneNoteStyle();
            Children = new List<OutlineElement>();
        }

        public OutlineElement(OutlineElementType type, string content, int level)
        {
            Type = type;
            Content = content;
            Level = level;
            Style = new ConverterOneNoteStyle();
            Children = new List<OutlineElement>();
        }

        public OutlineElement(OutlineElementType type, string content, int level, ConverterOneNoteStyle style)
        {
            Type = type;
            Content = content;
            Level = level;
            Style = style ?? new ConverterOneNoteStyle();
            Children = new List<OutlineElement>();
        }
    }
}
