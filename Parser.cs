using FlatBufferEx.Model;
using System.Text.RegularExpressions;
using Enum = FlatBufferEx.Model.Enum;

namespace FlatBufferEx
{
    /// <summary>
    /// Static parser class for FlatBuffer schema files
    /// Parses .fbs files and extracts tables, enums, fields, and other schema elements
    /// </summary>
    public static class Parser
    {
        // Regular expressions for parsing different FlatBuffer schema elements
        private static readonly Regex FieldRegEx = new Regex(@"\s*(?<name>[_a-zA-Z][_a-zA-Z0-9]*)\s*:\s*(?<type>\[*[_a-zA-Z][_a-zA-Z0-9\.]*\??\]*\??)(?:\s*=\s*(?<init>.+))?\s*(?<deprecated>\(deprecated\))?\s*;");
        private static readonly Regex TableRegEx = new Regex(@"(?<type>struct|table)\s+(?<name>[_a-zA-Z][_a-zA-Z0-9]*)\s*{(?<contents>[\s\S]*?)}");
        private static readonly Regex EnumRegEx = new Regex(@"enum\s+(?<name>[_a-zA-Z][_a-zA-Z0-9]*)\s*:\s*(?<type>[a-zA-Z]+)\s+{\s*(?<contents>[\s\S]*?)}");
        private static readonly Regex NamespaceRegEx = new Regex(@"namespace\s+(?<name>[_a-zA-Z][_a-zA-Z0-9\.]*);");
        private static readonly Regex IncludeRegEx = new Regex(@"include\s*""(?<file>.+)\.fbs""\s*;");

        /// <summary>
        /// Splits a type string into namespace and type components
        /// </summary>
        /// <param name="value">Type string to split</param>
        /// <returns>Tuple containing namespace list and type name</returns>
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

        /// <summary>
        /// Removes nullable type indicator (?) from type string
        /// </summary>
        /// <param name="type">Type string to modify</param>
        /// <returns>True if nullable indicator was removed</returns>
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

        /// <summary>
        /// Removes array type indicators ([]) from type string
        /// </summary>
        /// <param name="type">Type string to modify</param>
        /// <returns>True if array indicators were removed</returns>
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

        /// <summary>
        /// Creates a Field object from a type string
        /// Handles nullable types, array types, and nested field structures
        /// </summary>
        /// <param name="context">Current parsing context</param>
        /// <param name="scope">Current scope</param>
        /// <param name="table">Parent table</param>
        /// <param name="type">Type string to parse</param>
        /// <returns>Parsed Field object</returns>
        private static Field GetField(Context context, Scope scope, Table table, string type)
        {
            var field = new Field
            {
                Context = context,
                Scope = scope,
                Table = table,
            };

            // Check for nullable type
            field.IsNullable = RemoveNullableType(ref type);
            
            // Check for array type
            if (RemoveArrayType(ref type))
            {
                if (field.IsNullable)
                    throw new Exception("array cannot be null type");
                field.Type = "array";
                field.ArrayElement = GetField(context, scope, table, type);
            }
            else
            {
                // Split namespace and type
                (field.ReferNamespace, field.Type) = SplitNamespace(type);
            }

            return field;
        }

        /// <summary>
        /// Parses field definitions from table contents
        /// </summary>
        /// <param name="context">Current parsing context</param>
        /// <param name="scope">Current scope</param>
        /// <param name="table">Parent table</param>
        /// <param name="contents">Table contents to parse</param>
        /// <returns>Collection of parsed fields</returns>
        private static IEnumerable<Field> GetFields(Context context, Scope scope, Table table, string contents)
        {
            foreach (Match match in FieldRegEx.Matches(contents))
            {
                // Skip deprecated fields
                if (match.Groups["deprecated"].Success)
                    continue;

                var field = GetField(context, scope, table, match.Groups["type"].Value);
                field.Name = match.Groups["name"].Value;
                field.Init = match.Groups["init"].Success ? match.Groups["init"].Value : null;
                yield return field;
            }
        }

        /// <summary>
        /// Parses enum definitions from scope contents
        /// </summary>
        /// <param name="context">Current parsing context</param>
        /// <param name="scope">Current scope</param>
        /// <param name="contents">Contents to parse</param>
        /// <returns>Collection of parsed enums</returns>
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

        /// <summary>
        /// Parses table definitions from scope contents
        /// </summary>
        /// <param name="context">Current parsing context</param>
        /// <param name="scope">Current scope</param>
        /// <param name="contents">Contents to parse</param>
        /// <returns>Collection of parsed tables</returns>
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

        /// <summary>
        /// Extracts include file names from file contents
        /// </summary>
        /// <param name="contents">File contents to parse</param>
        /// <returns>Collection of include file names</returns>
        private static IEnumerable<string> GetIncludeFiles(string contents)
        {
            foreach (Match match in IncludeRegEx.Matches(contents))
            {
                yield return match.Groups["file"].Value;
            }
        }

        /// <summary>
        /// Extracts namespace from file contents
        /// </summary>
        /// <param name="contents">File contents to parse</param>
        /// <returns>List of namespace components</returns>
        private static List<string> GetNamespace(string contents)
        {
            var matched = NamespaceRegEx.Match(contents);
            if (matched.Success == false)
                return new List<string>();

            return matched.Groups["name"].Value.Split('.').ToList();
        }

        /// <summary>
        /// Parses a single FlatBuffer schema file and creates a Scope object
        /// </summary>
        /// <param name="context">Current parsing context</param>
        /// <param name="file">Path to the .fbs file</param>
        /// <returns>Parsed Scope object</returns>
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

        /// <summary>
        /// Parses all FlatBuffer schema files in a directory
        /// </summary>
        /// <param name="path">Directory path containing .fbs files</param>
        /// <param name="wildcard">File pattern to match (e.g., "*.fbs")</param>
        /// <returns>Complete parsing context with all scopes</returns>
        public static Context Parse(string path, string wildcard)
        {
            var context = new Context
            {
                Scopes = new List<Model.Scope>()
            };

            // Parse each file in the directory
            foreach (var file in Directory.GetFiles(path, wildcard))
            {
                context.Scopes.Add(GetScope(context, file));
            }

            return context;
        }

        // Commented out legacy code for creating origin temp files
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
