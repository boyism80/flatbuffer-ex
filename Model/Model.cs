namespace FlatBufferEx.Model
{
    public class Field
    {
        private static readonly HashSet<string> PrimitiveTypes = new()
        {
            "byte", "ubyte", "bool", "short", "ushort", "int", "uint", "float", "long", "ulong", "double", "string"
        };

        public Context Context { get; set; }
        public Scope Scope { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Init { get; set; }
        public Field ArrayElement { get; set; }
        public bool IsNullable { get; set; }

        // Namespace written in the schema type, e.g. `fb.protocol.internal` of `fb.protocol.internal.Item`.
        // Null when the type is referenced without a namespace.
        public List<string> ExplicitNamespace { get; set; }

        public bool IsArray => ArrayElement != null;
        public bool IsPrimitive => PrimitiveTypes.Contains(Type);
        public bool IsTable => !IsPrimitive && !IsArray && Context.ScopesOf(this).Any(x => x.Tables.Any(t => t.Name == Type));
        public bool IsEnum => !IsPrimitive && !IsArray && Context.ScopesOf(this).Any(x => x.Enums.Any(e => e.Name == Type));

        // A nullable scalar has no null in flatbuffers, so it is stored in a wrapper table of the `nullable` namespace.
        public bool IsNullableScalar => IsNullable && !IsTable && Type != "string";
        public string NullableTableName => "nullable_" + string.Join('_', TypeNamespace.Append(Type)).ToLower();

        public List<string> TypeNamespace
        {
            get
            {
                if (IsPrimitive)
                    return new List<string>();

                return ExplicitNamespace ?? Scope.Namespace;
            }
        }

        public List<string> RawTypeNamespace
        {
            get
            {
                if (IsPrimitive)
                    return new List<string>();

                return TypeNamespace.Append("raw").ToList();
            }
        }

        public string TypeKey
        {
            get
            {
                var key = IsArray ? $"[{ArrayElement.TypeKey}]" : string.Join('.', TypeNamespace.Append(Type));
                return IsNullable ? $"{key}?" : key;
            }
        }

        public IEnumerable<Field> SelfAndElements
        {
            get
            {
                if (IsArray)
                {
                    foreach (var field in ArrayElement.SelfAndElements)
                        yield return field;
                }

                yield return this;
            }
        }
    }

    public class Table
    {
        public Scope Scope { get; set; }
        public string Name { get; set; }
        public List<Field> Fields { get; set; }
    }

    public class EnumValue
    {
        public string Name { get; set; }

        // Value written after `=` in the schema, null when the value follows the previous one.
        public string Value { get; set; }
    }

    public class Enum
    {
        public Scope Scope { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public List<EnumValue> Values { get; set; }
    }

    public class Scope
    {
        public List<string> Namespace { get; set; }
        public List<Table> Tables { get; set; }
        public List<Enum> Enums { get; set; }

        public List<string> RawNamespace => Namespace.Append("raw").ToList();
    }

    public class Context
    {
        public List<Scope> Scopes { get; set; }

        // One field per distinct type, including array element types
        public IEnumerable<Field> FieldTypes => Scopes
            .SelectMany(x => x.Tables)
            .SelectMany(x => x.Fields)
            .SelectMany(x => x.SelfAndElements)
            .GroupBy(x => x.TypeKey)
            .Select(x => x.First());

        public IEnumerable<Field> NullableScalarTypes => FieldTypes.Where(x => x.IsNullableScalar);
        public IEnumerable<Field> ArrayTypes => FieldTypes.Where(x => x.IsArray);

        public IEnumerable<Scope> ScopesOf(Field field)
        {
            var ns = field.ExplicitNamespace ?? field.Scope.Namespace;
            return Scopes.Where(x => x.Namespace.SequenceEqual(ns));
        }
    }
}
