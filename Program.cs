using FlatBufferEx;
using FlatBufferEx.Model;
using FlatBufferEx.Util;
using NDesk.Options;
using Scriban;
using System.Diagnostics;
using System.IO.Compression;
using System.Xml.Linq;

namespace FlatBufferExample
{
    public enum Service : uint
    { }

    class Program
    {
        static async Task Main(string[] args)
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

            await Http.DownloadFile("https://github.com/google/flatbuffers/releases/download/v24.3.25/Windows.flatc.binary.zip", "flatbuffer.zip");
            if (Directory.Exists("flatbuffer"))
                Directory.Delete("flatbuffer", true);
            ZipFile.ExtractToDirectory("flatbuffer.zip", "flatbuffer");

            foreach (var lang in languages.Split('|').Select(x => x.Trim().ToLower()).Distinct().ToHashSet())
            {
                var originFiles = Parser.CreateOriginTempFiles(path, "flatbuffer", "*.fbs", lang).ToList();

                var env = lang switch 
                {
                    "c++" => "cpp",
                    "c#" => "csharp",
                    _ => throw new ArgumentException()
                };

                var p = new Process();
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.FileName = "cmd.exe";
                p.StartInfo.WorkingDirectory = "flatbuffer";
                p.StartInfo.Arguments = $"/c flatc.exe --{env} -o {lang} {string.Join(" ", originFiles)}";
                p.Start();

                while (!p.StandardOutput.EndOfStream)
                { 
                    var line = await p.StandardOutput.ReadLineAsync();
                    Console.WriteLine(line);
                }

                if (p.ExitCode != 0)
                    Environment.Exit(p.ExitCode);

                var template = lang switch
                {
                    "c++" => Template.Parse(File.ReadAllText("Template/cpp.txt")),
                    "c#" => Template.Parse(File.ReadAllText("Template/c#.txt")),
                    _ => throw new ArgumentException()
                };

                var ext = lang switch
                {
                    "c++" => ".h",
                    "c#" => ".cs",
                    _ => throw new ArgumentException()
                };

                var dir = Path.Join(output, lang);
                if (Directory.Exists(dir))
                    Directory.Delete(dir, true);
                Directory.CreateDirectory(dir);

                foreach (var g in Parser.Parse(path, "*.fbs").GroupBy(x => string.Join(".", x.Namespace)))
                {
                    var infos = g.ToList();
                    var obj = new ScribanEx();
                    obj.Add("files", infos.Select(x => x.File).ToList());
                    obj.Add("include_path", includePath);
                    obj.Add("namespace", g.Key.Split('.').ToList());
                    obj.Add("includes", infos.SelectMany(x => x.Includes).ToList());
                    obj.Add("tables", infos.SelectMany(x => x.Tables).ToList());
                    obj.Add("enums", infos.SelectMany(x => x.Enums).ToList());
                    var ctx = new TemplateContext();
                    ctx.PushGlobal(obj);

                    File.WriteAllText(Path.Join(dir, $"{g.Key}{ext}"), template.Render(ctx));
                }

                //var protocolTypes = new Dictionary<string, List<string>>();
                //foreach (var file in Directory.GetFiles(path, "*.fbs"))
                //{
                //    var info = Parser.Parse(file);
                //    var obj = new ScribanEx();
                //    obj.Add("file", info.File);
                //    obj.Add("include_path", includePath);
                //    obj.Add("root_type", info.RootType);
                //    obj.Add("namespace", info.Namespace);
                //    obj.Add("includes", info.Includes);
                //    obj.Add("tables", info.Tables);
                //    obj.Add("enums", info.Enums);
                //    var ctx = new TemplateContext();
                //    ctx.PushGlobal(obj);

                //    var fname = lang switch
                //    {
                //        "c++" => $"{Path.GetFileNameWithoutExtension(file)}.h",
                //        "c#" => $"{ScribanEx.UpperCamel(Path.GetFileNameWithoutExtension(file))}.cs",
                //        _ => throw new ArgumentException()
                //    };
                //    File.WriteAllText(Path.Join(dir, fname), template.Render(ctx));

                //    if (string.IsNullOrEmpty(info.RootType) == false)
                //    {
                //        var ns = string.Join(".", info.Namespace);
                //        if (protocolTypes.ContainsKey(ns) == false)
                //            protocolTypes.Add(ns, new List<string>());
                //        protocolTypes[ns].Add(info.RootType);
                //    }
                //}

                //switch (lang)
                //{
                //    case "c#":
                //        File.WriteAllText(Path.Join(dir, "IFlatBufferEx.cs"), Template.Parse(File.ReadAllText("Template/c#_root.txt")).Render(new 
                //        {
                //            ProtocolTypes = protocolTypes.ToDictionary(x => x.Key.Split('.').ToList(), x => x.Value.OrderBy(x => x).ToList())
                //        }));
                //        break;

                //    case "c++":
                //        File.WriteAllText(Path.Join(dir, "protocol_type.h"), Template.Parse(File.ReadAllText("Template/cpp_protocol_type.txt")).Render(new
                //        {
                //            ProtocolTypes = protocolTypes.ToDictionary(x => x.Key.Split('.').ToList(), x => x.Value.OrderBy(x => x).ToList())
                //        }));
                //        break;
                //}
            }
        }
    }
}