namespace OneMarkDotNet
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.Text.RegularExpressions;

    public sealed class CodeHighlightService
    {
        private static readonly string[] CSharpKeywords = new string[]
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char",
            "checked", "class", "const", "continue", "decimal", "default", "delegate", "do",
            "double", "else", "enum", "event", "explicit", "extern", "false", "finally",
            "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int",
            "interface", "internal", "is", "lock", "long", "namespace", "new", "null",
            "object", "operator", "out", "override", "params", "private", "protected",
            "public", "readonly", "ref", "return", "sbyte", "sealed", "short", "sizeof",
            "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true",
            "try", "typeof", "uint", "ulong", "unchecked", "unsafe", "ushort", "using",
            "virtual", "void", "volatile", "while", "var", "async", "await", "dynamic",
            "nameof", "when", "record", "init", "with"
        };

        private static readonly string[] JavaKeywords = new string[]
        {
            "abstract", "assert", "boolean", "break", "byte", "case", "catch", "char",
            "class", "const", "continue", "default", "do", "double", "else", "enum",
            "extends", "final", "finally", "float", "for", "goto", "if", "implements",
            "import", "instanceof", "int", "interface", "long", "native", "new", "null",
            "package", "private", "protected", "public", "return", "short", "static",
            "strictfp", "super", "switch", "synchronized", "this", "throw", "throws",
            "transient", "try", "void", "volatile", "while", "var", "record", "sealed",
            "permits", "yield"
        };

        private static readonly string[] PythonKeywords = new string[]
        {
            "False", "None", "True", "and", "as", "assert", "async", "await", "break",
            "class", "continue", "def", "del", "elif", "else", "except", "finally",
            "for", "from", "global", "if", "import", "in", "is", "lambda", "nonlocal",
            "not", "or", "pass", "raise", "return", "try", "while", "with", "yield",
            "self", "print", "range", "len", "str", "int", "float", "list", "dict",
            "set", "tuple", "bool", "type", "super", "input", "open"
        };

        private static readonly string[] JavaScriptKeywords = new string[]
        {
            "break", "case", "catch", "class", "const", "continue", "debugger", "default",
            "delete", "do", "else", "export", "extends", "false", "finally", "for",
            "function", "if", "import", "in", "instanceof", "let", "new", "null", "of",
            "return", "super", "switch", "this", "throw", "true", "try", "typeof",
            "undefined", "var", "void", "while", "with", "yield", "async", "await",
            "from", "as", "static", "get", "set"
        };

        private static readonly string[] TypeScriptKeywords = new string[]
        {
            "break", "case", "catch", "class", "const", "continue", "debugger", "default",
            "delete", "do", "else", "enum", "export", "extends", "false", "finally",
            "for", "function", "if", "import", "in", "instanceof", "let", "new", "null",
            "of", "return", "super", "switch", "this", "throw", "true", "try", "typeof",
            "undefined", "var", "void", "while", "with", "yield", "async", "await",
            "from", "as", "static", "get", "set", "type", "interface", "namespace",
            "declare", "abstract", "implements", "readonly", "keyof", "infer", "is",
            "never", "unknown", "any", "void", "object", "string", "number", "boolean",
            "symbol", "bigint", "public", "private", "protected"
        };

        private static readonly string[] CppKeywords = new string[]
        {
            "alignas", "alignof", "and", "and_eq", "asm", "auto", "bitand", "bitor",
            "bool", "break", "case", "catch", "char", "char8_t", "char16_t", "char32_t",
            "class", "compl", "concept", "const", "consteval", "constexpr", "const_cast",
            "continue", "co_await", "co_return", "co_yield", "decltype", "default",
            "delete", "do", "double", "dynamic_cast", "else", "enum", "explicit",
            "export", "extern", "false", "float", "for", "friend", "goto", "if",
            "inline", "int", "long", "mutable", "namespace", "new", "noexcept", "not",
            "not_eq", "nullptr", "operator", "or", "or_eq", "private", "protected",
            "public", "register", "reinterpret_cast", "requires", "return", "short",
            "signed", "sizeof", "static", "static_assert", "static_cast", "struct",
            "switch", "template", "this", "thread_local", "throw", "true", "try",
            "typedef", "typeid", "typename", "union", "unsigned", "using", "virtual",
            "void", "volatile", "wchar_t", "while", "xor", "xor_eq", "size_t",
            "uint8_t", "uint16_t", "uint32_t", "uint64_t", "int8_t", "int16_t",
            "int32_t", "int64_t"
        };

        private static readonly string[] GoKeywords = new string[]
        {
            "break", "case", "chan", "const", "continue", "default", "defer", "else",
            "fallthrough", "for", "func", "go", "goto", "if", "import", "interface",
            "map", "package", "range", "return", "select", "struct", "switch", "type",
            "var", "true", "false", "nil", "append", "cap", "close", "copy", "delete",
            "len", "make", "new", "panic", "print", "println", "recover", "iota",
            "string", "int", "int8", "int16", "int32", "int64", "uint", "uint8",
            "uint16", "uint32", "uint64", "float32", "float64", "bool", "byte",
            "rune", "error", "complex64", "complex128"
        };

        private static readonly string[] RustKeywords = new string[]
        {
            "as", "async", "await", "break", "const", "continue", "crate", "dyn",
            "else", "enum", "extern", "false", "fn", "for", "if", "impl", "in",
            "let", "loop", "match", "mod", "move", "mut", "pub", "ref", "return",
            "self", "Self", "static", "struct", "super", "trait", "true", "type",
            "unsafe", "use", "where", "while", "i8", "i16", "i32", "i64", "i128",
            "u8", "u16", "u32", "u64", "u128", "f32", "f64", "bool", "char",
            "str", "String", "Vec", "Option", "Result", "Box", "Rc", "Arc"
        };

        private static readonly string[] PhpKeywords = new string[]
        {
            "abstract", "and", "array", "as", "break", "callable", "case", "catch",
            "class", "clone", "const", "continue", "declare", "default", "die", "do",
            "echo", "else", "elseif", "empty", "enddeclare", "endfor", "endforeach",
            "endif", "endswitch", "endwhile", "eval", "exit", "extends", "final",
            "finally", "fn", "for", "foreach", "function", "global", "goto", "if",
            "implements", "include", "include_once", "instanceof", "insteadof",
            "interface", "isset", "list", "match", "namespace", "new", "or", "print",
            "private", "protected", "public", "require", "require_once", "return",
            "static", "switch", "throw", "trait", "try", "unset", "use", "var",
            "while", "xor", "yield", "int", "float", "bool", "string", "true",
            "false", "null", "void", "iterable", "object", "mixed", "never", "self",
            "parent"
        };

        private static readonly string[] RubyKeywords = new string[]
        {
            "BEGIN", "END", "alias", "and", "begin", "break", "case", "class", "def",
            "defined?", "do", "else", "elsif", "end", "ensure", "false", "for", "if",
            "in", "module", "next", "nil", "not", "or", "redo", "rescue", "retry",
            "return", "self", "super", "then", "true", "undef", "unless", "until",
            "when", "while", "yield", "require", "include", "attr_accessor",
            "attr_reader", "attr_writer", "raise", "puts", "print", "gets", "chomp",
            "to_s", "to_i", "to_f", "new", "initialize", "each", "map", "select",
            "reject", "reduce", "collect"
        };

        private static readonly string[] SwiftKeywords = new string[]
        {
            "associatedtype", "class", "deinit", "enum", "extension", "fileprivate",
            "func", "import", "init", "inout", "internal", "let", "open", "operator",
            "private", "protocol", "public", "rethrows", "static", "struct", "subscript",
            "typealias", "var", "break", "case", "continue", "default", "defer", "do",
            "else", "fallthrough", "for", "guard", "if", "in", "repeat", "return",
            "switch", "throw", "where", "while", "as", "Any", "catch", "false", "is",
            "nil", "super", "self", "Self", "throw", "true", "try", "throws", "async",
            "await", "some", "any", "actor", "isolated", "nonisolated", "convenience",
            "required", "override", "lazy", "weak", "unowned", "mutating", "nonmutating",
            "optional", "indirect", "Type", "Int", "Double", "Float", "String", "Bool",
            "Array", "Dictionary", "Set"
        };

        private static readonly string[] KotlinKeywords = new string[]
        {
            "abstract", "actual", "annotation", "as", "break", "by", "catch", "class",
            "companion", "const", "constructor", "continue", "crossinline", "data",
            "delegate", "do", "else", "enum", "expect", "external", "false", "final",
            "finally", "for", "fun", "if", "in", "infix", "init", "inline", "inner",
            "interface", "internal", "is", "it", "lateinit", "noinline", "null",
            "object", "open", "operator", "out", "override", "package", "private",
            "protected", "public", "reified", "return", "sealed", "suspend", "tailrec",
            "this", "throw", "true", "try", "typealias", "typeof", "val", "var",
            "vararg", "when", "while", "yield", "Int", "Long", "Double", "Float",
            "String", "Boolean", "List", "Map", "Set", "Unit", "Nothing", "Any",
            "Array"
        };

        private static readonly string[] SqlKeywords = new string[]
        {
            "SELECT", "FROM", "WHERE", "INSERT", "INTO", "VALUES", "UPDATE", "SET",
            "DELETE", "CREATE", "TABLE", "ALTER", "DROP", "INDEX", "VIEW", "JOIN",
            "INNER", "LEFT", "RIGHT", "OUTER", "FULL", "CROSS", "ON", "AND", "OR",
            "NOT", "NULL", "IS", "IN", "BETWEEN", "LIKE", "AS", "ORDER", "BY",
            "GROUP", "HAVING", "LIMIT", "OFFSET", "UNION", "ALL", "DISTINCT",
            "EXISTS", "CASE", "WHEN", "THEN", "ELSE", "END", "COUNT", "SUM", "AVG",
            "MIN", "MAX", "PRIMARY", "KEY", "FOREIGN", "REFERENCES", "CONSTRAINT",
            "DEFAULT", "CHECK", "UNIQUE", "ASC", "DESC", "IF", "BEGIN", "COMMIT",
            "ROLLBACK", "TRANSACTION", "GRANT", "REVOKE", "TRUNCATE", "EXPLAIN",
            "OVER", "PARTITION", "ROW_NUMBER", "RANK", "DENSE_RANK", "WITH",
            "RECURSIVE", "MERGE", "USING", "MATCHED", "TOP"
        };

        private static readonly string[] HtmlKeywords = new string[]
        {
            "html", "head", "body", "div", "span", "p", "a", "img", "ul", "ol", "li",
            "h1", "h2", "h3", "h4", "h5", "h6", "table", "tr", "td", "th", "thead",
            "tbody", "tfoot", "form", "input", "button", "select", "option", "textarea",
            "label", "script", "style", "link", "meta", "title", "header", "footer",
            "nav", "main", "section", "article", "aside", "figure", "figcaption",
            "details", "summary", "video", "audio", "source", "canvas", "svg", "br",
            "hr", "strong", "em", "code", "pre", "blockquote", "iframe", "template",
            "slot"
        };

        private static readonly string[] CssKeywords = new string[]
        {
            "color", "background", "background-color", "background-image", "border",
            "border-radius", "margin", "padding", "width", "height", "display", "position",
            "top", "left", "right", "bottom", "float", "clear", "overflow", "font",
            "font-size", "font-weight", "font-family", "text-align", "text-decoration",
            "text-transform", "line-height", "letter-spacing", "flex", "flex-direction",
            "flex-wrap", "justify-content", "align-items", "align-self", "grid",
            "grid-template-columns", "grid-template-rows", "gap", "opacity", "z-index",
            "transition", "transform", "animation", "box-shadow", "cursor", "outline",
            "visibility", "content", "list-style", "white-space", "word-wrap",
            "important", "inherit", "initial", "unset", "auto", "none", "block",
            "inline", "inline-block", "flex", "grid", "absolute", "relative",
            "fixed", "sticky", "hidden", "visible", "solid", "dashed", "dotted"
        };

        private static readonly string[] JsonKeywords = new string[]
        {
            "true", "false", "null"
        };

        private static readonly string[] PowerShellKeywords = new string[]
        {
            "begin", "break", "catch", "class", "continue", "data", "define", "do",
            "dynamicparam", "else", "elseif", "end", "enum", "exit", "filter", "finally",
            "for", "foreach", "from", "function", "hidden", "if", "in", "param",
            "process", "return", "static", "switch", "throw", "trap", "try", "until",
            "using", "var", "while", "workflow", "parallel", "sequence", "inlinescript",
            "configuration", "true", "false", "null", "Write-Host", "Write-Output",
            "Write-Error", "Write-Warning", "Get-Content", "Set-Content", "Get-ChildItem",
            "Get-Process", "Get-Service", "Select-Object", "Where-Object", "ForEach-Object",
            "Sort-Object", "Measure-Object", "New-Object", "Invoke-Command", "Test-Path",
            "Join-Path", "Split-Path", "Out-Null", "Out-String"
        };

        private readonly Dictionary<string, LanguageRuleSet> ruleSets;
        private readonly Dictionary<string, string> languageAliases;

        public CodeHighlightService()
        {
            ruleSets = new Dictionary<string, LanguageRuleSet>(StringComparer.OrdinalIgnoreCase);
            languageAliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "js", "JavaScript" },
                { "cs", "CSharp" },
                { "py", "Python" },
                { "ts", "TypeScript" },
                { "c++", "Cpp" },
                { "golang", "Go" },
                { "rb", "Ruby" },
                { "kot", "Kotlin" },
                { "ps1", "PowerShell" }
            };

            InitializeRuleSets();
        }

        private void InitializeRuleSets()
        {
            ruleSets["CSharp"] = new LanguageRuleSet
            {
                Keywords = CSharpKeywords,
                StringPattern = @"@?""(?:[^""]|"""")*""|'(?:[^'\\]|\\.)*'",
                CommentPattern = @"///.*?$|//.*?$|/\*[\s\S]*?\*/",
                NumberPattern = @"\b\d+(\.\d+)?([eE][+-]?\d+)?[fFdDmMlLuU]?\b"
            };

            ruleSets["Java"] = new LanguageRuleSet
            {
                Keywords = JavaKeywords,
                StringPattern = @"""(?:[^""\\]|\\.)*""|'(?:[^'\\]|\\.)*'",
                CommentPattern = @"//.*?$|/\*[\s\S]*?\*/",
                NumberPattern = @"\b\d+(\.\d+)?([eE][+-]?\d+)?[fFdDlL]?\b"
            };

            ruleSets["Python"] = new LanguageRuleSet
            {
                Keywords = PythonKeywords,
                StringPattern = @"@?""" + "\"" + @"(?:[^""\\]|\\.)*""" + "\"" + @"|@?'(?:[^'\\]|\\.)*'|(?:r|b|f|rb|br|fr|rf|bf)?""" + "\"" + @"(?:[^""\\]|\\.)*""" + "\"" + @"|(?:r|b|f|rb|br|fr|rf|bf)?'(?:[^'\\]|\\.)*'",
                CommentPattern = @"#.*?$|""" + "\"" + "\"" + @"[\s\S]*?""" + "\"" + "\"" + @"|'''[\s\S]*?'''",
                NumberPattern = @"\b\d+(\.\d+)?([eE][+-]?\d+)?[jJ]?\b|0[xX][0-9a-fA-F]+|0[bB][01]+|0[oO][0-7]+"
            };

            ruleSets["JavaScript"] = new LanguageRuleSet
            {
                Keywords = JavaScriptKeywords,
                StringPattern = @"""(?:[^""\\]|\\.)*""|'(?:[^'\\]|\\.)*'|`(?:[^`\\]|\\.)*`",
                CommentPattern = @"//.*?$|/\*[\s\S]*?\*/",
                NumberPattern = @"\b\d+(\.\d+)?([eE][+-]?\d+)?[n]?\b|0[xX][0-9a-fA-F]+|0[bB][01]+|0[oO][0-7]+"
            };

            ruleSets["TypeScript"] = new LanguageRuleSet
            {
                Keywords = TypeScriptKeywords,
                StringPattern = @"""(?:[^""\\]|\\.)*""|'(?:[^'\\]|\\.)*'|`(?:[^`\\]|\\.)*`",
                CommentPattern = @"//.*?$|/\*[\s\S]*?\*/",
                NumberPattern = @"\b\d+(\.\d+)?([eE][+-]?\d+)?[n]?\b|0[xX][0-9a-fA-F]+|0[bB][01]+|0[oO][0-7]+"
            };

            ruleSets["Cpp"] = new LanguageRuleSet
            {
                Keywords = CppKeywords,
                StringPattern = @"""(?:[^""\\]|\\.)*""|'(?:[^'\\]|\\.)*'|L""(?:[^""\\]|\\.)*""|L'(?:[^'\\]|\\.)*'",
                CommentPattern = @"//.*?$|/\*[\s\S]*?\*/",
                NumberPattern = @"\b\d+(\.\d+)?([eE][+-]?\d+)?[fFlLuU]{0,2}\b|0[xX][0-9a-fA-F]+[uUlL]{0,2}"
            };

            ruleSets["Go"] = new LanguageRuleSet
            {
                Keywords = GoKeywords,
                StringPattern = @"""(?:[^""\\]|\\.)*""|`[^`]*`",
                CommentPattern = @"//.*?$|/\*[\s\S]*?\*/",
                NumberPattern = @"\b\d+(\.\d+)?([eE][+-]?\d+)?\b|0[xX][0-9a-fA-F]+|0[bB][01]+|0[oO][0-7]+"
            };

            ruleSets["Rust"] = new LanguageRuleSet
            {
                Keywords = RustKeywords,
                StringPattern = @"""(?:[^""\\]|\\.)*""|'(?:[^'\\]|\\.)*'|r""(?:[^""]|"""")*""|r#""(?:[^#]|#[^""])*""#",
                CommentPattern = @"//.*?$|/\*[\s\S]*?\*/|///.*?$",
                NumberPattern = @"\b\d+(\.\d+)?([eE][+-]?\d+)?([fFiIuU]\d*)?\b|0[xX][0-9a-fA-F_]+|0[bB][01_]+|0[oO][0-7_]+"
            };

            ruleSets["PHP"] = new LanguageRuleSet
            {
                Keywords = PhpKeywords,
                StringPattern = @"""(?:[^""\\]|\\.)*""|'(?:[^'\\]|\\.)*'",
                CommentPattern = @"//.*?$|/\*[\s\S]*?\*/|#.*?$",
                NumberPattern = @"\b\d+(\.\d+)?([eE][+-]?\d+)?\b|0[xX][0-9a-fA-F]+|0[bB][01]+|0[oO][0-7]+"
            };

            ruleSets["Ruby"] = new LanguageRuleSet
            {
                Keywords = RubyKeywords,
                StringPattern = @"""(?:[^""\\]|\\.)*""|'(?:[^'\\]|\\.)*'|:(?:""""(?:[^""\\]|\\.)*""""|'(?:[^'\\]|\\.)*')",
                CommentPattern = @"#.*?$|=begin[\s\S]*?=end",
                NumberPattern = @"\b\d+(\.\d+)?([eE][+-]?\d+)?\b|0[xX][0-9a-fA-F]+|0[bB][01]+|0[oO][0-7]+"
            };

            ruleSets["Swift"] = new LanguageRuleSet
            {
                Keywords = SwiftKeywords,
                StringPattern = @"""(?:[^""\\]|\\.)*""",
                CommentPattern = @"//.*?$|/\*[\s\S]*?\*/|///.*?$",
                NumberPattern = @"\b\d+(\.\d+)?([eE][+-]?\d+)?\b|0[xX][0-9a-fA-F]+|0[bB][01]+|0[oO][0-7]+"
            };

            ruleSets["Kotlin"] = new LanguageRuleSet
            {
                Keywords = KotlinKeywords,
                StringPattern = @"""(?:[^""\\]|\\.)*""",
                CommentPattern = @"//.*?$|/\*[\s\S]*?\*/",
                NumberPattern = @"\b\d+(\.\d+)?([eE][+-]?\d+)?[fFLl]?\b|0[xX][0-9a-fA-F]+|0[bB][01]+"
            };

            ruleSets["SQL"] = new LanguageRuleSet
            {
                Keywords = SqlKeywords,
                StringPattern = @"'(?:[^'\\]|\\.)*'|""(?:[^""]|"""")*""",
                CommentPattern = @"--.*?$|/\*[\s\S]*?\*/",
                NumberPattern = @"\b\d+(\.\d+)?\b"
            };

            ruleSets["HTML"] = new LanguageRuleSet
            {
                Keywords = HtmlKeywords,
                StringPattern = @"""(?:[^""\\]|\\.)*""|'(?:[^'\\]|\\.)*'",
                CommentPattern = @"<!--[\s\S]*?-->",
                NumberPattern = @"\b\d+(\.\d+)?\b"
            };

            ruleSets["CSS"] = new LanguageRuleSet
            {
                Keywords = CssKeywords,
                StringPattern = @"""(?:[^""\\]|\\.)*""|'(?:[^'\\]|\\.)*'",
                CommentPattern = @"/\*[\s\S]*?\*/",
                NumberPattern = @"[#][0-9a-fA-F]{3,8}\b|\b\d+(\.\d+)?(%|px|em|rem|vh|vw|pt|cm|mm|in|s|ms|deg|rad|grad|fr)?\b"
            };

            ruleSets["JSON"] = new LanguageRuleSet
            {
                Keywords = JsonKeywords,
                StringPattern = @"""(?:[^""\\]|\\.)*""",
                CommentPattern = "",
                NumberPattern = @"\b-?\d+(\.\d+)?([eE][+-]?\d+)?\b"
            };

            ruleSets["PowerShell"] = new LanguageRuleSet
            {
                Keywords = PowerShellKeywords,
                StringPattern = @"""(?:[^""]|"""")*""|'(?:[^']|'')*'",
                CommentPattern = @"#.*?$|<#[\s\S]*?#>",
                NumberPattern = @"\b\d+(\.\d+)?\b|0[xX][0-9a-fA-F]+"
            };
        }

        public string HighlightCode(string code, string language, bool showLineNumbers)
        {
            if (string.IsNullOrEmpty(code))
            {
                return string.Empty;
            }

            var resolvedLanguage = ResolveLanguage(language);
            var ruleSet = GetRuleSet(resolvedLanguage);

            var highlighted = TokenizeAndHighlight(code, ruleSet);

            if (showLineNumbers)
            {
                highlighted = AddLineNumbers(highlighted);
            }

            return highlighted;
        }

        private string ResolveLanguage(string language)
        {
            if (string.IsNullOrEmpty(language))
            {
                return string.Empty;
            }

            string aliasKey = language.Trim();
            if (languageAliases.ContainsKey(aliasKey))
            {
                return languageAliases[aliasKey];
            }

            return aliasKey;
        }

        private LanguageRuleSet GetRuleSet(string language)
        {
            if (!string.IsNullOrEmpty(language) && ruleSets.ContainsKey(language))
            {
                return ruleSets[language];
            }

            return new LanguageRuleSet
            {
                Keywords = Array.Empty<string>(),
                StringPattern = @"""(?:[^""\\]|\\.)*""|'(?:[^'\\]|\\.)*'",
                CommentPattern = @"//.*?$|/\*[\s\S]*?\*/|#.*?$",
                NumberPattern = @"\b\d+(\.\d+)?\b"
            };
        }

        private string TokenizeAndHighlight(string code, LanguageRuleSet ruleSet)
        {
            var tokens = new List<Token>();
            var occupied = new bool[code.Length];

            ExtractTokens(tokens, occupied, code, ruleSet.CommentPattern, TokenType.Comment);
            ExtractTokens(tokens, occupied, code, ruleSet.StringPattern, TokenType.String);
            ExtractTokens(tokens, occupied, code, ruleSet.NumberPattern, TokenType.Number);

            if (ruleSet.Keywords != null && ruleSet.Keywords.Length > 0)
            {
                ExtractKeywordTokens(tokens, occupied, code, ruleSet.Keywords);
            }

            ExtractFunctionTokens(tokens, occupied, code);

            tokens.Sort((a, b) =>
            {
                int cmp = a.Start.CompareTo(b.Start);
                if (cmp != 0) return cmp;
                return a.Length.CompareTo(b.Length);
            });

            var sb = new StringBuilder(code.Length * 2);
            int lastEnd = 0;

            foreach (var token in tokens)
            {
                if (token.Start < lastEnd)
                {
                    continue;
                }

                if (token.Start > lastEnd)
                {
                    AppendEscaped(sb, code, lastEnd, token.Start - lastEnd);
                }

                var tokenText = code.Substring(token.Start, token.Length);
                var cssClass = GetCssClass(token.Type);
                sb.Append("<span class=\"");
                sb.Append(cssClass);
                sb.Append("\">");
                AppendEscaped(sb, tokenText);
                sb.Append("</span>");

                lastEnd = token.Start + token.Length;
            }

            if (lastEnd < code.Length)
            {
                AppendEscaped(sb, code, lastEnd, code.Length - lastEnd);
            }

            return sb.ToString();
        }

        private void ExtractTokens(List<Token> tokens, bool[] occupied, string code, string pattern, TokenType type)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                return;
            }

            try
            {
                var regex = new Regex(pattern, RegexOptions.Multiline | RegexOptions.Compiled);
                foreach (Match match in regex.Matches(code))
                {
                    if (match.Success && !IsOccupied(occupied, match.Index, match.Length))
                    {
                        tokens.Add(new Token
                        {
                            Start = match.Index,
                            Length = match.Length,
                            Type = type
                        });
                        MarkOccupied(occupied, match.Index, match.Length);
                    }
                }
            }
            catch (ArgumentException)
            {
                // Invalid regex pattern, skip
            }
        }

        private void ExtractKeywordTokens(List<Token> tokens, bool[] occupied, string code, string[] keywords)
        {
            var keywordPattern = @"\b(" + string.Join("|", keywords) + @")\b";
            try
            {
                var regex = new Regex(keywordPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
                foreach (Match match in regex.Matches(code))
                {
                    if (match.Success && !IsOccupied(occupied, match.Index, match.Length))
                    {
                        tokens.Add(new Token
                        {
                            Start = match.Index,
                            Length = match.Length,
                            Type = TokenType.Keyword
                        });
                        MarkOccupied(occupied, match.Index, match.Length);
                    }
                }
            }
            catch (ArgumentException)
            {
                // Invalid regex pattern, skip
            }
        }

        private void ExtractFunctionTokens(List<Token> tokens, bool[] occupied, string code)
        {
            var funcPattern = @"\b([a-zA-Z_]\w*)\s*(?=\()";
            try
            {
                var regex = new Regex(funcPattern, RegexOptions.Compiled);
                foreach (Match match in regex.Matches(code))
                {
                    if (match.Success && match.Groups[1].Success && !IsOccupied(occupied, match.Groups[1].Index, match.Groups[1].Length))
                    {
                        var group = match.Groups[1];
                        tokens.Add(new Token
                        {
                            Start = group.Index,
                            Length = group.Length,
                            Type = TokenType.Function
                        });
                        MarkOccupied(occupied, group.Index, group.Length);
                    }
                }
            }
            catch (ArgumentException)
            {
                // Invalid regex pattern, skip
            }
        }

        private static bool IsOccupied(bool[] occupied, int start, int length)
        {
            int end = start + length;
            for (int i = start; i < end && i < occupied.Length; i++)
            {
                if (occupied[i])
                {
                    return true;
                }
            }
            return false;
        }

        private static void MarkOccupied(bool[] occupied, int start, int length)
        {
            int end = start + length;
            for (int i = start; i < end && i < occupied.Length; i++)
            {
                occupied[i] = true;
            }
        }

        private static string GetCssClass(TokenType type)
        {
            switch (type)
            {
                case TokenType.Keyword:
                    return "kw";
                case TokenType.String:
                    return "str";
                case TokenType.Comment:
                    return "cmt";
                case TokenType.Number:
                    return "num";
                case TokenType.Function:
                    return "fn";
                default:
                    return string.Empty;
            }
        }

        private static void AppendEscaped(StringBuilder sb, string text)
        {
            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                switch (c)
                {
                    case '&':
                        sb.Append("&amp;");
                        break;
                    case '<':
                        sb.Append("&lt;");
                        break;
                    case '>':
                        sb.Append("&gt;");
                        break;
                    case '"':
                        sb.Append("&quot;");
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }
        }

        private static void AppendEscaped(StringBuilder sb, string text, int startIndex, int length)
        {
            int end = startIndex + length;
            for (int i = startIndex; i < end; i++)
            {
                char c = text[i];
                switch (c)
                {
                    case '&':
                        sb.Append("&amp;");
                        break;
                    case '<':
                        sb.Append("&lt;");
                        break;
                    case '>':
                        sb.Append("&gt;");
                        break;
                    case '"':
                        sb.Append("&quot;");
                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }
        }

        private static string AddLineNumbers(string html)
        {
            var lines = html.Split(new string[] { "\n" }, StringSplitOptions.None);
            var sb = new StringBuilder(html.Length + lines.Length * 30);
            int lineNumber = 1;
            int maxLineNumberWidth = lines.Length.ToString().Length;

            for (int i = 0; i < lines.Length; i++)
            {
                sb.Append("<span class=\"line-number\">");
                sb.Append(lineNumber.ToString().PadLeft(maxLineNumberWidth));
                sb.Append("</span>");
                sb.Append(lines[i]);

                if (i < lines.Length - 1)
                {
                    sb.Append("\n");
                }

                lineNumber++;
            }

            return sb.ToString();
        }

        private enum TokenType
        {
            Keyword,
            String,
            Comment,
            Number,
            Function
        }

        private sealed class Token
        {
            public int Start { get; set; }
            public int Length { get; set; }
            public TokenType Type { get; set; }
        }

        private sealed class LanguageRuleSet
        {
            public string[] Keywords { get; set; }
            public string StringPattern { get; set; }
            public string CommentPattern { get; set; }
            public string NumberPattern { get; set; }
        }
    }
}
