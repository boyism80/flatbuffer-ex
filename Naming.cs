using FlatBufferEx.Model;

namespace FlatBufferEx
{
    // Names as they are written in one output: generated code of a language, or the raw schema given to flatc
    public sealed class Naming
    {
        private readonly string _separator;
        private readonly Func<string, string> _segment;
        private readonly Func<string, string> _identifier;

        private Naming(string separator, Func<string, string> segment, Func<string, string> identifier)
        {
            _separator = separator;
            _segment = segment;
            _identifier = identifier;
        }

        public static Naming Code(Language language) => new(language.Separator, language.Escape, language.Escape);

        // flatc escapes identifiers itself, so the schema only adjusts namespaces
        public static Naming Schema(Language language) => new(".", language.SchemaNamespace, x => x);

        public string Identifier(string name) => _identifier(name);

        public string Namespace(Namespace ns) => string.Join(_separator, ns.Segments.Select(_segment));

        public string Qualify(Namespace ns, string name) => string.Join(_separator, ns.Segments.Select(_segment).Append(_identifier(name)));
    }
}
