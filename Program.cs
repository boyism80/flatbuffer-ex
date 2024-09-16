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
            var languages = "c++";
            var options = new OptionSet
            {
                { "p|path=", "input directory", v => path = v },
                { "l|lang=", "code language", v => languages = v },
                { "o|output=", "output directory", v => output = v }
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

                foreach (var file in Directory.GetFiles(path, "*.fbs"))
                {
                    var info = Parser.Parse(file, "model");
                    var obj = new ScribanEx();
                    obj.Add("file", info.File);
                    obj.Add("output_dir", info.OutputDir);
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
                }
            }
        }
    }
}