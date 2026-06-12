using System;
using System.Collections.Generic;

namespace NoteMark
{
    public class Theme
    {
        public string Name { get; set; }
        public Dictionary<string, string> Variables { get; set; }
        public OneNoteStyle Style { get; set; }

        public Theme(string name, Dictionary<string, string> variables)
        {
            Name = name ?? throw new ArgumentNullException(nameof(name));
            Variables = variables ?? new Dictionary<string, string>();
            Style = CssStyleMapper.MapCssToOneNoteStyle(Variables);
        }
    }
}
