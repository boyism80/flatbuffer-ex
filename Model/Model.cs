namespace FlatBufferEx.Model
{
    public sealed record Namespace(IReadOnlyList<string> Segments)
    {
        public static readonly Namespace Global = new(new List<string>());

        // Namespace of the flatc-generated types that back the generated classes.
        public Namespace Raw => new(Segments.Append("raw").ToList());

        public bool Equals(Namespace other) => other != null && Segments.SequenceEqual(other.Segments);
        public override int GetHashCode() => string.Join('.', Segments).GetHashCode();
    }

    public abstract record FieldType(bool Nullable)
    {
        // Lets templates branch on the type, since they cannot see the record class.
        public abstract string Kind { get; }

        // A nullable scalar has no null in flatbuffers, so it is stored in a wrapper table of the `nullable` namespace.
        public virtual string NullableTableName => null;
        public bool IsNullableScalar => Nullable && NullableTableName != null;
    }

    // byte, ubyte, bool, short, ushort, int, uint, float, long, ulong, double
    public sealed record ScalarType(string Name, bool Nullable) : FieldType(Nullable)
    {
        public override string Kind => "scalar";
        public override string NullableTableName => $"nullable_{Name}".ToLower();
    }

    public sealed record StringType(bool Nullable) : FieldType(Nullable)
    {
        public override string Kind => "string";
    }

    public sealed record ArrayType(FieldType Element) : FieldType(false)
    {
        public override string Kind => "array";
    }

    public sealed record TableType(Table Table, bool Nullable) : FieldType(Nullable)
    {
        public override string Kind => "table";
    }

    public sealed record EnumType(Enum Enum, bool Nullable) : FieldType(Nullable)
    {
        public override string Kind => "enum";
        public override string NullableTableName => ("nullable_" + string.Join('_', Enum.Scope.Namespace.Segments.Append(Enum.Name))).ToLower();
    }

    public class Field
    {
        public string Name { get; set; }
        public FieldType Type { get; set; }
        public string Init { get; set; }

        // Array element types first, then the field type itself.
        public IEnumerable<FieldType> Types
        {
            get
            {
                var types = new List<FieldType>();
                for (var type = Type; type != null; type = (type as ArrayType)?.Element)
                    types.Add(type);

                types.Reverse();
                return types;
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

    // The contents of one schema file.
    public class Scope
    {
        public Namespace Namespace { get; set; }
        public List<Table> Tables { get; set; }
        public List<Enum> Enums { get; set; }
    }

    public class Context
    {
        public List<Scope> Scopes { get; set; }

        public IEnumerable<Table> Tables => Scopes.SelectMany(x => x.Tables);
        public IEnumerable<Enum> Enums => Scopes.SelectMany(x => x.Enums);

        // Every distinct field type in order of first use, including array element types.
        public IEnumerable<FieldType> FieldTypes => Tables.SelectMany(x => x.Fields).SelectMany(x => x.Types).Distinct();
        public IEnumerable<FieldType> NullableScalarTypes => FieldTypes.Where(x => x.IsNullableScalar);
        public IEnumerable<ArrayType> ArrayTypes => FieldTypes.OfType<ArrayType>();
    }
}
