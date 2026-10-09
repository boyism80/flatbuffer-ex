using FlatBufferEx.Model;
using System.Text.RegularExpressions;
using Enum = FlatBufferEx.Model.Enum;

namespace FlatBufferEx
{
    public static class Parser
    {
        private static readonly Regex FieldRegex = new(@"\s*(?<name>[_a-zA-Z][_a-zA-Z0-9]*)\s*:\s*(?<type>\[*[_a-zA-Z][_a-zA-Z0-9\.]*\??\]*\??)(?:\s*=\s*(?<init>.+?))?\s*(?<deprecated>\(deprecated\))?\s*;");
        private static readonly Regex TableRegex = new(@"(?<type>struct|table)\s+(?<name>[_a-zA-Z][_a-zA-Z0-9]*)\s*{(?<contents>[\s\S]*?)}");
        private static readonly Regex EnumRegex = new(@"enum\s+(?<name>[_a-zA-Z][_a-zA-Z0-9]*)\s*:\s*(?<type>[a-zA-Z]+)\s+{\s*(?<contents>[\s\S]*?)}");
        private static readonly Regex NamespaceRegex = new(@"namespace\s+(?<name>[_a-zA-Z][_a-zA-Z0-9\.]*);");
        private static readonly Regex CommentRegex = new(@"//[^\n]*|/\*[\s\S]*?\*/");
        private static readonly HashSet<string> Scalars = new() { "byte", "ubyte", "bool", "short", "ushort", "int", "uint", "float", "long", "ulong", "double" };

        public static Context Parse(string path)
        {
            var context = new Context { Scopes = new List<Scope>() };

            // Field types may refer to tables and enums of later files, so they are resolved after every file is read.
            var fieldTypes = new List<(Field Field, Scope Scope, string Type)>();
            foreach (var file in Directory.GetFiles(path, "*.fbs").OrderBy(x => x, StringComparer.Ordinal))
            {
                var contents = CommentRegex.Replace(File.ReadAllText(file), string.Empty);
                var declaration = NamespaceRegex.Match(contents);
                var ns = declaration.Success ? new Namespace(declaration.Groups["name"].Value.Split('.')) : Namespace.Global;

                // Files of the same namespace share one scope, so its generated types are declared once.
                var scope = context.Scopes.FirstOrDefault(x => x.Namespace == ns);
                if (scope == null)
                {
                    scope = new Scope { Namespace = ns, Tables = new List<Table>(), Enums = new List<Enum>() };
                    context.Scopes.Add(scope);
                }

                scope.Tables.AddRange(TableRegex.Matches(contents).Select(match => new Table
                {
                    Scope = scope,
                    Name = match.Groups["name"].Value,
                    Fields = FieldRegex.Matches(match.Groups["contents"].Value)
                        .Where(x => !x.Groups["deprecated"].Success)
                        .Select(x =>
                        {
                            var field = new Field
                            {
                                Name = x.Groups["name"].Value,
                                Init = x.Groups["init"].Success ? x.Groups["init"].Value : null,
                            };
                            fieldTypes.Add((field, scope, x.Groups["type"].Value));
                            return field;
                        })
                        .ToList(),
                }));

                scope.Enums.AddRange(EnumRegex.Matches(contents).Select(match => new Enum
                {
                    Scope = scope,
                    Name = match.Groups["name"].Value,
                    Type = match.Groups["type"].Value,
                    Values = match.Groups["contents"].Value
                        .Split(',')
                        .Where(x => !string.IsNullOrWhiteSpace(x))
                        .Select(x => x.Split('='))
                        .Select(x => new EnumValue { Name = x[0].Trim(), Value = x.Length > 1 ? x[1].Trim() : null })
                        .ToList(),
                }));
            }

            foreach (var (field, scope, type) in fieldTypes)
                field.Type = ParseType(context, scope, type);

            return context;
        }

        private static FieldType ParseType(Context context, Scope scope, string type)
        {
            var nullable = type.EndsWith('?');
            if (nullable)
                type = type[..^1];

            if (type.StartsWith('[') && type.EndsWith(']'))
            {
                if (nullable)
                    throw new FormatException($"array cannot be a nullable type: {type}?");

                // A flatbuffers vector cannot hold null.
                var element = ParseType(context, scope, type[1..^1]);
                if (element.Nullable)
                    throw new FormatException($"array element cannot be a nullable type: {type}");

                return new ArrayType(element);
            }

            if (type == "string")
                return new StringType(nullable);

            if (Scalars.Contains(type))
                return new ScalarType(type, nullable);

            // A type without a namespace refers to the namespace of its own schema file.
            var parts = type.Split('.');
            var ns = parts.Length > 1 ? new Namespace(parts[..^1]) : scope.Namespace;
            var name = parts[^1];

            var table = context.Tables.FirstOrDefault(x => x.Scope.Namespace == ns && x.Name == name);
            if (table != null)
                return new TableType(table, nullable);

            var e = context.Enums.FirstOrDefault(x => x.Scope.Namespace == ns && x.Name == name);
            if (e != null)
                return new EnumType(e, nullable);

            throw new FormatException($"undefined type: {type}");
        }
    }
}
