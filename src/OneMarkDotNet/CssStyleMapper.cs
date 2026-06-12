using System;
using System.Collections.Generic;

namespace OneMarkDotNet
{
    public static class CssStyleMapper
    {
        public static OneNoteStyle MapCssToOneNoteStyle(Dictionary<string, string> variables)
        {
            OneNoteStyle style = new OneNoteStyle();

            if (variables == null || variables.Count == 0)
            {
                return style;
            }

            if (variables.TryGetValue("--font-family", out string fontFamily))
            {
                style.FontFamily = fontFamily;
            }

            if (variables.TryGetValue("--bg-color", out string bgColor))
            {
                style.BackgroundColor = bgColor;
            }

            if (variables.TryGetValue("--line-height", out string lineHeight))
            {
                if (double.TryParse(lineHeight, out double lh))
                {
                    style.LineHeight = lh;
                }
            }

            if (variables.TryGetValue("--paragraph-margin", out string paragraphMargin))
            {
                string marginValue = paragraphMargin;
                if (marginValue.EndsWith("px"))
                {
                    marginValue = marginValue.Substring(0, marginValue.Length - 2).Trim();
                }
                if (double.TryParse(marginValue, out double pm))
                {
                    style.ParagraphMargin = pm;
                }
            }

            if (variables.TryGetValue("--monospace", out string monospace))
            {
                style.MonospaceFont = monospace;
            }

            if (variables.TryGetValue("--select-text-bg-color", out string codeBgColor))
            {
                style.CodeBackgroundColor = codeBgColor;
            }

            if (variables.TryGetValue("--select-text-font-color", out string textColor))
            {
                style.TextColor = textColor;
            }

            if (variables.TryGetValue("--blockquote-heading-icons", out string blockquoteIcons))
            {
                if (bool.TryParse(blockquoteIcons, out bool bi))
                {
                    style.BlockquoteHeadingIcons = bi;
                }
            }

            if (variables.TryGetValue("--enable-heading-in-blockquote", out string enableHeading))
            {
                if (bool.TryParse(enableHeading, out bool eh))
                {
                    style.EnableHeadingInBlockquote = eh;
                }
            }

            if (variables.TryGetValue("--enable-code-line-number", out string enableLineNum))
            {
                if (bool.TryParse(enableLineNum, out bool eln))
                {
                    style.EnableCodeLineNumber = eln;
                }
            }

            if (variables.TryGetValue("--enable-latex-to-image", out string enableLatex))
            {
                if (bool.TryParse(enableLatex, out bool el))
                {
                    style.EnableLatexToImage = el;
                }
            }

            if (variables.TryGetValue("--block-width-margin", out string blockBorder))
            {
                style.BlockBorderColor = blockBorder;
            }

            return style;
        }
    }
}
