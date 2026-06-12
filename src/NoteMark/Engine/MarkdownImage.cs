namespace NoteMark
{
    public sealed class MarkdownImage
    {
        public byte[] Data { get; set; }
        public string Format { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }

        public MarkdownImage()
        {
            Data = new byte[0];
            Format = "png";
        }
    }
}
