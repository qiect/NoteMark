using System.Text;

namespace OneMarkDotNet.RenderingServices;

public static class HtmlTemplateGenerator
{
    public static string GenerateMermaidHtml(string mermaidContent)
    {
        var escaped = EscapeForHtml(mermaidContent);
        return $"""
            <!DOCTYPE html>
            <html>
            <head>
            <meta charset="utf-8">
            <style>
              body {{ margin: 0; padding: 20px; background: white; }}
              #container {{ display: flex; justify-content: center; }}
            </style>
            <script src="https://cdn.jsdelivr.net/npm/mermaid@10/dist/mermaid.min.js"></script>
            </head>
            <body>
            <div id="container">
              <pre class="mermaid">{escaped}</pre>
            </div>
            <script>
              mermaid.initialize({{ startOnLoad: true, theme: 'default' }});
              mermaid.run().then(() => {{
                window.chrome.webview.postMessage(JSON.stringify({{ type: 'renderComplete' }}));
              }});
            </script>
            </body>
            </html>
            """;
    }

    public static string GenerateFlowchartHtml(string flowchartContent)
    {
        var escaped = EscapeForHtml(flowchartContent);
        return $"""
            <!DOCTYPE html>
            <html>
            <head>
            <meta charset="utf-8">
            <style>
              body {{ margin: 0; padding: 20px; background: white; }}
              #container {{ display: flex; justify-content: center; }}
            </style>
            <script src="https://cdn.jsdelivr.net/npm/raphael@2/raphael.min.js"></script>
            <script src="https://cdn.jsdelivr.net/npm/flowchart@1/release/flowchart.min.js"></script>
            </head>
            <body>
            <div id="container">
              <div id="diagram"></div>
            </div>
            <script>
              var diagram = flowchart.parse(`{escaped}`);
              diagram.drawSVG('diagram');
              window.chrome.webview.postMessage(JSON.stringify({{ type: 'renderComplete' }}));
            </script>
            </body>
            </html>
            """;
    }

    public static string GenerateSequenceHtml(string sequenceContent)
    {
        var escaped = EscapeForHtml(sequenceContent);
        return $"""
            <!DOCTYPE html>
            <html>
            <head>
            <meta charset="utf-8">
            <style>
              body {{ margin: 0; padding: 20px; background: white; }}
              #container {{ display: flex; justify-content: center; }}
            </style>
            <script src="https://cdn.jsdelivr.net/npm/raphael@2/raphael.min.js"></script>
            <script src="https://cdn.jsdelivr.net/npm/js-sequence-diagrams@2/dist/sequence-diagram-min.js"></script>
            </head>
            <body>
            <div id="container">
              <div id="diagram"></div>
            </div>
            <script>
              var diagram = Diagram.parse(`{escaped}`);
              diagram.drawSVG('diagram', {{ theme: 'simple' }});
              window.chrome.webview.postMessage(JSON.stringify({{ type: 'renderComplete' }}));
            </script>
            </body>
            </html>
            """;
    }

    public static string GenerateMindmapHtml(string mindmapContent)
    {
        var escaped = EscapeForHtml(mindmapContent);
        return $"""
            <!DOCTYPE html>
            <html>
            <head>
            <meta charset="utf-8">
            <style>
              body {{ margin: 0; padding: 20px; background: white; }}
              #container {{ display: flex; justify-content: center; }}
              .markmap {{ width: 100%; height: 600px; }}
            </style>
            <script src="https://cdn.jsdelivr.net/npm/d3@7"></script>
            <script src="https://cdn.jsdelivr.net/npm/markmap-view"></script>
            <script src="https://cdn.jsdelivr.net/npm/markmap-lib"></script>
            </head>
            <body>
            <div id="container">
              <svg id="markmap" class="markmap"></svg>
            </div>
            <script>
              const {{ Markmap }} = window.markmap;
              const content = `{escaped}`;
              const {{ root }} = markmap.transform(markmap.transformMarkdown(content));
              Markmap.create('svg#markmap', null, root);
              setTimeout(() => {{
                window.chrome.webview.postMessage(JSON.stringify({{ type: 'renderComplete' }}));
              }}, 500);
            </script>
            </body>
            </html>
            """;
    }

    public static string GenerateLatexHtml(string formula, bool isInline)
    {
        var escaped = EscapeForHtml(formula);
        var displayMode = isInline ? "false" : "true";
        return $"""
            <!DOCTYPE html>
            <html>
            <head>
            <meta charset="utf-8">
            <style>
              body {{ margin: 0; padding: 20px; background: white; }}
              #container {{ display: flex; justify-content: center; }}
              .katex-display {{ margin: 0; }}
            </style>
            <link rel="stylesheet" href="https://cdn.jsdelivr.net/npm/katex@0.16/dist/katex.min.css">
            <script src="https://cdn.jsdelivr.net/npm/katex@0.16/dist/katex.min.js"></script>
            </head>
            <body>
            <div id="container">
              <span id="formula"></span>
            </div>
            <script>
              try {{
                katex.render(`{escaped}`, document.getElementById('formula'), {{
                  displayMode: {displayMode},
                  throwOnError: true
                }});
                window.chrome.webview.postMessage(JSON.stringify({{
                  type: 'renderComplete',
                  html: document.getElementById('formula').innerHTML
                }}));
              }} catch (e) {{
                window.chrome.webview.postMessage(JSON.stringify({{
                  type: 'renderError',
                  error: e.message
                }}));
              }}
            </script>
            </body>
            </html>
            """;
    }

    public static string GenerateCodeHighlightHtml(string code, string language, bool showLineNumbers)
    {
        var escaped = EscapeForHtml(code);
        var lineNumberStyles = showLineNumbers ? "padding-left: 3.8em;" : "";
        var lineNumberScript = showLineNumbers ? GenerateLineNumberScript() : "";

        return $"""
            <!DOCTYPE html>
            <html>
            <head>
            <meta charset="utf-8">
            <style>
              body {{ margin: 0; padding: 0; background: white; }}
              pre {{ margin: 0; padding: 1em; {lineNumberStyles} }}
              code {{ font-family: 'Consolas', 'Monaco', monospace; font-size: 14px; }}
              .hljs {{ background: transparent; padding: 0; }}
            </style>
            <link rel="stylesheet" href="https://cdn.jsdelivr.net/gh/highlightjs/cdn-release@11/build/styles/vs.min.css">
            <script src="https://cdn.jsdelivr.net/gh/highlightjs/cdn-release@11/build/highlight.min.js"></script>
            </head>
            <body>
            <pre><code id="code" class="language-{language}">{escaped}</code></pre>
            <script>
              hljs.highlightElement(document.getElementById('code'));
              {lineNumberScript}
              window.chrome.webview.postMessage(JSON.stringify({{
                type: 'renderComplete',
                html: document.querySelector('pre').innerHTML
              }}));
            </script>
            </body>
            </html>
            """;
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
        return content
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&#39;");
    }
}
