using System;

namespace OneMarkDotNet
{
    public class BorderStyle
    {
        public string Color { get; set; }
        public double Width { get; set; }
        public string Style { get; set; }

        public BorderStyle()
        {
            Color = "#cccccc";
            Width = 1.0;
            Style = "solid";
        }
    }
}
