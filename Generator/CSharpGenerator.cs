using FlatBufferEx.Model;
using Scriban.Runtime;

namespace FlatBufferEx
{
    public sealed class CSharpGenerator : Generator
    {
        public CSharpGenerator(Context context) : base(context, Naming.Code(Language.CSharp))
        { }

        protected override string Template => "c#/protocol.txt";

        protected override string Scalar(string name) => name switch
        {
            "byte" => "sbyte",
            "ubyte" => "byte",
            _ => name,
        };

        protected override object Field(Field field)
        {
            var name = UpperCamel(field.Name);
            return new
            {
                Name = name,
                Type = Type(field.Type),
                Initializer = Initializer(field),
                Read = Read($"raw.{name}", field.Type),
            };
        }

        protected override void Globals(ScriptObject globals)
        {
            globals["nullables"] = Context.NullableScalarTypes.Select(x => new
            {
                Table = x.NullableTableName,
                Type = Type(x),
                Value = x is EnumType e ? $"({RawName(e.Enum)})value.Value" : "value.Value",
            }).ToList();
            globals["arrays"] = Context.ArrayTypes.Select(x => x.Element).Select(x => new
            {
                x.Kind,
                Type = Type(x),
                Size = x switch
                {
                    ScalarType scalar => Size(scalar.Name),
                    EnumType enumType => Size(enumType.Enum.Type),
                    _ => 0,
                },
                Underlying = x is EnumType e ? Scalar(e.Enum.Type) : null,
                RawName = x is TableType table ? RawName(table.Table) : null,
            }).ToList();
        }

        private static string UpperCamel(string value)
        {
            return string.Concat(value.ToLower().Split('_', StringSplitOptions.RemoveEmptyEntries).Select(x => char.ToUpperInvariant(x[0]) + x.Substring(1)));
        }

        private string Type(FieldType type)
        {
            var result = type switch
            {
                ArrayType array => $"List<{Type(array.Element)}>",
                TableType table => FullName(table.Table),
                EnumType e => FullName(e.Enum),
                StringType => "string",
                ScalarType scalar => Scalar(scalar.Name),
                _ => throw new ArgumentException($"no type for {type}"),
            };

            return type.IsNullableScalar ? $"{result}?" : result;
        }

        private string Read(string accessor, FieldType type)
        {
            if (type is ArrayType array)
                return $"Enumerable.Range(0, {accessor}Length).Select(i => {accessor}(i)).Select(x => {Read("x", array.Element)}).ToList()";

            var value = type switch
            {
                TableType => $"new {Type(type)}({accessor}.Value)",
                _ when type.IsNullableScalar => $"({Type(type)}){accessor}.Value.Value",
                EnumType => $"({Type(type)}){accessor}",
                _ => accessor,
            };

            return type.Nullable && type is not StringType ? $"{accessor} != null ? {value} : null" : value;
        }

        private string Initializer(Field field)
        {
            var type = field.Type;
            var result = field.Init;
            if (result == null && type.Nullable)
                result = "null";
            else if (result == null && type is TableType or ArrayType)
                result = $"new {Type(type)}()";
            else if (result == null && type is StringType)
                result = "string.Empty";
            else if (result == null && type is ScalarType scalar)
                result = scalar.Name switch
                {
                    "long" or "ulong" => "0L",
                    "float" => "0.0f",
                    "double" => "0.0",
                    "bool" => "false",
                    _ => "0",
                };

            return result == null ? string.Empty : $" = {result};";
        }

        private static int Size(string scalar)
        {
            return scalar switch
            {
                "byte" or "ubyte" or "bool" => 1,
                "short" or "ushort" => 2,
                "long" or "ulong" or "double" => 8,
                _ => 4,
            };
        }
    }
}
