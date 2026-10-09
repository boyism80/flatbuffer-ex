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

        public static Context Parse(string path)
        {
            var context = new Context { Scopes = new List<Scope>() };

            foreach (var file in Directory.GetFiles(path, "*.fbs").OrderBy(x => x, StringComparer.Ordinal))
            {
                var contents = CommentRegex.Replace(File.ReadAllText(file), string.Empty);
                var ns = NamespaceRegex.Match(contents);
                var scope = new Scope
                {
                    Namespace = ns.Success ? ns.Groups["name"].Value.Split('.').ToList() : new List<string>(),
                };

                scope.Tables = TableRegex.Matches(contents).Select(match => new Table
                {
                    Scope = scope,
                    Name = match.Groups["name"].Value,
                    Fields = FieldRegex.Matches(match.Groups["contents"].Value)
                        .Where(x => !x.Groups["deprecated"].Success)
                        .Select(x =>
                        {
                            var field = ParseType(context, scope, x.Groups["type"].Value);
                            field.Name = x.Groups["name"].Value;
                            field.Init = x.Groups["init"].Success ? x.Groups["init"].Value : null;
                            return field;
                        })
                        .ToList(),
                }).ToList();

                scope.Enums = EnumRegex.Matches(contents).Select(match => new Enum
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
                }).ToList();

                context.Scopes.Add(scope);
            }

            return context;
        }

        private static Field ParseType(Context context, Scope scope, string type)
        {
            var field = new Field { Context = context, Scope = scope };

            if (type.EndsWith('?'))
            {
                field.IsNullable = true;
                type = type[..^1];
            }

            if (type.StartsWith('[') && type.EndsWith(']'))
            {
                if (field.IsNullable)
                    throw new FormatException($"array cannot be a nullable type: {type}?");

                field.Type = "array";
                field.ArrayElement = ParseType(context, scope, type[1..^1]);
            }
            else if (type.Contains('.'))
            {
                var parts = type.Split('.');
                field.ExplicitNamespace = parts[..^1].ToList();
                field.Type = parts[^1];
            }
            else
            {
                field.Type = type;
            }

            return field;
        }
    }
}
