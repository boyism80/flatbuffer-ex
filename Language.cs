namespace FlatBufferEx
{
    public sealed class Language
    {
        public static readonly Language Cpp = new("c++", "cpp", "cpp/protocol.txt", ".h", "::", "{0}_", true, new HashSet<string>
        {
            "alignas", "alignof", "and", "and_eq", "asm", "auto", "bitand", "bitor", "bool", "break", "case", "catch",
            "char", "char8_t", "char16_t", "char32_t", "class", "compl", "concept", "const", "consteval", "constexpr",
            "constinit", "const_cast", "continue", "co_await", "co_return", "co_yield", "decltype", "default", "delete",
            "do", "double", "dynamic_cast", "else", "enum", "explicit", "export", "extern", "false", "float", "for",
            "friend", "goto", "if", "inline", "int", "long", "mutable", "namespace", "new", "noexcept", "not", "not_eq",
            "nullptr", "operator", "or", "or_eq", "private", "protected", "public", "register", "reinterpret_cast",
            "requires", "return", "short", "signed", "sizeof", "static", "static_assert", "static_cast", "struct",
            "switch", "template", "this", "thread_local", "throw", "true", "try", "typedef", "typeid", "typename",
            "union", "unsigned", "using", "virtual", "void", "volatile", "wchar_t", "while", "xor", "xor_eq"
        });

        public static readonly Language CSharp = new("c#", "csharp", "c#/protocol.txt", ".cs", ".", "@{0}", false, new HashSet<string>
        {
            "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const",
            "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern",
            "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface",
            "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override",
            "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
            "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof",
            "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while"
        });

        public static readonly IReadOnlyDictionary<string, Language> All = new Dictionary<string, Language>
        {
            [Cpp.Name] = Cpp,
            [CSharp.Name] = CSharp
        };

        public string Name { get; }
        public string FlatcOption { get; }
        public string Template { get; }
        public string Extension { get; }
        public string Separator { get; }

        // flatc escapes reserved words in type, field and enum value names (C++ `name_`, C# `@name`) but not in
        // namespaces. A C++ namespace can be renamed in the schema itself, while C#'s `@name` cannot be written in a
        // schema, so the C# schema keeps the name and flatc's output is escaped afterwards.
        public bool EscapesNamespaceInSchema { get; }

        private readonly string _escapeFormat;
        private readonly HashSet<string> _keywords;

        private Language(string name, string flatcOption, string template, string extension, string separator, string escapeFormat, bool escapesNamespaceInSchema, HashSet<string> keywords)
        {
            Name = name;
            FlatcOption = flatcOption;
            Template = template;
            Extension = extension;
            Separator = separator;
            EscapesNamespaceInSchema = escapesNamespaceInSchema;
            _escapeFormat = escapeFormat;
            _keywords = keywords;
        }

        public string Escape(string name)
        {
            return _keywords.Contains(name) ? string.Format(_escapeFormat, name) : name;
        }

        public string SchemaNamespace(string segment)
        {
            return EscapesNamespaceInSchema ? Escape(segment) : segment;
        }
    }
}
