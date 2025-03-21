using FlatBufferEx;
using FlatBufferEx.Model;
using NDesk.Options;
using Scriban;
using System.Diagnostics;
#if !DEBUG
using FlatBufferEx.Util;
using System.IO.Compression;
#endif

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
                var fname = $"nullable_{string.Join('_', nullableField.FixedNamespace.Concat([nullableField.Type]))}.fbs".ToLower();
                var path = Path.Combine(output, fname);
                File.WriteAllText(path, Template.Parse(File.ReadAllText("Template/nullable.txt")).Render(new { Field = nullableField, lang = lang }));
                yield return path;
            }
        }

        static async Task Main(string[] args)
        {
            var path = @"D:\Users\CSHYEON\Data\git\game\c++\fb\protocol";
            var output = "output";
            var includePath = string.Empty;
            var languages = "c++|c#";
            var gmn = string.Empty;
            var options = new OptionSet
            {
                { "p|path=", "input directory", v => path = v },
                { "l|lang=", "code language", v => languages = v },
                { "o|output=", "output directory", v => output = v },
                { "i|include=", "include directory path", v => includePath = v },
                { "gmn|go-module-name=", "go module name", v => gmn = v },
            };
            options.Parse(args);
            output = Path.GetFullPath(output);
            if (Directory.Exists(output))
                Directory.Delete(output, true);
            Directory.CreateDirectory(output);

#if !DEBUG
            await Http.DownloadFile("https://github.com/google/flatbuffers/releases/download/v24.3.25/Windows.flatc.binary.zip", "flatbuffer.zip");
            if (Directory.Exists("flatbuffer"))
                Directory.Delete("flatbuffer", true);
            ZipFile.ExtractToDirectory("flatbuffer.zip", "flatbuffer");
#endif

            var context = Parser.Parse(path, "*.fbs");
            foreach (var lang in languages.Split('|').Select(x => x.Trim().ToLower()).Distinct().ToHashSet())
            {
                var regeneratedFlatBufferFilePath = "raw";
                var regeneratedFlatBufferFiles = GenerateRawFlatBufferFiles(context, regeneratedFlatBufferFilePath, lang).Select(f => Path.Join(Directory.GetCurrentDirectory(), f)).ToList();
                var env = lang switch
                {
                    "c++" => "cpp",
                    "c#" => "csharp",
                    "go" => "go",
                    _ => throw new ArgumentException()
                };

                var p = new Process
                {
                    StartInfo =
                    {
                        CreateNoWindow = true,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        FileName = "cmd.exe",
                        WorkingDirectory = "flatbuffer",
                        Arguments = $"/c flatc.exe --{env} -I {Path.Join(Directory.GetCurrentDirectory(), regeneratedFlatBufferFilePath)} -o {Path.Join(output, "raw", lang)} {string.Join(" ", regeneratedFlatBufferFiles)}",
                    }
                };

                if (lang == "go" && string.IsNullOrEmpty(gmn) == false)
                    p.StartInfo.Arguments += $" --go-module-name {gmn}";

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
                Directory.Delete(regeneratedFlatBufferFilePath, true);
#endif
                if (p.ExitCode != 0)
                    Environment.Exit(p.ExitCode);

                var template = lang switch
                {
                    "c++" => Template.Parse(File.ReadAllText("Template/cpp.txt")),
                    "c#" => Template.Parse(File.ReadAllText("Template/c#.txt")),
                    "go" => Template.Parse(File.ReadAllText("Template/go.txt")),
                    _ => throw new ArgumentException()
                };

                var ext = lang switch
                {
                    "c++" => ".h",
                    "c#" => ".cs",
                    "go" => ".go",
                    _ => throw new ArgumentException()
                };

                var dir = Path.Join(output, lang);
                if (Directory.Exists(dir))
                    Directory.Delete(dir, true);
                Directory.CreateDirectory(dir);

                if (lang == "go")
                {
                    RenderBaseGoFile(context, gmn, dir, "fbex.go");
                    foreach (var scope in context.Scopes)
                    {
                        var obj = new ScribanEx
                        {
                            ["context"] = context,
                            ["include_path"] = includePath,
                            ["scope"] = scope,
                            ["go_module_name"] = gmn,
                            ["include_files"] = scope.IncludeFiles,
                            ["include_scopes"] = scope.IncludedScopes
                        };
                        var ctx = new TemplateContext();
                        ctx.PushGlobal(obj);

                        var dest = Path.Join(dir, Path.Join(scope.Namespace.ToArray()));
                        dest = Path.Join(dest, $"{Path.GetFileName(dest)}{ext}");
                        if (!Directory.Exists(Path.GetDirectoryName(dest)))
                            Directory.CreateDirectory(Path.GetDirectoryName(dest));

                        File.WriteAllText(dest, template.Render(ctx));
                    }
                }
                else
                {
                    var obj = new ScribanEx
                    {
                        ["context"] = context,
                        ["include_path"] = includePath,
                    };
                    var ctx = new TemplateContext();
                    ctx.PushGlobal(obj);

                    var dest = Path.Join(dir, $"protocol{ext}");
                    if (!Directory.Exists(Path.GetDirectoryName(dest)))
                        Directory.CreateDirectory(Path.GetDirectoryName(dest));

                    File.WriteAllText(dest, template.Render(ctx));
                }
            }
        }

        private static void RenderBaseGoFile(Context context, string gmn, string dir, string fname)
        {
            var baseTemplate = Template.Parse(File.ReadAllText("Template/go_base.txt"));
            var obj1 = new ScribanEx
            {
                ["context"] = context,
                ["go_module_name"] = gmn,
            };
            var ctx1 = new TemplateContext();
            ctx1.PushGlobal(obj1);
            File.WriteAllText(Path.Join(dir, fname), baseTemplate.Render(ctx1));
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