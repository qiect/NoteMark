using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace OneMarkDotNet.RenderingServices;

public sealed class CodeHighlightService
{
    private static readonly string[] SupportedLanguages =
    [
        "csharp", "java", "python", "javascript", "typescript",
        "cpp", "go", "rust", "sql", "html", "css", "json",
        "xml", "yaml", "markdown", "bash", "powershell"
    ];

    private static readonly Dictionary<string, string> LanguageAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["c#"] = "csharp",
        ["cs"] = "csharp",
        ["js"] = "javascript",
        ["ts"] = "typescript",
        ["c++"] = "cpp",
        ["golang"] = "go",
        ["md"] = "markdown",
        ["sh"] = "bash",
        ["shell"] = "bash",
        ["ps1"] = "powershell",
        ["yml"] = "yaml"
    };

    private readonly Dictionary<string, LanguageRuleSet> _ruleSets;

    public CodeHighlightService()
    {
        _ruleSets = InitializeRuleSets();
    }

    public string HighlightCode(string code, string language, bool showLineNumbers = false, bool forceRehighlight = false)
    {
        var stopwatch = Stopwatch.StartNew();

        var normalizedLang = NormalizeLanguage(language);
        if (normalizedLang is null)
        {
            return WrapInPreCode(EscapeHtml(code), showLineNumbers);
        }

        var highlighted = ApplyHighlighting(code, normalizedLang);
        var result = WrapInPreCode(highlighted, showLineNumbers);

        stopwatch.Stop();
        return result;
    }

    public static IReadOnlyList<string> GetSupportedLanguages() => SupportedLanguages;

    private static string? NormalizeLanguage(string language)
    {
        if (string.IsNullOrWhiteSpace(language))
            return null;

        var lower = language.ToLowerInvariant().Trim();
        if (LanguageAliases.TryGetValue(lower, out var alias))
            return alias;

        return SupportedLanguages.Contains(lower) ? lower : null;
    }

    private string ApplyHighlighting(string code, string language)
    {
        if (!_ruleSets.TryGetValue(language, out var ruleSet))
            return EscapeHtml(code);

        var tokens = Tokenize(code, ruleSet);
        return ReconstructFromTokens(tokens, code);
    }

    private static List<Token> Tokenize(string code, LanguageRuleSet ruleSet)
    {
        var tokens = new List<Token>();
        var occupied = new bool[code.Length];

        foreach (var rule in ruleSet.Rules)
        {
            foreach (Match match in rule.Regex.Matches(code))
            {
                if (match.Index >= code.Length || match.Length == 0)
                    continue;

                var isOverlapping = false;
                for (var i = match.Index; i < match.Index + match.Length && i < code.Length; i++)
                {
                    if (occupied[i])
                    {
                        isOverlapping = true;
                        break;
                    }
                }

                if (isOverlapping)
                    continue;

                for (var i = match.Index; i < match.Index + match.Length && i < code.Length; i++)
                {
                    occupied[i] = true;
                }

                tokens.Add(new Token(match.Index, match.Length, rule.CssClass));
            }
        }

        tokens.Sort((a, b) => a.Index.CompareTo(b.Index));
        return tokens;
    }

    private static string ReconstructFromTokens(List<Token> tokens, string code)
    {
        var sb = new StringBuilder();
        var lastEnd = 0;

        foreach (var token in tokens)
        {
            if (token.Index > lastEnd)
            {
                sb.Append(EscapeHtml(code.Substring(lastEnd, token.Index - lastEnd)));
            }

            var text = code.Substring(token.Index, token.Length);
            sb.Append(string.Format(CultureInfo.InvariantCulture, "<span class=\"{0}\">{1}</span>", token.CssClass, EscapeHtml(text)));
            lastEnd = token.Index + token.Length;
        }

        if (lastEnd < code.Length)
        {
            sb.Append(EscapeHtml(code.Substring(lastEnd)));
        }

        return sb.ToString();
    }

    private static string WrapInPreCode(string highlightedCode, bool showLineNumbers)
    {
        if (!showLineNumbers)
        {
            return string.Format(CultureInfo.InvariantCulture, "<pre><code class=\"hljs\">{0}</code></pre>", highlightedCode);
        }

        var lines = highlightedCode.Split(new[] { '\n' }, StringSplitOptions.None);
        var sb = new StringBuilder();
        sb.Append("<pre><code class=\"hljs\">");

        for (var i = 0; i < lines.Length; i++)
        {
            var lineNumber = i + 1;
            sb.Append(string.Format(CultureInfo.InvariantCulture, "<span class=\"hljs-ln-number\" data-line-number=\"{0}\"></span>", lineNumber));
            sb.Append(string.Format(CultureInfo.InvariantCulture, "<span class=\"hljs-ln-line\">{0}", lines[i]));
            if (i < lines.Length - 1)
                sb.Append('\n');
            sb.Append("</span>");
        }

        sb.Append("</code></pre>");
        return sb.ToString();
    }

    private static string EscapeHtml(string text) =>
        text.Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;");

    private static Dictionary<string, LanguageRuleSet> InitializeRuleSets() => new()
    {
        ["csharp"] = new LanguageRuleSet(
        [
            new HighlightRule(new Regex(@"///.*?$", RegexOptions.Multiline | RegexOptions.Compiled), "hljs-comment"),
            new HighlightRule(new Regex(@"/\*[\s\S]*?\*/", RegexOptions.Compiled), "hljs-comment"),
            new HighlightRule(new Regex(@"//.*?$", RegexOptions.Multiline | RegexOptions.Compiled), "hljs-comment"),
            new HighlightRule(new Regex(@"@?""(?:[^""\\]|\\.)*""", RegexOptions.Compiled), "hljs-string"),
            new HighlightRule(new Regex(@"'(?:[^'\\]|\\.)*'", RegexOptions.Compiled), "hljs-string"),
            new HighlightRule(new Regex(@"\b(?:abstract|as|base|bool|break|byte|case|catch|char|checked|class|const|continue|decimal|default|delegate|do|double|else|enum|event|explicit|extern|false|finally|fixed|float|for|foreach|goto|if|implicit|in|int|interface|internal|is|lock|long|namespace|new|null|object|operator|out|override|params|private|protected|public|readonly|ref|return|sbyte|sealed|short|sizeof|stackalloc|static|string|struct|switch|this|throw|true|try|typeof|uint|ulong|unchecked|unsafe|ushort|using|virtual|void|volatile|while|var|dynamic|async|await|record|init|required|with|nint|nuint|scoped|file|allows)\b", RegexOptions.Compiled), "hljs-keyword"),
            new HighlightRule(new Regex(@"\b(?:true|false|null|this|base)\b", RegexOptions.Compiled), "hljs-literal"),
            new HighlightRule(new Regex(@"\b\d+\.\d+(?:[eE][+-]?\d+)?[fFdDmM]?\b|\b\d+[fFdDmMuluUsU]?\b|\b0x[0-9a-fA-F]+[lLuU]?\b", RegexOptions.Compiled), "hljs-number"),
            new HighlightRule(new Regex(@"\b[A-Z][a-zA-Z0-9]*\b", RegexOptions.Compiled), "hljs-type"),
            new HighlightRule(new Regex(@"#region\b|#endregion\b|#if\b|#else\b|#elif\b|#endif\b|#define\b|#undef\b|#warning\b|#error\b|#line\b|#pragma\b", RegexOptions.Compiled), "hljs-meta"),
            new HighlightRule(new Regex(@"\b(?:Console|Math|String|Convert|DateTime|Task|List|Dictionary|IEnumerable|IQueryable|Action|Func|Attribute|Exception|EventArgs|IDisposable|IAsyncDisposable)\b", RegexOptions.Compiled), "hljs-built_in")
        ]),
        ["java"] = new LanguageRuleSet(
        [
            new HighlightRule(new Regex(@"/\*[\s\S]*?\*/", RegexOptions.Compiled), "hljs-comment"),
            new HighlightRule(new Regex(@"//.*?$", RegexOptions.Multiline | RegexOptions.Compiled), "hljs-comment"),
            new HighlightRule(new Regex(@"""(?:[^""\\]|\\.)*""", RegexOptions.Compiled), "hljs-string"),
            new HighlightRule(new Regex(@"'(?:[^'\\]|\\.)*'", RegexOptions.Compiled), "hljs-string"),
            new HighlightRule(new Regex(@"\b(?:abstract|assert|boolean|break|byte|case|catch|char|class|const|continue|default|do|double|else|enum|extends|final|finally|float|for|goto|if|implements|import|instanceof|int|interface|long|native|new|package|private|protected|public|return|short|static|strictfp|super|switch|synchronized|this|throw|throws|transient|try|void|volatile|while|var|record|sealed|permits|yield)\b", RegexOptions.Compiled), "hljs-keyword"),
            new HighlightRule(new Regex(@"\b(?:true|false|null)\b", RegexOptions.Compiled), "hljs-literal"),
            new HighlightRule(new Regex(@"\b\d+\.\d+(?:[eE][+-]?\d+)?[fFdD]?\b|\b\d+[lLfFdD]?\b|\b0x[0-9a-fA-F]+[lL]?\b", RegexOptions.Compiled), "hljs-number"),
            new HighlightRule(new Regex(@"@[A-Za-z_]\w*", RegexOptions.Compiled), "hljs-meta"),
            new HighlightRule(new Regex(@"\b(?:String|Integer|Long|Double|Float|Boolean|Byte|Short|Character|List|Map|Set|ArrayList|HashMap|HashSet|System|Math|Thread|Runnable|Exception|Override|Deprecated|SuppressWarnings)\b", RegexOptions.Compiled), "hljs-built_in")
        ]),
        ["python"] = new LanguageRuleSet(
        [
            new HighlightRule(new Regex(@"'''[\s\S]*?'''|""""[\s\S]*?""""", RegexOptions.Compiled), "hljs-string"),
            new HighlightRule(new Regex(@"#.*?$", RegexOptions.Multiline | RegexOptions.Compiled), "hljs-comment"),
            new HighlightRule(new Regex(@"f?""""(?:[^""\\]|\\.)*""""|f?'(?:[^'\\]|\\.)*'|f?""(?:[^""\\]|\\.)*""", RegexOptions.Compiled), "hljs-string"),
            new HighlightRule(new Regex(@"\b(?:False|True|None|and|as|assert|async|await|break|class|continue|def|del|elif|else|except|finally|for|from|global|if|import|in|is|lambda|nonlocal|not|or|pass|raise|return|try|while|with|yield|match|case)\b", RegexOptions.Compiled), "hljs-keyword"),
            new HighlightRule(new Regex(@"\b(?:True|False|None)\b", RegexOptions.Compiled), "hljs-literal"),
            new HighlightRule(new Regex(@"\b\d+\.\d+(?:[eE][+-]?\d+)?[jJ]?\b|\b\d+[jJ]?\b|\b0[xXoObB][0-9a-fA-F]+\b", RegexOptions.Compiled), "hljs-number"),
            new HighlightRule(new Regex(@"\b(?:int|float|str|list|dict|set|tuple|bool|bytes|range|type|object|complex|frozenset|bytearray|memoryview|enumerate|zip|map|filter|sorted|reversed|len|print|input|open|super|property|classmethod|staticmethod|Exception|ValueError|TypeError|KeyError|IndexError|AttributeError|RuntimeError|NotImplementedError)\b", RegexOptions.Compiled), "hljs-built_in"),
            new HighlightRule(new Regex(@"@[A-Za-z_]\w*", RegexOptions.Compiled), "hljs-meta")
        ]),
        ["javascript"] = new LanguageRuleSet(
        [
            new HighlightRule(new Regex(@"/\*[\s\S]*?\*/", RegexOptions.Compiled), "hljs-comment"),
            new HighlightRule(new Regex(@"//.*?$", RegexOptions.Multiline | RegexOptions.Compiled), "hljs-comment"),
            new HighlightRule(new Regex(@"`(?:[^`\\]|\\.)*`", RegexOptions.Compiled), "hljs-string"),
            new HighlightRule(new Regex(@"""(?:[^""\\]|\\.)*""|'(?:[^'\\]|\\.)*'", RegexOptions.Compiled), "hljs-string"),
            new HighlightRule(new Regex(@"\b(?:async|await|break|case|catch|class|const|continue|debugger|default|delete|do|else|export|extends|finally|for|from|function|if|import|in|instanceof|let|new|of|return|static|super|switch|this|throw|try|typeof|var|void|while|with|yield)\b", RegexOptions.Compiled), "hljs-keyword"),
            new HighlightRule(new Regex(@"\b(?:true|false|null|undefined|NaN|Infinity)\b", RegexOptions.Compiled), "hljs-literal"),
            new HighlightRule(new Regex(@"\b\d+\.\d+(?:[eE][+-]?\d+)?[n]?\b|\b\d+[n]?\b|\b0[xXoObB][0-9a-fA-F]+[n]?\b", RegexOptions.Compiled), "hljs-number"),
            new HighlightRule(new Regex(@"\b(?:console|document|window|Math|JSON|Promise|Array|Object|String|Number|Boolean|Symbol|Map|Set|WeakMap|WeakSet|Proxy|Reflect|Error|TypeError|RangeError|SyntaxError|RegExp|Date|parseInt|parseFloat|isNaN|isFinite|encodeURI|decodeURI|setTimeout|setInterval|clearTimeout|clearInterval|fetch|require|module|exports|process|Buffer)\b", RegexOptions.Compiled), "hljs-built_in")
        ]),
        ["typescript"] = new LanguageRuleSet(
        [
            new HighlightRule(new Regex(@"/\*[\s\S]*?\*/", RegexOptions.Compiled), "hljs-comment"),
            new HighlightRule(new Regex(@"//.*?$", RegexOptions.Multiline | RegexOptions.Compiled), "hljs-comment"),
            new HighlightRule(new Regex(@"`(?:[^`\\]|\\.)*`", RegexOptions.Compiled), "hljs-string"),
            new HighlightRule(new Regex(@"""(?:[^""\\]|\\.)*""|'(?:[^'\\]|\\.)*'", RegexOptions.Compiled), "hljs-string"),
            new HighlightRule(new Regex(@"\b(?:abstract|any|as|async|await|bigint|boolean|break|case|catch|class|const|constructor|continue|debugger|declare|default|delete|do|else|enum|export|extends|finally|for|from|function|if|implements|import|in|infer|instanceof|interface|is|keyof|let|module|namespace|never|new|null|number|object|of|out|override|package|private|protected|public|readonly|require|return|satisfies|set|static|string|super|switch|symbol|this|throw|try|type|typeof|undefined|unique|unknown|var|void|while|with|yield)\b", RegexOptions.Compiled), "hljs-keyword"),
            new HighlightRule(new Regex(@"\b(?:true|false|null|undefined)\b", RegexOptions.Compiled), "hljs-literal"),
            new HighlightRule(new Regex(@"\b\d+\.\d+(?:[eE][+-]?\d+)?\b|\b\d+\b|\b0[xXoObB][0-9a-fA-F]+\b", RegexOptions.Compiled), "hljs-number"),
            new HighlightRule(new Regex(@"\b(?:console|document|window|Math|JSON|Promise|Array|Record|Partial|Required|Readonly|Pick|Omit|Exclude|Extract|NonNullable|ReturnType|InstanceType|Parameters|ConstructorParameters)\b", RegexOptions.Compiled), "hljs-built_in"),
            new HighlightRule(new Regex(@"@[A-Za-z_]\w*", RegexOptions.Compiled), "hljs-meta")
        ]),
        ["cpp"] = new LanguageRuleSet(
        [
            new HighlightRule(new Regex(@"/\*[\s\S]*?\*/", RegexOptions.Compiled), "hljs-comment"),
            new HighlightRule(new Regex(@"//.*?$", RegexOptions.Multiline | RegexOptions.Compiled), "hljs-comment"),
            new HighlightRule(new Regex(@"""(?:[^""\\]|\\.)*""", RegexOptions.Compiled), "hljs-string"),
            new HighlightRule(new Regex(@"'(?:[^'\\]|\\.)*'", RegexOptions.Compiled), "hljs-string"),
            new HighlightRule(new Regex(@"\b(?:alignas|alignof|and|and_eq|asm|auto|bitand|bitor|bool|break|case|catch|char|char8_t|char16_t|char32_t|class|compl|concept|const|consteval|constexpr|constinit|const_cast|continue|co_await|co_return|co_yield|decltype|default|delete|do|double|dynamic_cast|else|enum|explicit|export|extern|false|float|for|friend|goto|if|inline|int|long|mutable|namespace|new|noexcept|not|not_eq|nullptr|operator|or|or_eq|private|protected|public|register|reinterpret_cast|requires|return|short|signed|sizeof|static|static_assert|static_cast|struct|switch|template|this|thread_local|throw|true|try|typedef|typeid|typename|union|unsigned|using|virtual|void|volatile|wchar_t|while|xor|xor_eq|override|final)\b", RegexOptions.Compiled), "hljs-keyword"),
            new HighlightRule(new Regex(@"\b(?:true|false|nullptr)\b", RegexOptions.Compiled), "hljs-literal"),
            new HighlightRule(new Regex(@"\b\d+\.\d+(?:[eE][+-]?\d+)?[fFlL]?\b|\b\d+[uUlL]{0,2}\b|\b0[xX][0-9a-fA-F]+[uUlL]{0,2}\b", RegexOptions.Compiled), "hljs-number"),
            new HighlightRule(new Regex(@"#\s*(?:include|define|undef|if|ifdef|ifndef|else|elif|endif|line|error|pragma)\b", RegexOptions.Compiled), "hljs-meta"),
            new HighlightRule(new Regex(@"\b(?:std|cout|cin|endl|string|vector|map|set|list|deque|array|tuple|pair|optional|variant|unique_ptr|shared_ptr|make_unique|make_shared|size_t|uint8_t|uint16_t|uint32_t|uint64_t|int8_t|int16_t|int32_t|int64_t)\b", RegexOptions.Compiled), "hljs-built_in")
        ]),
        ["go"] = new LanguageRuleSet(
        [
            new HighlightRule(new Regex(@"/\*[\s\S]*?\*/", RegexOptions.Compiled), "hljs-comment"),
            new HighlightRule(new Regex(@"//.*?$", RegexOptions.Multiline | RegexOptions.Compiled), "hljs-comment"),
            new HighlightRule(new Regex(@"`[^`]*`", RegexOptions.Compiled), "hljs-string"),
            new HighlightRule(new Regex(@"""(?:[^""\\]|\\.)*""", RegexOptions.Compiled), "hljs-string"),
            new HighlightRule(new Regex(@"\b(?:break|case|chan|const|continue|default|defer|else|fallthrough|for|func|go|goto|if|import|interface|map|package|range|return|select|struct|switch|type|var)\b", RegexOptions.Compiled), "hljs-keyword"),
            new HighlightRule(new Regex(@"\b(?:true|false|nil|iota)\b", RegexOptions.Compiled), "hljs-literal"),
            new HighlightRule(new Regex(@"\b\d+\.\d+(?:[eE][+-]?\d+)?\b|\b\d+\b|\b0[xXoObB][0-9a-fA-F]+\b", RegexOptions.Compiled), "hljs-number"),
            new HighlightRule(new Regex(@"\b(?:bool|byte|complex64|complex128|error|float32|float64|int|int8|int16|int32|int64|rune|string|uint|uint8|uint16|uint32|uint64|uintptr|append|cap|close|copy|delete|imag|len|make|new|panic|print|println|real|recover|fmt|log|os|io|http|json|sql|time|context|sync|errors|strings|strconv|bytes|bufio|filepath|path|regexp|template|html|net|crypto|encoding|reflect|unsafe)\b", RegexOptions.Compiled), "hljs-built_in")
        ]),
        ["rust"] = new LanguageRuleSet(
        [
            new HighlightRule(new Regex(@"/\*[\s\S]*?\*/", RegexOptions.Compiled), "hljs-comment"),
            new HighlightRule(new Regex(@"//.*?$", RegexOptions.Multiline | RegexOptions.Compiled), "hljs-comment"),
            new HighlightRule(new Regex(@"""(?:[^""\\]|\\.)*""", RegexOptions.Compiled), "hljs-string"),
            new HighlightRule(new Regex(@"\b(?:as|async|await|break|const|continue|crate|dyn|else|enum|extern|fn|for|if|impl|in|let|loop|match|mod|move|mut|pub|ref|return|self|Self|static|struct|super|trait|type|unsafe|use|where|while|yield)\b", RegexOptions.Compiled), "hljs-keyword"),
            new HighlightRule(new Regex(@"\b(?:true|false)\b", RegexOptions.Compiled), "hljs-literal"),
            new HighlightRule(new Regex(@"\b\d+\.\d+(?:[eE][+-]?\d+)?(?:f32|f64)?\b|\b\d+(?:_?\d)*(?:i8|i16|i32|i64|i128|isize|u8|u16|u32|u64|u128|usize)?\b|\b0x[0-9a-fA-F_]+(?:i8|i16|i32|i64|i128|isize|u8|u16|u32|u64|u128|usize)?\b|\b0o[0-7_]+(?:i8|i16|i32|i64|i128|isize|u8|u16|u32|u64|u128|usize)?\b|\b0b[01_]+(?:i8|i16|i32|i64|i128|isize|u8|u16|u32|u64|u128|usize)?\b", RegexOptions.Compiled), "hljs-number"),
            new HighlightRule(new Regex(@"\b(?:bool|char|f32|f64|i8|i16|i32|i64|i128|isize|str|u8|u16|u32|u64|u128|usize|String|Vec|Box|Rc|Arc|Option|Result|Ok|Err|Some|None|HashMap|HashSet|BTreeMap|BTreeSet|Cow|Cell|RefCell|Mutex|RwLock|Duration|Instant|SystemTime|Path|PathBuf|Error|io|fmt|convert|clone|copy|default|debug|display|iterator|into|from|tryfrom|tryinto|Read|Write|Seek|BufRead|Display|Debug|Clone|Copy|Default|PartialEq|Eq|PartialOrd|Ord|Hash|Send|Sync|Sized|Unpin|Fn|FnMut|FnOnce|Drop|AsRef|AsMut|Into|From|TryFrom|TryInto|Iterator|IntoIterator|DoubleEndedIterator|ExactSizeIterator|Extend|FromIterator)\b", RegexOptions.Compiled), "hljs-built_in"),
            new HighlightRule(new Regex(@"\b(?:macro_rules|macro)\b", RegexOptions.Compiled), "hljs-meta"),
            new HighlightRule(new Regex(@"#!\[.*?\]", RegexOptions.Compiled), "hljs-meta")
        ]),
        ["sql"] = new LanguageRuleSet(
        [
            new HighlightRule(new Regex(@"--.*?$", RegexOptions.Multiline | RegexOptions.Compiled), "hljs-comment"),
            new HighlightRule(new Regex(@"/\*[\s\S]*?\*/", RegexOptions.Compiled), "hljs-comment"),
            new HighlightRule(new Regex(@"'[^']*'", RegexOptions.Compiled), "hljs-string"),
            new HighlightRule(new Regex(@"\b(?:SELECT|FROM|WHERE|INSERT|INTO|VALUES|UPDATE|SET|DELETE|CREATE|TABLE|ALTER|DROP|INDEX|VIEW|JOIN|INNER|LEFT|RIGHT|OUTER|FULL|CROSS|ON|AND|OR|NOT|IN|BETWEEN|LIKE|IS|NULL|AS|ORDER|BY|GROUP|HAVING|LIMIT|OFFSET|UNION|ALL|DISTINCT|EXISTS|CASE|WHEN|THEN|ELSE|END|BEGIN|COMMIT|ROLLBACK|TRANSACTION|GRANT|REVOKE|PRIMARY|KEY|FOREIGN|REFERENCES|CONSTRAINT|DEFAULT|CHECK|UNIQUE|AUTO_INCREMENT|CASCADE|IF|EXISTS|TOP|FETCH|NEXT|ROWS|ONLY|WITH|RECURSIVE|OVER|PARTITION|RANK|ROW_NUMBER|DENSE_RANK|COALESCE|CAST|CONVERT|ISNULL|TRUNCATE|MERGE|USING|NATURAL|EXCEPT|INTERSECT|ANY|SOME|ASC|DESC|NOT|NO|ACTION)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "hljs-keyword"),
            new HighlightRule(new Regex(@"\b(?:COUNT|SUM|AVG|MIN|MAX|UPPER|LOWER|LENGTH|SUBSTRING|TRIM|CONCAT|REPLACE|ROUND|FLOOR|CEILING|ABS|DATE|NOW|YEAR|MONTH|DAY|HOUR|MINUTE|SECOND|GETDATE|DATEADD|DATEDIFF|FORMAT|LEN|LEFT|RIGHT|CHARINDEX|STUFF|ISNULL|NULLIF|COALESCE|IDENTITY|SCOPE_IDENTITY)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "hljs-built_in"),
            new HighlightRule(new Regex(@"\b\d+\.\d+\b|\b\d+\b", RegexOptions.Compiled), "hljs-number")
        ]),
        ["html"] = new LanguageRuleSet(
        [
            new HighlightRule(new Regex(@"<!--[\s\S]*?-->", RegexOptions.Compiled), "hljs-comment"),
            new HighlightRule(new Regex(@"""[^""]*""", RegexOptions.Compiled), "hljs-string"),
            new HighlightRule(new Regex(@"'[^']*'", RegexOptions.Compiled), "hljs-string"),
            new HighlightRule(new Regex(@"\b(?:html|head|body|div|span|p|a|img|ul|ol|li|table|tr|td|th|form|input|button|select|option|textarea|label|h[1-6]|meta|link|script|style|title|header|footer|nav|main|section|article|aside|details|summary|figure|figcaption|template|slot|br|hr|pre|code|blockquote|em|strong|b|i|u|s|sub|sup|small|mark|del|ins|abbr|cite|dfn|kbd|samp|var|time|data|wbr|area|map|object|embed|iframe|source|track|audio|video|canvas|svg|math|picture|dialog|menu|menuitem|datalist|output|progress|meter|fieldset|legend|optgroup|keygen|base|col|colgroup|thead|tbody|tfoot|caption|address|ruby|rt|rp|bdi|bdo)\b", RegexOptions.Compiled), "hljs-tag"),
            new HighlightRule(new Regex(@"\b(?:id|class|style|href|src|alt|title|type|name|value|placeholder|action|method|target|rel|media|width|height|colspan|rowspan|disabled|readonly|required|checked|selected|multiple|autofocus|autocomplete|min|max|step|pattern|maxlength|minlength|content|charset|http-equiv|lang|dir|tabindex|accesskey|hidden|draggable|contenteditable|spellcheck|translate|role|aria-[a-z]+|data-[a-z-]+)\b", RegexOptions.Compiled), "hljs-attr")
        ]),
        ["css"] = new LanguageRuleSet(
        [
            new HighlightRule(new Regex(@"/\*[\s\S]*?\*/", RegexOptions.Compiled), "hljs-comment"),
            new HighlightRule(new Regex(@"""(?:[^""\\]|\\.)*""", RegexOptions.Compiled), "hljs-string"),
            new HighlightRule(new Regex(@"'(?:[^'\\]|\\.)*'", RegexOptions.Compiled), "hljs-string"),
            new HighlightRule(new Regex(@"\b(?:align-content|align-items|align-self|animation|animation-delay|animation-direction|animation-duration|animation-fill-mode|animation-iteration-count|animation-name|animation-play-state|animation-timing-function|appearance|backdrop-filter|backface-visibility|background|background-attachment|background-blend-mode|background-clip|background-color|background-image|background-origin|background-position|background-repeat|background-size|border|border-bottom|border-bottom-color|border-bottom-left-radius|border-bottom-right-radius|border-bottom-style|border-bottom-width|border-collapse|border-color|border-image|border-left|border-left-color|border-left-style|border-left-width|border-radius|border-right|border-right-color|border-right-style|border-right-width|border-spacing|border-style|border-top|border-top-color|border-top-left-radius|border-top-right-radius|border-top-style|border-top-width|border-width|bottom|box-decoration-break|box-shadow|box-sizing|caption-side|clear|clip-path|color|column-count|column-fill|column-gap|column-rule|column-span|column-width|columns|content|counter-increment|counter-reset|cursor|direction|display|empty-cells|filter|flex|flex-basis|flex-direction|flex-flow|flex-grow|flex-shrink|flex-wrap|float|font|font-family|font-feature-settings|font-kerning|font-language-override|font-size|font-size-adjust|font-stretch|font-style|font-variant|font-weight|gap|grid|grid-area|grid-auto-columns|grid-auto-flow|grid-auto-rows|grid-column|grid-column-end|grid-column-gap|grid-column-start|grid-gap|grid-row|grid-row-end|grid-row-gap|grid-row-start|grid-template|grid-template-areas|grid-template-columns|grid-template-rows|height|justify-content|justify-items|justify-self|left|letter-spacing|line-height|list-style|list-style-image|list-style-position|list-style-type|margin|margin-bottom|margin-left|margin-right|margin-top|max-height|max-width|min-height|min-width|mix-blend-mode|object-fit|object-position|opacity|order|outline|outline-color|outline-offset|outline-style|outline-width|overflow|overflow-wrap|overflow-x|overflow-y|padding|padding-bottom|padding-left|padding-right|padding-top|perspective|perspective-origin|place-content|place-items|place-self|pointer-events|position|quotes|resize|right|row-gap|scroll-behavior|tab-size|table-layout|text-align|text-align-last|text-combine-upright|text-decoration|text-decoration-color|text-decoration-line|text-decoration-style|text-indent|text-justify|text-orientation|text-overflow|text-shadow|text-transform|top|transform|transform-origin|transform-style|transition|transition-delay|transition-duration|transition-property|transition-timing-function|unicode-bidi|user-select|vertical-align|visibility|white-space|width|word-break|word-spacing|word-wrap|writing-mode|z-index)\b", RegexOptions.Compiled), "hljs-tag"),
            new HighlightRule(new Regex(@"\b(?:inherit|initial|unset|auto|none|block|inline|inline-block|flex|grid|inline-flex|inline-grid|relative|absolute|fixed|sticky|static|hidden|visible|scroll|solid|dashed|dotted|bold|bolder|lighter|normal|italic|center|left|right|justify|baseline|middle|top|bottom|cover|contain|pointer|default|transparent|currentColor|ease|ease-in|ease-out|ease-in-out|linear|step-start|step-end|forwards|backwards|both|infinite|alternate|alternate-reverse|normal|reverse|row|column|row-reverse|column-reverse|wrap|nowrap|wrap-reverse|space-between|space-around|space-evenly|stretch|start|end|flex-start|flex-end|center|max-content|min-content|fit-content|available|border-box|content-box|break-spaces|pre|pre-wrap|pre-line|nowrap|uppercase|lowercase|capitalize|underline|overline|line-through|blink|ellipsis|clip|scale|rotate|translate|translateX|translateY|translateZ|skew|matrix|perspective|rgb|rgba|hsl|hsla|var|calc|env|min|max|clamp|attr|url|linear-gradient|radial-gradient|conic-gradient|repeating-linear-gradient|repeating-radial-gradient|repeating-conic-gradient)\b", RegexOptions.Compiled), "hljs-built_in"),
            new HighlightRule(new Regex(@"#[0-9a-fA-F]{3,8}\b", RegexOptions.Compiled), "hljs-number"),
            new HighlightRule(new Regex(@"\b\d+\.?\d*(?:px|em|rem|vh|vw|vmin|vmax|%|cm|mm|in|pt|pc|ch|ex|fr|deg|rad|turn|s|ms|Hz|kHz|dpi|dpcm|dppx)?\b", RegexOptions.Compiled), "hljs-number"),
            new HighlightRule(new Regex(@"@[a-zA-Z-]+\b", RegexOptions.Compiled), "hljs-meta")
        ]),
        ["json"] = new LanguageRuleSet(
        [
            new HighlightRule(new Regex(@"""(?:[^""\\]|\\.)*""\s*:", RegexOptions.Compiled), "hljs-attr"),
            new HighlightRule(new Regex(@"""(?:[^""\\]|\\.)*""", RegexOptions.Compiled), "hljs-string"),
            new HighlightRule(new Regex(@"\b(?:true|false|null)\b", RegexOptions.Compiled), "hljs-literal"),
            new HighlightRule(new Regex(@"\b-?\d+\.?\d+(?:[eE][+-]?\d+)?\b", RegexOptions.Compiled), "hljs-number")
        ]),
        ["xml"] = new LanguageRuleSet(
        [
            new HighlightRule(new Regex(@"<!--[\s\S]*?-->", RegexOptions.Compiled), "hljs-comment"),
            new HighlightRule(new Regex(@"<!\[CDATA\[[\s\S]*?\]\]>", RegexOptions.Compiled), "hljs-string"),
            new HighlightRule(new Regex(@"""[^""]*""", RegexOptions.Compiled), "hljs-string"),
            new HighlightRule(new Regex(@"'[^']*'", RegexOptions.Compiled), "hljs-string"),
            new HighlightRule(new Regex(@"<\?[\s\S]*?\?>", RegexOptions.Compiled), "hljs-meta"),
            new HighlightRule(new Regex(@"<!DOCTYPE[^>]*>", RegexOptions.Compiled), "hljs-meta"),
            new HighlightRule(new Regex(@"</?[a-zA-Z_][\w.-]*", RegexOptions.Compiled), "hljs-tag"),
            new HighlightRule(new Regex(@"\b[a-zA-Z_][\w.-]*=", RegexOptions.Compiled), "hljs-attr")
        ]),
        ["yaml"] = new LanguageRuleSet(
        [
            new HighlightRule(new Regex(@"#.*$", RegexOptions.Multiline | RegexOptions.Compiled), "hljs-comment"),
            new HighlightRule(new Regex(@"""(?:[^""\\]|\\.)*""", RegexOptions.Compiled), "hljs-string"),
            new HighlightRule(new Regex(@"'(?:[^'\\]|\\.)*'", RegexOptions.Compiled), "hljs-string"),
            new HighlightRule(new Regex(@"\b(?:true|false|null|yes|no|on|off)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "hljs-literal"),
            new HighlightRule(new Regex(@"\b-?\d+\.?\d+(?:[eE][+-]?\d+)?\b", RegexOptions.Compiled), "hljs-number"),
            new HighlightRule(new Regex(@"[a-zA-Z_][\w-]*(?=\s*:)", RegexOptions.Compiled), "hljs-attr"),
            new HighlightRule(new Regex(@"---|\.\.\.", RegexOptions.Compiled), "hljs-meta")
        ]),
        ["markdown"] = new LanguageRuleSet(
        [
            new HighlightRule(new Regex(@"<!--[\s\S]*?-->", RegexOptions.Compiled), "hljs-comment"),
            new HighlightRule(new Regex(@"^#{1,6}\s+.*$", RegexOptions.Multiline | RegexOptions.Compiled), "hljs-section"),
            new HighlightRule(new Regex(@"```[\s\S]*?```", RegexOptions.Compiled), "hljs-string"),
            new HighlightRule(new Regex(@"`[^`]+`", RegexOptions.Compiled), "hljs-string"),
            new HighlightRule(new Regex(@"\*\*[^*]+\*\*|__[^_]+__", RegexOptions.Compiled), "hljs-strong"),
            new HighlightRule(new Regex(@"\*[^*]+\*|_[^_]+_", RegexOptions.Compiled), "hljs-emphasis"),
            new HighlightRule(new Regex(@"\[[^\]]*\]\([^)]*\)", RegexOptions.Compiled), "hljs-link"),
            new HighlightRule(new Regex(@"!\[[^\]]*\]\([^)]*\)", RegexOptions.Compiled), "hljs-symbol"),
            new HighlightRule(new Regex(@"^[-*+]\s", RegexOptions.Multiline | RegexOptions.Compiled), "hljs-bullet"),
            new HighlightRule(new Regex(@"^\d+\.\s", RegexOptions.Multiline | RegexOptions.Compiled), "hljs-bullet"),
            new HighlightRule(new Regex(@"^>\s.*$", RegexOptions.Multiline | RegexOptions.Compiled), "hljs-quote"),
            new HighlightRule(new Regex(@"^---+$|^\*\*\*+$|^___+$", RegexOptions.Multiline | RegexOptions.Compiled), "hljs-meta")
        ]),
        ["bash"] = new LanguageRuleSet(
        [
            new HighlightRule(new Regex(@"#[^!].*?$|#$", RegexOptions.Multiline | RegexOptions.Compiled), "hljs-comment"),
            new HighlightRule(new Regex(@"""(?:[^""\\]|\\.)*""", RegexOptions.Compiled), "hljs-string"),
            new HighlightRule(new Regex(@"'(?:[^'\\]|\\.)*'", RegexOptions.Compiled), "hljs-string"),
            new HighlightRule(new Regex(@"\b(?:if|then|else|elif|fi|case|esac|for|while|until|do|done|in|function|select|time|coproc|return|exit|break|continue|declare|export|local|readonly|typeset|unset|source|alias|unalias|set|shift|eval|trap|type|builtin|command|read|readarray|mapfile|printf|echo|cd|pwd|pushd|popd|dirs|ls|cp|mv|rm|mkdir|rmdir|cat|head|tail|grep|sed|awk|find|xargs|sort|uniq|wc|cut|tr|tee|pipe|chmod|chown|chgrp|test|expr|let|true|false)\b", RegexOptions.Compiled), "hljs-keyword"),
            new HighlightRule(new Regex(@"\$\{[^}]+\}|\$[A-Za-z_]\w*|\$\([^)]+\)", RegexOptions.Compiled), "hljs-variable"),
            new HighlightRule(new Regex(@"\b\d+\b", RegexOptions.Compiled), "hljs-number")
        ]),
        ["powershell"] = new LanguageRuleSet(
        [
            new HighlightRule(new Regex(@"<#[\s\S]*?#>", RegexOptions.Compiled), "hljs-comment"),
            new HighlightRule(new Regex(@"#.*?$", RegexOptions.Multiline | RegexOptions.Compiled), "hljs-comment"),
            new HighlightRule(new Regex(@"""(?:[^""\\]|\\.)*""", RegexOptions.Compiled), "hljs-string"),
            new HighlightRule(new Regex(@"'(?:[^'\\]|\\.)*'", RegexOptions.Compiled), "hljs-string"),
            new HighlightRule(new Regex(@"\b(?:begin|break|catch|class|continue|data|define|do|dynamicparam|else|elseif|end|exit|filter|finally|for|foreach|from|function|if|in|inlinescript|parallel|param|process|return|switch|throw|trap|try|until|using|var|while|workflow)\b", RegexOptions.Compiled), "hljs-keyword"),
            new HighlightRule(new Regex(@"\b(?:true|false|null)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled), "hljs-literal"),
            new HighlightRule(new Regex(@"\$\w+", RegexOptions.Compiled), "hljs-variable"),
            new HighlightRule(new Regex(@"\b\d+\.\d+\b|\b\d+\b", RegexOptions.Compiled), "hljs-number"),
            new HighlightRule(new Regex(@"\b(?:Write-Host|Write-Output|Write-Error|Write-Warning|Write-Verbose|Write-Debug|Get-Content|Set-Content|Out-File|Get-ChildItem|Get-Process|Stop-Process|Get-Service|Start-Service|Stop-Service|Test-Path|New-Item|Remove-Item|Copy-Item|Move-Item|Select-Object|Where-Object|ForEach-Object|Sort-Object|Group-Object|Measure-Object|Import-Module|Export-ModuleMember|Invoke-Command|Start-Job|Wait-Job|Receive-Job|Enter-PSSession|Exit-PSSession|New-PSSession|Remove-PSSession|ConvertTo-Json|ConvertFrom-Json|Invoke-RestMethod|Invoke-WebRequest|Get-Date|Set-Date|Start-Sleep|Read-Host|Get-Random|Get-Member|Select-String|Compare-Object|Tee-Object)\b", RegexOptions.Compiled), "hljs-built_in")
        ])
    };

    private sealed record Token(int Index, int Length, string CssClass);

    private sealed record HighlightRule(Regex Regex, string CssClass);

    private sealed class LanguageRuleSet(List<HighlightRule> Rules)
    {
        public List<HighlightRule> Rules { get; } = Rules;
    }
}
