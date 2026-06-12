namespace NoteMark.Elements
{
    public sealed class HeadingElement : BlockElement
    {
        public int Level { get; set; }

        public HeadingElement()
        {
            ElementType = "Heading";
            Level = 1;
        }

        public HeadingElement(int level)
        {
            ElementType = "Heading";
            Level = level;
        }
    }
}
