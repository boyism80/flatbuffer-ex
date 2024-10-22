using FlatBufferEx;
using FlatBufferEx.Model;
using FlatBufferEx.Util;
using NDesk.Options;
using Scriban;
using System.Diagnostics;
using System.IO.Compression;

namespace FlatBufferExample
{
    public enum Service : uint
    { }

    class Program
    {
        private static IEnumerable<string> GenerateRawFlatBufferFiles(Context context, string output, string lang)
        {
            if (Directory.Exists(output))
                Directory.Delete(output, true);
            Directory.CreateDirectory(output);
            foreach (var scope in context.Scopes)
            {
                foreach (var table in scope.Tables)
                {
                    var contents = Generator.RawFlatBufferTableContents(table, lang);
                    var path = Path.Combine(output, $"{string.Join('.', scope.Namespace)}.{table.Name.ToLower()}.fbs");
                    File.WriteAllText(path, contents);
                    yield return path;
                }

                foreach (var e in scope.Enums)
                {
                    var contents = Generator.RawFlatBufferEnumContents(e, lang);
                    var path = Path.Combine(output, $"{string.Join('.', scope.Namespace)}.{e.Name.ToLower()}.fbs");
                    File.WriteAllText(path, contents);
                    yield return path;
                }
            }

            foreach (var nullableField in context.NullableFields)
            {
                var fname = $"nullable_{string.Join('_', nullableField.FixedNamespace.Concat(new[] { nullableField.Type }))}.fbs".ToLower();
                var path = Path.Combine(output, fname);
                File.WriteAllText(path, Template.Parse(File.ReadAllText("Template/nullable.txt")).Render(new { Field = nullableField }));
                yield return path;
            }
        }

        static async Task Main(string[] args)
        {
            var path = @"D:\Users\CSHYEON\Data\git\game\c++\fb\protocol";
            var output = "output";
            var includePath = string.Empty;
            var languages = "c++|c#";
            var options = new OptionSet
            {
                { "p|path=", "input directory", v => path = v },
                { "l|lang=", "code language", v => languages = v },
                { "o|output=", "output directory", v => output = v },
                { "i|include=", "include directory path", v => includePath = v },
            };
            options.Parse(args);
            output = Path.GetFullPath(output);

#if !DEBUG
            await Http.DownloadFile("https://github.com/google/flatbuffers/releases/download/v24.3.25/Windows.flatc.binary.zip", "flatbuffer.zip");
            if (Directory.Exists("flatbuffer"))
                Directory.Delete("flatbuffer", true);
            ZipFile.ExtractToDirectory("flatbuffer.zip", "flatbuffer");
#endif

            var context = Parser.Parse(path, "*.fbs");
            foreach (var lang in languages.Split('|').Select(x => x.Trim().ToLower()).Distinct().ToHashSet())
            {
                var dir = Path.Join(output, lang);
                if (Directory.Exists(dir))
                    Directory.Delete(dir, true);
                Directory.CreateDirectory(dir);

                var rawFilePath = Path.Join(output, "raw");
                var rawFlatBufferFiles = GenerateRawFlatBufferFiles(context, rawFilePath, lang).ToList();
                var env = lang switch
                {
                    "c++" => "cpp",
                    "c#" => "csharp",
                    _ => throw new ArgumentException()
                };

                var p = new Process();
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.FileName = "cmd.exe";
                p.StartInfo.WorkingDirectory = "flatbuffer";
                p.StartInfo.Arguments = $"/c flatc.exe --{env} -I {rawFilePath} -o {Path.Join(output, lang)} {string.Join(" ", rawFlatBufferFiles)}";
                p.Start();

                while (p.StandardOutput.Peek() > -1)
                {
                    var line = await p.StandardOutput.ReadLineAsync();
                    Console.WriteLine(line);
                }

                while (p.StandardError.Peek() > -1)
                {
                    var line = await p.StandardError.ReadLineAsync();
                    Console.WriteLine(line);
                }

#if !DEBUG
                Directory.Delete(rawFilePath, true);
#endif
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

                var obj = new ScribanEx();
                obj.Add("context", context);
                obj.Add("include_path", includePath);
                var ctx = new TemplateContext();
                ctx.PushGlobal(obj);

                var dest = Path.Join(dir, $"protocol{ext}");
                if (!Directory.Exists(Path.GetDirectoryName(dest)))
                    Directory.CreateDirectory(Path.GetDirectoryName(dest));

                File.WriteAllText(dest, template.Render(ctx));
            }
        }

        private static IEnumerable<string> IncludesToFiles(List<string> includes, List<FlatBufferFileInfo> parseResultList)
        {
            foreach (var include in includes)
            {
                foreach (var parseResult in parseResultList)
                {
                    if (parseResult.File == include)
                        yield return string.Join('.', parseResult.Namespace);
                }
            }
        }
    }
}