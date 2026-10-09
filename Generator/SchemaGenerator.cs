using FlatBufferEx.Model;
using Scriban.Runtime;

namespace FlatBufferEx
{
    // The flatc input schema. Nullable scalars become wrapper tables of the `nullable` namespace.
    public sealed class SchemaGenerator : Generator
    {
        public SchemaGenerator(Context context, Language language) : base(context, Naming.Schema(language))
        { }

        protected override string Template => "raw.txt";

        protected override string Scalar(string name) => name;

        protected override object Field(Field field) => new { Name = field.Name, Type = Type(field.Type) };

        protected override void Globals(ScriptObject globals)
        {
            globals["nullables"] = Context.NullableScalarTypes.Select(x => new { Table = x.NullableTableName, Type = ValueType(x) }).ToList();
        }

        private string Type(FieldType type) => type switch
        {
            ArrayType array => $"[{Type(array.Element)}]",
            _ when type.IsNullableScalar => $"nullable.{type.NullableTableName}",
            _ => ValueType(type),
        };

        private string ValueType(FieldType type) => type switch
        {
            ScalarType scalar => scalar.Name,
            StringType => "string",
            TableType table => RawName(table.Table),
            EnumType e => RawName(e.Enum),
            _ => throw new ArgumentException($"no value type for {type}"),
        };
    }
}
