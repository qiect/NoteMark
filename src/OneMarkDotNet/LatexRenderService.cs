namespace OneMarkDotNet
{
    using System;
    using System.Diagnostics;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    public sealed class LatexRenderService : IDisposable
    {
        private WebView2Helper webViewHelper;
        private bool disposed;
        private readonly object syncLock = new object();

        public async Task<byte[]> RenderToImageAsync(string formula)
        {
            ThrowIfDisposed();

            var stopwatch = Stopwatch.StartNew();
            byte[] imageData = null;
            string errorMessage = null;

            try
            {
                var helper = GetOrCreateWebView2Helper();
                var renderCompleted = new TaskCompletionSource<bool>();

                Action<string> messageHandler = null;
                messageHandler = (message) =>
                {
                    if (message == "render-complete")
                    {
                        renderCompleted.SetResult(true);
                    }
                    else if (message.StartsWith("render-error:"))
                    {
                        renderCompleted.SetException(new InvalidOperationException(
                            "LaTeX rendering failed: " + message.Substring("render-error:".Length)));
                    }
                };

                helper.MessageReceived += messageHandler;

                try
                {
                    var html = HtmlTemplateGenerator.GenerateKaTeXTemplate(formula);
                    await helper.NavigateToStringAsync(html);

                    var delayTask = Task.Delay(30000);
                    var completedTask = await Task.WhenAny(renderCompleted.Task, delayTask);

                    if (completedTask == delayTask)
                    {
                        throw new TimeoutException("LaTeX rendering timed out after 30 seconds.");
                    }

                    await renderCompleted.Task;

                    await Task.Delay(200);

                    imageData = await helper.CaptureScreenshotAsync();
                }
                finally
                {
                    helper.MessageReceived -= messageHandler;
                }
            }
            catch (Exception ex)
            {
                errorMessage = ex.Message;
            }

            stopwatch.Stop();

            if (imageData != null)
            {
                return imageData;
            }

            throw new InvalidOperationException(
                "Failed to render LaTeX formula to image" +
                (string.IsNullOrEmpty(errorMessage) ? "." : ": " + errorMessage));
        }

        public async Task<string> RenderToTextAsync(string formula)
        {
            ThrowIfDisposed();

            if (string.IsNullOrEmpty(formula))
            {
                return string.Empty;
            }

            return await Task.Run(() => ConvertLatexToUnicode(formula));
        }

        private WebView2Helper GetOrCreateWebView2Helper()
        {
            lock (syncLock)
            {
                if (webViewHelper == null)
                {
                    webViewHelper = new WebView2Helper();
                    webViewHelper.InitializeAsync().GetAwaiter().GetResult();
                }

                return webViewHelper;
            }
        }

        private static string ConvertLatexToUnicode(string formula)
        {
            var result = formula;

            var replacements = new System.Collections.Generic.Dictionary<string, string>
            {
                { @"\alpha", "\u03B1" },
                { @"\beta", "\u03B2" },
                { @"\gamma", "\u03B3" },
                { @"\delta", "\u03B4" },
                { @"\epsilon", "\u03B5" },
                { @"\varepsilon", "\u03B5" },
                { @"\zeta", "\u03B6" },
                { @"\eta", "\u03B7" },
                { @"\theta", "\u03B8" },
                { @"\vartheta", "\u03D1" },
                { @"\iota", "\u03B9" },
                { @"\kappa", "\u03BA" },
                { @"\lambda", "\u03BB" },
                { @"\mu", "\u03BC" },
                { @"\nu", "\u03BD" },
                { @"\xi", "\u03BE" },
                { @"\pi", "\u03C0" },
                { @"\rho", "\u03C1" },
                { @"\sigma", "\u03C3" },
                { @"\tau", "\u03C4" },
                { @"\upsilon", "\u03C5" },
                { @"\phi", "\u03C6" },
                { @"\varphi", "\u03D5" },
                { @"\chi", "\u03C7" },
                { @"\psi", "\u03C8" },
                { @"\omega", "\u03C9" },
                { @"\Gamma", "\u0393" },
                { @"\Delta", "\u0394" },
                { @"\Theta", "\u0398" },
                { @"\Lambda", "\u039B" },
                { @"\Xi", "\u039E" },
                { @"\Pi", "\u03A0" },
                { @"\Sigma", "\u03A3" },
                { @"\Phi", "\u03A6" },
                { @"\Psi", "\u03A8" },
                { @"\Omega", "\u03A9" },
                { @"\infty", "\u221E" },
                { @"\pm", "\u00B1" },
                { @"\mp", "\u2213" },
                { @"\times", "\u00D7" },
                { @"\div", "\u00F7" },
                { @"\cdot", "\u22C5" },
                { @"\leq", "\u2264" },
                { @"\geq", "\u2265" },
                { @"\neq", "\u2260" },
                { @"\approx", "\u2248" },
                { @"\equiv", "\u2261" },
                { @"\sim", "\u223C" },
                { @"\propto", "\u221D" },
                { @"\ll", "\u226A" },
                { @"\gg", "\u226B" },
                { @"\subset", "\u2282" },
                { @"\supset", "\u2283" },
                { @"\subseteq", "\u2286" },
                { @"\supseteq", "\u2287" },
                { @"\in", "\u2208" },
                { @"\notin", "\u2209" },
                { @"\cup", "\u222A" },
                { @"\cap", "\u2229" },
                { @"\emptyset", "\u2205" },
                { @"\forall", "\u2200" },
                { @"\exists", "\u2203" },
                { @"\neg", "\u00AC" },
                { @"\wedge", "\u2227" },
                { @"\vee", "\u2228" },
                { @"\Rightarrow", "\u21D2" },
                { @"\Leftarrow", "\u21D0" },
                { @"\Leftrightarrow", "\u21D4" },
                { @"\rightarrow", "\u2192" },
                { @"\leftarrow", "\u2190" },
                { @"\leftrightarrow", "\u2194" },
                { @"\partial", "\u2202" },
                { @"\nabla", "\u2207" },
                { @"\int", "\u222B" },
                { @"\iint", "\u222C" },
                { @"\iiint", "\u222D" },
                { @"\oint", "\u222E" },
                { @"\sum", "\u2211" },
                { @"\prod", "\u220F" },
                { @"\sqrt", "\u221A" },
                { @"\hbar", "\u210F" },
                { @"\ell", "\u2113" },
                { @"\Re", "\u211C" },
                { @"\Im", "\u2111" },
                { @"\angle", "\u2220" },
                { @"\perp", "\u22A5" },
                { @"\parallel", "\u2225" },
                { @"\circ", "\u2218" },
                { @"\bullet", "\u2219" },
                { @"\oplus", "\u2295" },
                { @"\otimes", "\u2297" },
                { @"\odot", "\u2299" },
                { @"\dagger", "\u2020" },
                { @"\ddagger", "\u2021" },
                { @"\degree", "\u00B0" },
                { @"\%", "%" },
                { @"\#", "#" },
                { @"\&", "&" },
                { @"\$", "$" },
                { @"\_", "_" },
                { @"\{", "{" },
                { @"\}", "}" },
                { @"\ ", " " },
                { @"\\", "\n" }
            };

            foreach (var kvp in replacements)
            {
                result = result.Replace(kvp.Key, kvp.Value);
            }

            result = Regex.Replace(result, @"\^{([^}]+)}", "$1");
            result = Regex.Replace(result, @"_{([^}]+)}", "$1");
            result = Regex.Replace(result, @"\^{(\w)}", "$1");
            result = Regex.Replace(result, @"_{(\w)}", "$1");
            result = Regex.Replace(result, @"\\frac\{([^}]*)}\{([^}]*)}", "($1)/($2)");
            result = Regex.Replace(result, @"\\dfrac\{([^}]*)}\{([^}]*)}", "($1)/($2)");
            result = Regex.Replace(result, @"\\sqrt\{([^}]*)}", "\u221A($1)");
            result = Regex.Replace(result, @"\\text\{([^}]*)}", "$1");
            result = Regex.Replace(result, @"\\mathrm\{([^}]*)}", "$1");
            result = Regex.Replace(result, @"\\mathbf\{([^}]*)}", "$1");
            result = Regex.Replace(result, @"\\mathit\{([^}]*)}", "$1");
            result = Regex.Replace(result, @"\\left[([(.|])", "$1");
            result = Regex.Replace(result, @"\\right[([).|])", "$1");
            result = Regex.Replace(result, @"\\[a-zA-Z]+", "");

            return result.Trim();
        }

        private void ThrowIfDisposed()
        {
            if (disposed)
            {
                throw new ObjectDisposedException(nameof(LatexRenderService));
            }
        }

        public void Dispose()
        {
            if (disposed)
            {
                return;
            }

            disposed = true;

            lock (syncLock)
            {
                if (webViewHelper != null)
                {
                    webViewHelper.Dispose();
                    webViewHelper = null;
                }
            }
        }
    }
}
