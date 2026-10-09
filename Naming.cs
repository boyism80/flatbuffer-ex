using FlatBufferEx.Model;
using Enum = FlatBufferEx.Model.Enum;

namespace FlatBufferEx
{
    // Names as they are written in one output: generated code of a language, or the raw schema given to flatc
    public sealed class Naming
    {
        private readonly string _separator;
        private readonly Func<string, string> _namespace;
        private readonly Func<string, string> _identifier;

        private Naming(string separator, Func<string, string> ns, Func<string, string> identifier)
        {
            _separator = separator;
            _namespace = ns;
            _identifier = identifier;
        }

        public static Naming Code(Language language) => new(language.Separator, language.Escape, language.Escape);

        // flatc escapes identifiers itself, so the schema only adjusts namespaces
        public static Naming Schema(Language language) => new(".", language.SchemaNamespace, x => x);

        public string Name(object value)
        {
            return value switch
            {
                Table x => _identifier(x.Name),
                Enum x => _identifier(x.Name),
                EnumValue x => _identifier(x.Name),
                Field x => _identifier(x.Name),
                _ => throw new ArgumentException($"no name for {value}")
            };
        }

        public string Namespace(Scope scope) => string.Join(_separator, scope.Namespace.Select(_namespace));
        public string RawNamespace(Scope scope) => string.Join(_separator, scope.RawNamespace.Select(_namespace));

        public string FullName(object value)
        {
            return value switch
            {
                Table x => Qualify(x.Scope.Namespace, x.Name),
                Enum x => Qualify(x.Scope.Namespace, x.Name),
                Field x => x.IsPrimitive ? x.Type : Qualify(x.TypeNamespace, x.Type),
                _ => throw new ArgumentException($"no full name for {value}")
            };
        }

        public string RawName(object value)
        {
            return value switch
            {
                Table x => Qualify(x.Scope.RawNamespace, x.Name),
                Enum x => Qualify(x.Scope.RawNamespace, x.Name),
                Field x => x.IsPrimitive ? x.Type : Qualify(x.RawTypeNamespace, x.Type),
                _ => throw new ArgumentException($"no raw name for {value}")
            };
        }

        private string Qualify(IEnumerable<string> ns, string name)
        {
            return string.Join(_separator, ns.Select(_namespace).Append(_identifier(name)));
        }
    }
}
