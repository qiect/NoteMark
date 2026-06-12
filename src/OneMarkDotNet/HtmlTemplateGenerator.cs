namespace OneMarkDotNet
{
    using System;
    using System.Text;

    public static class HtmlTemplateGenerator
    {
        private const string KaTeXJsCdn = "https://cdn.jsdelivr.net/npm/katex@0.16.9/dist/katex.min.js";
        private const string KaTeXCssCdn = "https://cdn.jsdelivr.net/npm/katex@0.16.9/dist/katex.min.css";
        private const string MermaidJsCdn = "https://cdn.jsdelivr.net/npm/mermaid@10.6.1/dist/mermaid.min.js";

        public static string GenerateKaTeXTemplate(string formula)
        {
            if (formula == null)
            {
                formula = string.Empty;
            }

            var escapedFormula = EscapeForJavaScript(formula);

            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset=\"UTF-8\">");
            html.AppendLine("<link rel=\"stylesheet\" href=\"" + KaTeXCssCdn + "\">");
            html.AppendLine("<script src=\"" + KaTeXJsCdn + "\"></script>");
            html.AppendLine("<style>");
            html.AppendLine("body {");
            html.AppendLine("  margin: 0;");
            html.AppendLine("  padding: 16px;");
            html.AppendLine("  background: #ffffff;");
            html.AppendLine("  display: inline-block;");
            html.AppendLine("}");
            html.AppendLine(".katex-display {");
            html.AppendLine("  margin: 0;");
            html.AppendLine("}");
            html.AppendLine(".error-output {");
            html.AppendLine("  color: #cc0000;");
            html.AppendLine("  font-family: Consolas, monospace;");
            html.AppendLine("  font-size: 14px;");
            html.AppendLine("  white-space: pre-wrap;");
            html.AppendLine("}");
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            html.AppendLine("<div id=\"formula-output\"></div>");
            html.AppendLine("<script>");
            html.AppendLine("try {");
            html.AppendLine("  katex.render(\"" + escapedFormula + "\", document.getElementById(\"formula-output\"), {");
            html.AppendLine("    displayMode: true,");
            html.AppendLine("    throwOnError: true");
            html.AppendLine("  });");
            html.AppendLine("  window.chrome.webview.postMessage(\"render-complete\");");
            html.AppendLine("} catch (e) {");
            html.AppendLine("  document.getElementById(\"formula-output\").className = \"error-output\";");
            html.AppendLine("  document.getElementById(\"formula-output\").textContent = e.message;");
            html.AppendLine("  window.chrome.webview.postMessage(\"render-error:\" + e.message);");
            html.AppendLine("}");
            html.AppendLine("</script>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");

            return html.ToString();
        }

        public static string GenerateMermaidTemplate(string code)
        {
            if (code == null)
            {
                code = string.Empty;
            }

            var escapedCode = EscapeForJavaScript(code);

            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html>");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset=\"UTF-8\">");
            html.AppendLine("<script src=\"" + MermaidJsCdn + "\"></script>");
            html.AppendLine("<style>");
            html.AppendLine("body {");
            html.AppendLine("  margin: 0;");
            html.AppendLine("  padding: 16px;");
            html.AppendLine("  background: #ffffff;");
            html.AppendLine("  display: inline-block;");
            html.AppendLine("}");
            html.AppendLine("#diagram-output svg {");
            html.AppendLine("  max-width: 100%;");
            html.AppendLine("}");
            html.AppendLine(".error-output {");
            html.AppendLine("  color: #cc0000;");
            html.AppendLine("  font-family: Consolas, monospace;");
            html.AppendLine("  font-size: 14px;");
            html.AppendLine("  white-space: pre-wrap;");
            html.AppendLine("}");
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");
            html.AppendLine("<div id=\"diagram-output\"></div>");
            html.AppendLine("<script>");
            html.AppendLine("mermaid.initialize({ startOnLoad: false, theme: 'default' });");
            html.AppendLine("try {");
            html.AppendLine("  mermaid.render('mermaid-svg', `" + escapedCode + "`).then(function(result) {");
            html.AppendLine("    document.getElementById(\"diagram-output\").innerHTML = result.svg;");
            html.AppendLine("    window.chrome.webview.postMessage(\"render-complete\");");
            html.AppendLine("  }).catch(function(err) {");
            html.AppendLine("    document.getElementById(\"diagram-output\").className = \"error-output\";");
            html.AppendLine("    document.getElementById(\"diagram-output\").textContent = err.message || String(err);");
            html.AppendLine("    window.chrome.webview.postMessage(\"render-error:\" + (err.message || String(err)));");
            html.AppendLine("  });");
            html.AppendLine("} catch (e) {");
            html.AppendLine("  document.getElementById(\"diagram-output\").className = \"error-output\";");
            html.AppendLine("  document.getElementById(\"diagram-output\").textContent = e.message;");
            html.AppendLine("  window.chrome.webview.postMessage(\"render-error:\" + e.message);");
            html.AppendLine("}");
            html.AppendLine("</script>");
            html.AppendLine("</body>");
            html.AppendLine("</html>");

            return html.ToString();
        }

        private static string EscapeForJavaScript(string input)
        {
            if (string.IsNullOrEmpty(input))
            {
                return string.Empty;
            }

            var sb = new StringBuilder(input.Length);
            for (int i = 0; i < input.Length; i++)
            {
                char c = input[i];
                switch (c)
                {
                    case '\\':
                        sb.Append("\\\\");
                        break;
                    case '\"':
                        sb.Append("\\\"");
                        break;
                    case '\'':
                        sb.Append("\\\'");
                        break;
                    case '\n':
                        sb.Append("\\n");
                        break;
                    case '\r':
                        sb.Append("\\r");
                        break;
                    case '\t':
                        sb.Append("\\t");
                        break;
                    case '`':
                        sb.Append("\\`");
                        break;
                    case '$':
                        sb.Append("\\$");
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }

            return sb.ToString();
        }
    }
}
