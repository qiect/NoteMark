namespace OneMarkDotNet.Elements
{
    public enum DiagramType
    {
        Mermaid,
        Flow,
        Sequence,
        Mindmap
    }

    public sealed class DiagramBlockElement : BlockElement
    {
        public DiagramType DiagramType { get; set; }
        public string Code { get; set; }

        public DiagramBlockElement()
        {
            ElementType = "DiagramBlock";
            DiagramType = DiagramType.Mermaid;
            Code = string.Empty;
        }

        public DiagramBlockElement(DiagramType diagramType, string code)
        {
            ElementType = "DiagramBlock";
            DiagramType = diagramType;
            Code = code;
        }

        public static DiagramType ParseDiagramType(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return DiagramType.Mermaid;
            }

            switch (value.ToLowerInvariant())
            {
                case "mermaid":
                    return DiagramType.Mermaid;
                case "flow":
                case "flowchart":
                    return DiagramType.Flow;
                case "sequence":
                    return DiagramType.Sequence;
                case "mindmap":
                    return DiagramType.Mindmap;
                default:
                    return DiagramType.Mermaid;
            }
        }
    }
}
