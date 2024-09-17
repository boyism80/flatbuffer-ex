using FlatBufferEx;
using FlatBufferEx.Model;
using NDesk.Options;
using Scriban;

namespace FlatBufferExample
{
    class Program
    {
        static void Main(string[] args)
        {
            var path = @"D:\Users\CSHYEON\Data\git\game\c++\fb\protocol";
            var output = "output";
            var includePath = string.Empty;
            var languages = "c#";
            var options = new OptionSet
            {
                { "p|path=", "input directory", v => path = v },
                { "l|lang=", "code language", v => languages = v },
                { "o|output=", "output directory", v => output = v },
                { "i|include=", "include directory path", v => includePath = v },
            };
            options.Parse(args);

            foreach (var lang in languages.Split('|').Select(x => x.Trim().ToLower()).Distinct().ToHashSet())
            {
                switch (lang)
                {
                    case "c++":
                        break;

                    default:
                        break;
                }

                var template = lang switch
                {
                    "c++" => Template.Parse(File.ReadAllText("Template/cpp.txt")),
                    "c#" => Template.Parse(File.ReadAllText("Template/c#.txt")),
                    _ => throw new ArgumentException()
                };

                var dir = Path.Join(output, lang);
                if (Directory.Exists(dir))
                    Directory.Delete(dir, true);
                Directory.CreateDirectory(dir);

                var protocolTypes = new Dictionary<string, List<string>>();
                foreach (var file in Directory.GetFiles(path, "*.fbs"))
                {
                    var info = Parser.Parse(file);
                    var obj = new ScribanEx();
                    obj.Add("file", info.File);
                    obj.Add("include_path", includePath);
                    obj.Add("root_type", info.RootType);
                    obj.Add("namespace", info.Namespace);
                    obj.Add("includes", info.Includes);
                    obj.Add("tables", info.Tables);
                    obj.Add("enums", info.Enums);
                    var ctx = new TemplateContext();
                    ctx.PushGlobal(obj);

                    var fname = lang switch
                    {
                        "c++" => $"{Path.GetFileNameWithoutExtension(file)}.h",
                        "c#" => $"{ScribanEx.UpperCamel(Path.GetFileNameWithoutExtension(file))}.cs",
                        _ => throw new ArgumentException()
                    };
                    File.WriteAllText(Path.Join(dir, fname), template.Render(ctx));

                    if (string.IsNullOrEmpty(info.RootType) == false)
                    {
                        var ns = string.Join(".", info.Namespace);
                        if (protocolTypes.ContainsKey(ns) == false)
                            protocolTypes.Add(ns, new List<string>());
                        protocolTypes[ns].Add(info.RootType);
                    }
                }

                switch (lang)
                {
                    case "c#":
                        File.WriteAllText(Path.Join(dir, "IFlatBufferEx.cs"), Template.Parse(File.ReadAllText("Template/c#_root.txt")).Render(new 
                        {
                            ProtocolTypes = protocolTypes.ToDictionary(x => x.Key.Split('.').ToList(), x => x.Value.OrderBy(x => x).ToList())
                        }));
                        break;
                }
            }
        }
    }
}