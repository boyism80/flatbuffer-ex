using FlatBufferEx.Model;
using Scriban.Runtime;

namespace FlatBufferEx
{
    public sealed class CppGenerator : Generator
    {
        private readonly string _includePath;

        public CppGenerator(Context context, string includePath) : base(context, Naming.Code(Language.Cpp))
        {
            _includePath = includePath;
        }

        protected override string Template => "cpp/protocol.txt";

        protected override string Scalar(string name) => name switch
        {
            "byte" => "int8_t",
            "ubyte" => "uint8_t",
            "short" => "int16_t",
            "ushort" => "uint16_t",
            "int" => "int32_t",
            "uint" => "uint32_t",
            "long" => "int64_t",
            "ulong" => "uint64_t",
            _ => name,
        };

        protected override object Field(Field field)
        {
            var name = Naming.Identifier(field.Name);
            var type = field.Type;
            return new
            {
                Name = name,
                Type = Type(type),
                Parameter = Parameter(type),
                Initializer = Initializer(field),
                MemberInitializer = type is StringType { Nullable: false } ? $"std::string({name})" : name,
                Read = Read($"raw.{name}()", type),
            };
        }

        protected override void Globals(ScriptObject globals)
        {
            globals["include_path"] = _includePath;
            globals["nullables"] = Context.NullableScalarTypes.Select(x => new
            {
                Table = x.NullableTableName,
                Type = Type(x),
                Value = x is EnumType e ? $"static_cast<{RawName(e.Enum)}>(value.value())" : "value.value()",
            }).ToList();

            // A class must follow the classes it holds by value, in std::optional or in std::vector, but the schema may
            // declare tables in any order. Classes are emitted in dependency order, reopening a namespace when needed.
            var order = new List<(Scope Scope, Table Table)>();
            var visited = new HashSet<Table>();
            void Visit(Table table)
            {
                if (visited.Add(table) == false)
                    return;

                foreach (var dependency in table.Fields.SelectMany(x => x.Types).OfType<TableType>())
                    Visit(dependency.Table);
                order.Add((table.Scope, table));
            }

            foreach (var scope in Context.Scopes)
            {
                if (scope.Tables.Count == 0)
                    order.Add((scope, null));
                foreach (var table in scope.Tables)
                    Visit(table);
            }

            var blocks = new List<object>();
            var opened = new HashSet<Scope>();
            for (var i = 0; i < order.Count;)
            {
                var scope = order[i].Scope;
                var tables = new List<TableModel>();
                for (; i < order.Count && order[i].Scope == scope; i++)
                {
                    if (order[i].Table != null)
                        tables.Add(Tables[order[i].Table]);
                }

                blocks.Add(new
                {
                    Namespace = Naming.Namespace(scope.Namespace),
                    First = opened.Add(scope),
                    ProtocolTypes = scope.Tables.Select(x => Naming.Identifier(x.Name)).ToList(),
                    Tables = tables,
                });
            }
            globals["class_blocks"] = blocks;
        }

        private string Type(FieldType type)
        {
            var result = type switch
            {
                ArrayType array => $"std::vector<{Type(array.Element)}>",
                TableType table => FullName(table.Table),
                EnumType e => FullName(e.Enum),
                StringType => "std::string",
                ScalarType scalar => Scalar(scalar.Name),
                _ => throw new ArgumentException($"no type for {type}"),
            };

            return type.Nullable ? $"std::optional<{result}>" : result;
        }

        private string Parameter(FieldType type)
        {
            if (type.Nullable || type is TableType)
                return $"const {Type(type)}&";
            else if (type is StringType)
                return "std::string_view";
            else
                return Type(type);
        }

        private string Read(string accessor, FieldType type)
        {
            var value = type.IsNullableScalar ? $"{accessor}->value()" : accessor;
            value = type switch
            {
                StringType => $"flatbuffers::option::decode({value}->c_str())",
                EnumType e => $"({FullName(e.Enum)}){value}",
                TableType => $"*{accessor}",
                ArrayType array => $"unpack<{Type(array.Element)}>({accessor})",
                _ => value,
            };

            return type.Nullable ? $"{accessor} != nullptr ? {value} : {Type(type)}()" : value;
        }

        private static string Initializer(Field field)
        {
            var type = field.Type;
            var result = field.Init;
            if (result == null && type.Nullable)
                result = "std::nullopt";
            else if (result == null && type is ArrayType)
                result = "{}";
            else if (result == null && type is ScalarType scalar)
                result = scalar.Name switch
                {
                    "long" => "0LL",
                    "ulong" => "0ULL",
                    "float" => "0.0f",
                    "double" => "0.0",
                    "bool" => "false",
                    _ => "0",
                };

            return result == null ? string.Empty : $" = {result}";
        }
    }
}
