using System.Globalization;
using System.Text;

namespace OneMarkDotNet.RenderingServices;

public static class HtmlTemplateGenerator
{
    public static string GenerateMermaidHtml(string mermaidContent)
    {
        var htmlEscaped = EscapeForHtml(mermaidContent);
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html>");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"utf-8\">");
        sb.AppendLine("<style>");
        sb.AppendLine("  body { margin: 0; padding: 20px; background: white; }");
        sb.AppendLine("  #container { display: flex; justify-content: center; }");
        sb.AppendLine("</style>");
        sb.AppendLine("<script src=\"https://cdn.jsdelivr.net/npm/mermaid@10/dist/mermaid.min.js\"></script>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("<div id=\"container\">");
        sb.Append("  <pre class=\"mermaid\">").Append(htmlEscaped).AppendLine("</pre>");
        sb.AppendLine("</div>");
        sb.AppendLine("<script>");
        sb.AppendLine("  mermaid.initialize({ startOnLoad: true, theme: 'default' });");
        sb.AppendLine("  mermaid.run().then(function() {");
        sb.AppendLine("    window.chrome.webview.postMessage(JSON.stringify({ type: 'renderComplete' }));");
        sb.AppendLine("  });");
        sb.AppendLine("</script>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }

    public static string GenerateFlowchartHtml(string flowchartContent)
    {
        var jsEscaped = EscapeForJavaScript(flowchartContent);
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html>");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"utf-8\">");
        sb.AppendLine("<style>");
        sb.AppendLine("  body { margin: 0; padding: 20px; background: white; }");
        sb.AppendLine("  #container { display: flex; justify-content: center; }");
        sb.AppendLine("</style>");
        sb.AppendLine("<script src=\"https://cdn.jsdelivr.net/npm/raphael@2/raphael.min.js\"></script>");
        sb.AppendLine("<script src=\"https://cdn.jsdelivr.net/npm/flowchart@1/release/flowchart.min.js\"></script>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("<div id=\"container\">");
        sb.AppendLine("  <div id=\"diagram\"></div>");
        sb.AppendLine("</div>");
        sb.AppendLine("<script>");
        sb.Append("  var diagram = flowchart.parse('").Append(jsEscaped).AppendLine("');");
        sb.AppendLine("  diagram.drawSVG('diagram');");
        sb.AppendLine("  window.chrome.webview.postMessage(JSON.stringify({ type: 'renderComplete' }));");
        sb.AppendLine("</script>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }

    public static string GenerateSequenceHtml(string sequenceContent)
    {
        var jsEscaped = EscapeForJavaScript(sequenceContent);
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html>");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"utf-8\">");
        sb.AppendLine("<style>");
        sb.AppendLine("  body { margin: 0; padding: 20px; background: white; }");
        sb.AppendLine("  #container { display: flex; justify-content: center; }");
        sb.AppendLine("</style>");
        sb.AppendLine("<script src=\"https://cdn.jsdelivr.net/npm/raphael@2/raphael.min.js\"></script>");
        sb.AppendLine("<script src=\"https://cdn.jsdelivr.net/npm/js-sequence-diagrams@2/dist/sequence-diagram-min.js\"></script>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("<div id=\"container\">");
        sb.AppendLine("  <div id=\"diagram\"></div>");
        sb.AppendLine("</div>");
        sb.AppendLine("<script>");
        sb.Append("  var diagram = Diagram.parse('").Append(jsEscaped).AppendLine("');");
        sb.AppendLine("  diagram.drawSVG('diagram', { theme: 'simple' });");
        sb.AppendLine("  window.chrome.webview.postMessage(JSON.stringify({ type: 'renderComplete' }));");
        sb.AppendLine("</script>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }

    public static string GenerateMindmapHtml(string mindmapContent)
    {
        var jsEscaped = EscapeForJavaScript(mindmapContent);
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html>");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"utf-8\">");
        sb.AppendLine("<style>");
        sb.AppendLine("  body { margin: 0; padding: 20px; background: white; }");
        sb.AppendLine("  #container { display: flex; justify-content: center; }");
        sb.AppendLine("  .markmap { width: 100%; height: 600px; }");
        sb.AppendLine("</style>");
        sb.AppendLine("<script src=\"https://cdn.jsdelivr.net/npm/d3@7\"></script>");
        sb.AppendLine("<script src=\"https://cdn.jsdelivr.net/npm/markmap-view\"></script>");
        sb.AppendLine("<script src=\"https://cdn.jsdelivr.net/npm/markmap-lib\"></script>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("<div id=\"container\">");
        sb.AppendLine("  <svg id=\"markmap\" class=\"markmap\"></svg>");
        sb.AppendLine("</div>");
        sb.AppendLine("<script>");
        sb.AppendLine("  var MarkmapClass = window.markmap.Markmap;");
        sb.Append("  var content = '").Append(jsEscaped).AppendLine("';");
        sb.AppendLine("  var root = markmap.transform(markmap.transformMarkdown(content));");
        sb.AppendLine("  MarkmapClass.create('svg#markmap', null, root);");
        sb.AppendLine("  setTimeout(function() {");
        sb.AppendLine("    window.chrome.webview.postMessage(JSON.stringify({ type: 'renderComplete' }));");
        sb.AppendLine("  }, 500);");
        sb.AppendLine("</script>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }

    public static string GenerateLatexHtml(string formula, bool isInline)
    {
        var jsEscaped = EscapeForJavaScript(formula);
        var displayMode = isInline ? "false" : "true";
        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html>");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"utf-8\">");
        sb.AppendLine("<style>");
        sb.AppendLine("  body { margin: 0; padding: 20px; background: white; }");
        sb.AppendLine("  #container { display: flex; justify-content: center; }");
        sb.AppendLine("  .katex-display { margin: 0; }");
        sb.AppendLine("</style>");
        sb.AppendLine("<link rel=\"stylesheet\" href=\"https://cdn.jsdelivr.net/npm/katex@0.16/dist/katex.min.css\">");
        sb.AppendLine("<script src=\"https://cdn.jsdelivr.net/npm/katex@0.16/dist/katex.min.js\"></script>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.AppendLine("<div id=\"container\">");
        sb.AppendLine("  <span id=\"formula\"></span>");
        sb.AppendLine("</div>");
        sb.AppendLine("<script>");
        sb.AppendLine("  try {");
        sb.Append("    katex.render('").Append(jsEscaped).Append("', document.getElementById('formula'), { displayMode: ").Append(displayMode).AppendLine(", throwOnError: true });");
        sb.AppendLine("    window.chrome.webview.postMessage(JSON.stringify({ type: 'renderComplete', html: document.getElementById('formula').innerHTML }));");
        sb.AppendLine("  } catch (e) {");
        sb.AppendLine("    window.chrome.webview.postMessage(JSON.stringify({ type: 'renderError', error: e.message }));");
        sb.AppendLine("  }");
        sb.AppendLine("</script>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }

    public static string GenerateCodeHighlightHtml(string code, string language, bool showLineNumbers)
    {
        var htmlEscaped = EscapeForHtml(code);
        var lineNumberStyles = showLineNumbers ? "padding-left: 3.8em;" : "";
        var lineNumberScript = showLineNumbers ? GenerateLineNumberScript() : "";

        var sb = new StringBuilder();
        sb.AppendLine("<!DOCTYPE html>");
        sb.AppendLine("<html>");
        sb.AppendLine("<head>");
        sb.AppendLine("<meta charset=\"utf-8\">");
        sb.AppendLine("<style>");
        sb.AppendLine("  body { margin: 0; padding: 0; background: white; }");
        sb.Append("  pre { margin: 0; padding: 1em; ").Append(lineNumberStyles).AppendLine(" }");
        sb.AppendLine("  code { font-family: 'Consolas', 'Monaco', monospace; font-size: 14px; }");
        sb.AppendLine("  .hljs { background: transparent; padding: 0; }");
        sb.AppendLine("</style>");
        sb.AppendLine("<link rel=\"stylesheet\" href=\"https://cdn.jsdelivr.net/gh/highlightjs/cdn-release@11/build/styles/vs.min.css\">");
        sb.AppendLine("<script src=\"https://cdn.jsdelivr.net/gh/highlightjs/cdn-release@11/build/highlight.min.js\"></script>");
        sb.AppendLine("</head>");
        sb.AppendLine("<body>");
        sb.Append("<pre><code id=\"code\" class=\"language-").Append(language).Append("\">").Append(htmlEscaped).AppendLine("</code></pre>");
        sb.AppendLine("<script>");
        sb.AppendLine("  hljs.highlightElement(document.getElementById('code'));");
        if (showLineNumbers)
            sb.Append(lineNumberScript).AppendLine();
        sb.AppendLine("  window.chrome.webview.postMessage(JSON.stringify({ type: 'renderComplete', html: document.querySelector('pre').innerHTML }));");
        sb.AppendLine("</script>");
        sb.AppendLine("</body>");
        sb.AppendLine("</html>");
        return sb.ToString();
    }

    private static string GenerateLineNumberScript()
    {
        return """
            (function() {
              var code = document.getElementById('code');
              var lines = code.innerHTML.split('\n');
              var numbered = lines.map(function(line, i) {
                return '<span class="line-number" style="display:inline-block;width:2.5em;margin-left:-2.5em;padding-right:0.5em;text-align:right;color:#999;user-select:none;">' + (i + 1) + '</span>' + line;
              });
              code.innerHTML = numbered.join('\n');
            })();
            """;
    }

    private static string EscapeForHtml(string content)
    {
        var sb = new StringBuilder(content.Length);
        foreach (var c in content)
        {
            switch (c)
            {
                case '&': sb.Append("&amp;"); break;
                case '<': sb.Append("&lt;"); break;
                case '>': sb.Append("&gt;"); break;
                case '"': sb.Append("&quot;"); break;
                case '\'': sb.Append("&#39;"); break;
                default: sb.Append(c); break;
            }
        }
        return sb.ToString();
    }

    private static string EscapeForJavaScript(string content)
    {
        var sb = new StringBuilder(content.Length);
        foreach (var c in content)
        {
            switch (c)
            {
                case '\\': sb.Append("\\\\"); break;
                case '\'': sb.Append("\\'"); break;
                case '"': sb.Append("\\\""); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                case '<': sb.Append("\\x3c"); break;
                case '>': sb.Append("\\x3e"); break;
                case '\u2028': sb.Append("\\u2028"); break;
                case '\u2029': sb.Append("\\u2029"); break;
                default: sb.Append(c); break;
            }
        }
        return sb.ToString();
    }
}
