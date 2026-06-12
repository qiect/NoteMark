namespace OneMarkDotNet.Elements
{
    using System.Collections.Generic;

    public sealed class TableElement : BlockElement
    {
        public List<string[]> Rows { get; set; }
        public int HeaderRowCount { get; set; }
        public int ColumnCount { get; set; }

        public TableElement()
        {
            ElementType = "Table";
            Rows = new List<string[]>();
            HeaderRowCount = 0;
            ColumnCount = 0;
        }

        public TableElement(List<string[]> rows, int headerRowCount, int columnCount)
        {
            ElementType = "Table";
            Rows = rows;
            HeaderRowCount = headerRowCount;
            ColumnCount = columnCount;
        }
    }
}
