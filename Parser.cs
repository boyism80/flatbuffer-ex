using FlatBufferEx.Model;
using System;
using System.Text.RegularExpressions;

namespace FlatBufferEx
{
    public static class Parser
    {
        private static readonly Regex FieldRegEx = new Regex(@"\s*(?<name>[_a-zA-Z][_a-zA-Z0-9]*)\s*:\s*(?<type>(?:\[?)[_a-zA-Z][_a-zA-Z0-9\.]*\]?)(?:\s*=\s*(?<init>.+?)\s*(?<deprecated>\(deprecated\))?)?;");
        private static readonly Regex TableRegEx = new Regex(@"(?<type>struct|table)\s+(?<name>[_a-zA-Z][_a-zA-Z0-9]*)\s*{(?<contents>[\s\S]*?)}");
        private static readonly Regex ArrayRegEx = new Regex(@"\[(?<type>[a-zA-Z0-9]+)\]");
        private static readonly Regex EnumRegEx = new Regex(@"enum\s+(?<name>[_a-zA-Z][_a-zA-Z0-9]*)\s*:\s*(?<type>[a-zA-Z]+)\s+{\s*(?<contents>.+)\s*}");
        private static readonly Regex NamespaceRegEx = new Regex(@"namespace\s+(?<name>[_a-zA-Z][_a-zA-Z0-9\.]*);");
        private static readonly Regex IncludeRegEx = new Regex(@"include\s*""(?<file>.+)\.fbs""\s*;");
        private static readonly Regex RootTypeRegEx = new Regex(@"root_type\s+(?<name>[_a-zA-Z][_a-zA-Z0-9\.]*);");

        private static IEnumerable<Field> ParseFields(string contents)
        {
            foreach (var match in FieldRegEx.Matches(contents).Cast<Match>())
            {
                if (match.Groups["deprecated"].Success)
                    continue;

                var type = match.Groups["type"].Value;
                var ns = null as List<string>;
                if (type.Contains('.'))
                {
                    var splitted = type.Split('.').ToList();
                    ns = splitted.GetRange(0, splitted.Count - 1).ToList();
                    type = splitted.Last();
                }
                var matchArray = ArrayRegEx.Match(type);

                yield return new Field
                { 
                    Name = match.Groups["name"].Value,
                    Type = matchArray.Success ? "array" : type,
                    Init = match.Groups["init"].Value,
                    Namespace = ns,
                    ArrayElement = matchArray.Success ? new Field
                    { 
                        Name = null,
                        Type = matchArray.Groups["type"].Value,
                        ArrayElement = null,
                        Init = null,
                        Deprecated = false
                    } : null,
                    Deprecated = !string.IsNullOrEmpty(match.Groups["deprecated"].Value)
                };
            }
        }

        private static IEnumerable<Table> ParseTable(string src)
        {
            foreach(var match in TableRegEx.Matches(src).Cast<Match>())
            {
                var table = new Table
                {
                    Type = match.Groups["type"].Value,
                    Name = match.Groups["name"].Value,
                    Fields = new List<Field>(),
                    Root = RootTypeRegEx.IsMatch(src)
                };

                foreach (var field in ParseFields(match.Groups["contents"].Value))
                {
                    table.Fields.Add(field);
                }

                yield return table;
            }
        }

        private static IEnumerable<Model.Enum> ParseEnum(string src)
        {
            foreach (var match in EnumRegEx.Matches(src).Cast<Match>())
            {
                var @enum = new Model.Enum
                {
                    Type = match.Groups["type"].Value,
                    Name = match.Groups["name"].Value,
                    Values = match.Groups["contents"].Value.Split(',').Select(x => x.Split('=')[0].Trim()).ToList()
                };

                yield return @enum;
            }
        }

        private static IEnumerable<string> ParseNamespace(string src)
        {
            var match = NamespaceRegEx.Match(src);
            if (match.Success)
            {
                var name = match.Groups["name"].Value;
                foreach (var x in name.Split("."))
                    yield return x;
            }
        }

        private static IEnumerable<string> ParseInclude(string src)
        {
            foreach (var match in IncludeRegEx.Matches(src).Cast<Match>())
            {
                yield return match.Groups["file"].Value;
            }
        }

        private static string ParseRootType(string src)
        {
            var match = RootTypeRegEx.Match(src);
            if (match.Success)
                return match.Groups["name"].Value;
            else
                return null;
        }

        public static FlatBufferFileInfo Parse(string path)
        {
            var src = File.ReadAllText(path);
            return new FlatBufferFileInfo
            { 
                File = Path.GetFileNameWithoutExtension(path),
                RootType = ParseRootType(src),
                Namespace = ParseNamespace(src).ToList(),
                Includes = ParseInclude(src).ToList(),
                Tables = ParseTable(src).ToList(),
                Enums = ParseEnum(src).ToList()
            };
        }

        public static IEnumerable<string> CreateOriginTempFiles(string path, string to, string wildcard, string lang)
        {
            if (Directory.Exists(to) == false)
                Directory.CreateDirectory(to);

            foreach (var file in Directory.GetFiles(to, wildcard))
            {
                File.Delete(file);
            }

            foreach (var file in Directory.GetFiles(path, wildcard))
            {
                var contents = File.ReadAllText(file);
                var matchNamespace = NamespaceRegEx.Match(contents);
                contents = contents.Remove(matchNamespace.Groups["name"].Index, matchNamespace.Groups["name"].Value.Length).Insert(matchNamespace.Groups["name"].Index, string.Join('.', matchNamespace.Groups["name"].Value.Split('.').Select(x => 
                {
                    switch (lang)
                    {
                        case "c#":
                            return ScribanEx.CsReplaceReservedKeyword(x);

                        default:
                            return x;
                    }
                }).Concat(new[] { "origin" })));

                var matchFields = FieldRegEx.Matches(contents);
                foreach (var match in matchFields.Cast<Match>().Reverse())
                {
                    var values = match.Groups["type"].Value.Split('.').ToList();
                    if (values.Count == 1)
                        continue;

                    switch (lang)
                    {
                        case "c#":
                            values = values.Select(x => ScribanEx.CsReplaceReservedKeyword(x)).ToList();
                            break;
                    }

                    values.Insert(values.Count - 1, "origin");
                    contents = contents.Remove(match.Groups["type"].Index, match.Groups["type"].Value.Length).Insert(match.Groups["type"].Index, string.Join('.', values));
                }

                var fileName = Path.GetFileName(file);
                File.WriteAllText(Path.Join(to, Path.GetFileName(file)), contents);
                yield return fileName;
            }
        }

        public static IEnumerable<FlatBufferFileInfo> Parse(string path, string wildcard)
        {
            foreach (var file in Directory.GetFiles(path, wildcard))
            {
                yield return Parse(file);
            }
        }
    }
}
