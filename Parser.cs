using FlatBufferEx.Model;
using System.ComponentModel.Design.Serialization;
using System.Text.RegularExpressions;
using Enum = FlatBufferEx.Model.Enum;

namespace FlatBufferEx
{
    public static class Parser
    {
        private static readonly Regex FieldRegEx = new Regex(@"\s*(?<name>[_a-zA-Z][_a-zA-Z0-9]*)\s*:\s*(?<type>\[?[_a-zA-Z][_a-zA-Z0-9\.]*\??\]?\??)(?:\s*=\s*(?<init>.+))?\s*(?<deprecated>\(deprecated\))?\s*;");
        private static readonly Regex TableRegEx = new Regex(@"(?<type>struct|table)\s+(?<name>[_a-zA-Z][_a-zA-Z0-9]*)\s*{(?<contents>[\s\S]*?)}");
        private static readonly Regex EnumRegEx = new Regex(@"enum\s+(?<name>[_a-zA-Z][_a-zA-Z0-9]*)\s*:\s*(?<type>[a-zA-Z]+)\s+{\s*(?<contents>.+)\s*}");
        private static readonly Regex NamespaceRegEx = new Regex(@"namespace\s+(?<name>[_a-zA-Z][_a-zA-Z0-9\.]*);");
        private static readonly Regex IncludeRegEx = new Regex(@"include\s*""(?<file>.+)\.fbs""\s*;");

        private static (List<string> Namespace, string Type) SplitNamespace(string value)
        {
            if (value.Contains('.'))
            {
                var splitted = value.Split('.').ToList();
                return (splitted.GetRange(0, splitted.Count - 1).ToList(), splitted.Last());
            }
            else
            {
                return (null, value);
            }
        }

        private static bool RemoveNullableType(ref string type)
        {
            if (type.EndsWith('?'))
            {
                type = type.Substring(0, type.Length - 1);
                return true;
            }
            else
            {
                return false;
            }
        }

        private static bool RemoveArrayType(ref string type)
        {
            if (type.StartsWith('[') && type.EndsWith(']'))
            {
                type = type.Substring(1, type.Length - 2);
                return true;
            }
            else
            {
                return false;
            }
        }

        private static Field GetField(Context context, Scope scope, Table table, string type)
        {
            var field = new Field
            {
                Context = context,
                Scope = scope,
                Table = table,
            };

            field.IsNullable = RemoveNullableType(ref type);
            if (RemoveArrayType(ref type))
            {
                field.Type = "array";
                field.ArrayElement = GetField(context, scope, table, type);
            }
            else
            {
                (field.ReferNamespace, field.Type) = SplitNamespace(type);
            }

            return field;
        }

        private static IEnumerable<Field> GetFields(Context context, Scope scope, Table table, string contents)
        {
            foreach (Match match in FieldRegEx.Matches(contents))
            {
                if (match.Groups["deprecated"].Success)
                    continue;

                var field = GetField(context, scope, table, match.Groups["type"].Value);
                field.Name = match.Groups["name"].Value;
                field.Init = match.Groups["init"].Success ? match.Groups["init"].Value : null;
                yield return field;
            }
        }

        private static IEnumerable<Enum> GetEnums(Context context, Scope scope, string contents)
        {
            foreach (Match match in EnumRegEx.Matches(contents))
            {
                yield return new Enum
                {
                    Context = context,
                    Scope = scope,
                    Type = match.Groups["type"].Value,
                    Name = match.Groups["name"].Value,
                    Values = match.Groups["contents"].Value.Split(',').Select(x => x.Split('=')[0].Trim()).ToList(),
                };
            }
        }

        private static IEnumerable<Table> GetTables(Context context, Scope scope, string contents)
        {
            foreach (Match match in TableRegEx.Matches(contents))
            {
                var table = new Table
                {
                    Context = context,
                    Scope = scope,
                    Type = match.Groups["type"].Value,
                    Name = match.Groups["name"].Value,
                };

                table.Fields = GetFields(context, scope, table, match.Groups["contents"].Value).ToList();
                yield return table;
            }
        }

        private static IEnumerable<string> GetIncludeFiles(string contents)
        {
            foreach (Match match in IncludeRegEx.Matches(contents))
            {
                yield return match.Groups["file"].Value;
            }
        }

        private static List<string> GetNamespace(string contents)
        {
            var matched = NamespaceRegEx.Match(contents);
            if (matched.Success == false)
                return new List<string>();

            return matched.Groups["name"].Value.Split('.').ToList();
        }

        public static Scope GetScope(Context context, string file)
        {
            var contents = File.ReadAllText(file);

            var scope = new Scope
            {
                Context = context,
                FileName = Path.GetFileNameWithoutExtension(file),
                IncludeFiles = GetIncludeFiles(contents).ToList(),
                Namespace = GetNamespace(contents),
            };
            scope.Tables = GetTables(context, scope, contents).ToList();
            scope.Enums = GetEnums(context, scope, contents).ToList();
            return scope;
        }

        public static Context Parse(string path, string wildcard)
        {
            var context =  new Context
            {
                Scopes = new List<Model.Scope>()
            };

            foreach (var file in Directory.GetFiles(path, wildcard))
            {
                context.Scopes.Add(GetScope(context, file));
            }

            return context;
        }

        //public static IEnumerable<string> CreateOriginTempFiles(string path, string to, string wildcard, string lang)
        //{
        //    if (Directory.Exists(to) == false)
        //        Directory.CreateDirectory(to);

        //    foreach (var file in Directory.GetFiles(to, wildcard))
        //    {
        //        File.Delete(file);
        //    }

        //    foreach (var file in Directory.GetFiles(path, wildcard))
        //    {
        //        var contents = File.ReadAllText(file);
        //        var matchNamespace = NamespaceRegEx.Match(contents);
        //        contents = contents.Remove(matchNamespace.Groups["name"].Index, matchNamespace.Groups["name"].Value.Length).Insert(matchNamespace.Groups["name"].Index, string.Join('.', matchNamespace.Groups["name"].Value.Split('.').Select(x => 
        //        {
        //            switch (lang)
        //            {
        //                case "c#":
        //                    return ScribanEx.CsReplaceReservedKeyword(x);

        //                default:
        //                    return x;
        //            }
        //        }).Concat(new[] { "origin" })));

        //        var matchFields = FieldRegEx.Matches(contents);
        //        foreach (var match in matchFields.Cast<Match>().Reverse())
        //        {
        //            var values = match.Groups["type"].Value.Split('.').ToList();
        //            if (values.Count == 1)
        //                continue;

        //            switch (lang)
        //            {
        //                case "c#":
        //                    values = values.Select(x => ScribanEx.CsReplaceReservedKeyword(x)).ToList();
        //                    break;
        //            }

        //            values.Insert(values.Count - 1, "origin");
        //            contents = contents.Remove(match.Groups["type"].Index, match.Groups["type"].Value.Length).Insert(match.Groups["type"].Index, string.Join('.', values));
        //        }

        //        var fileName = Path.GetFileName(file);
        //        File.WriteAllText(Path.Join(to, Path.GetFileName(file)), contents);
        //        yield return fileName;
        //    }
        //}

        //public static IEnumerable<FlatBufferFileInfo> Parse(string path, string wildcard)
        //{
        //    foreach (var file in Directory.GetFiles(path, wildcard))
        //    {
        //        yield return Parse(file);
        //    }
        //}
    }
}
