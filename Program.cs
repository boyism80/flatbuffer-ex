using FlatBufferEx;
using FlatBufferEx.Model;
using FlatBufferEx.Util;
using NDesk.Options;
using Scriban;
using System.Diagnostics;
using System.IO.Compression;

namespace FlatBufferExample
{
    /// <summary>
    /// Service enumeration (extensible)
    /// </summary>
    public enum Service : uint
    { }

    /// <summary>
    /// Main program class for FlatBuffer extension tool
    /// Parses FlatBuffer schema files (.fbs) and generates code for various languages.
    /// </summary>
    class Program
    {
        /// <summary>
        /// Generates raw FlatBuffer files based on the context.
        /// Creates individual .fbs files for each table and enum,
        /// and also generates files for nullable fields.
        /// </summary>
        /// <param name="context">Parsed FlatBuffer context</param>
        /// <param name="output">Output directory path</param>
        /// <param name="lang">Target language</param>
        /// <returns>Generated file paths</returns>
        private static IEnumerable<string> GenerateRawFlatBufferFiles(Context context, string output, string lang)
        {
            // Initialize output directory
            if (Directory.Exists(output))
                Directory.Delete(output, true);
            Directory.CreateDirectory(output);
            
            // Generate .fbs files for tables in each scope
            foreach (var scope in context.Scopes)
            {
                foreach (var table in scope.Tables)
                {
                    var contents = Generator.RawFlatBufferTableContents(table, lang);
                    var path = Path.Combine(output, $"{string.Join('.', scope.Namespace)}.{table.Name.ToLower()}.fbs");
                    File.WriteAllText(path, contents);
                    yield return path;
                }

                // Generate .fbs files for enums in each scope
                foreach (var e in scope.Enums)
                {
                    var contents = Generator.RawFlatBufferEnumContents(e, lang);
                    var path = Path.Combine(output, $"{string.Join('.', scope.Namespace)}.{e.Name.ToLower()}.fbs");
                    File.WriteAllText(path, contents);
                    yield return path;
                }
            }

            // Generate .fbs files for nullable fields
            foreach (var nullableField in context.NullableFields)
            {
                var fname = $"nullable_{string.Join('_', nullableField.FixedNamespace.Concat(new[] { nullableField.Type }))}.fbs".ToLower();
                var path = Path.Combine(output, fname);
                File.WriteAllText(path, Template.Parse(File.ReadAllText("Template/nullable.txt")).Render(new { Field = nullableField }));
                yield return path;
            }
        }

        /// <summary>
        /// Application entry point
        /// Parses command line arguments and executes FlatBuffer compiler to generate code.
        /// </summary>
        /// <param name="args">Command line arguments</param>
        static async Task Main(string[] args)
        {
            // Default configuration values
            var path = @"D:\Users\CSHYEON\Data\git\game\c++\fb\protocol";
            var output = "output";
            var includePath = string.Empty;
            var languages = "c++|c#";
            
            // Parse command line options
            var options = new OptionSet
            {
                { "p|path=", "input directory", v => path = v },
                { "l|lang=", "code language", v => languages = v },
                { "o|output=", "output directory", v => output = v },
                { "i|include=", "include directory path", v => includePath = v },
            };
            options.Parse(args);
            
            // Prepare output directory
            output = Path.GetFullPath(output);
            if (Directory.Exists(output))
                Directory.Delete(output, true);
            Directory.CreateDirectory(output);

#if !DEBUG
            // Download and extract FlatBuffer compiler in Release mode
            await Http.DownloadFile("https://github.com/google/flatbuffers/releases/download/v25.2.10/Windows.flatc.binary.zip", "flatbuffer.zip");
            if (Directory.Exists("flatbuffer"))
                Directory.Delete("flatbuffer", true);
            ZipFile.ExtractToDirectory("flatbuffer.zip", "flatbuffer");
#endif

            // Parse FlatBuffer schema files to create context
            var context = Parser.Parse(path, "*.fbs");
            
            // Generate code for each language
            foreach (var lang in languages.Split('|').Select(x => x.Trim().ToLower()).Distinct().ToHashSet())
            {
                var regeneratedFlatBufferFilePath = "raw";
                
                // Generate raw FlatBuffer files
                var regeneratedFlatBufferFiles = GenerateRawFlatBufferFiles(context, regeneratedFlatBufferFilePath, lang)
                    .Select(f => Path.Join(Directory.GetCurrentDirectory(), f)).ToList();
                
                // Set language-specific environment
                var env = lang switch
                {
                    "c++" => "cpp",
                    "c#" => "csharp",
                    _ => throw new ArgumentException()
                };

                // Execute FlatBuffer compiler
                var p = new Process();
                p.StartInfo.CreateNoWindow = true;
                p.StartInfo.RedirectStandardOutput = true;
                p.StartInfo.RedirectStandardError = true;
                p.StartInfo.FileName = "cmd.exe";
                p.StartInfo.WorkingDirectory = "flatbuffer";
                p.StartInfo.Arguments = $"/c flatc.exe --{env} -I {Path.Join(Directory.GetCurrentDirectory(), regeneratedFlatBufferFilePath)} -o {Path.Join(output, "raw", lang)} {string.Join(" ", regeneratedFlatBufferFiles)}";
                p.Start();

                // Read standard output
                while (p.StandardOutput.Peek() > -1)
                {
                    var line = await p.StandardOutput.ReadLineAsync();
                    Console.WriteLine(line);
                }

                // Read standard error
                while (p.StandardError.Peek() > -1)
                {
                    var line = await p.StandardError.ReadLineAsync();
                    Console.WriteLine(line);
                }

#if !DEBUG
                // Clean up temporary files
                Directory.Delete(regeneratedFlatBufferFilePath, true);
#endif
                // Exit if compiler execution failed
                if (p.ExitCode != 0)
                    Environment.Exit(p.ExitCode);

                // Load language-specific template
                var template = lang switch
                {
                    "c++" => Template.Parse(File.ReadAllText("Template/cpp.txt")),
                    "c#" => Template.Parse(File.ReadAllText("Template/c#.txt")),
                    _ => throw new ArgumentException()
                };

                // Set language-specific file extension
                var ext = lang switch
                {
                    "c++" => ".h",
                    "c#" => ".cs",
                    _ => throw new ArgumentException()
                };

                // Prepare output directory
                var dir = Path.Join(output, lang);
                if (Directory.Exists(dir))
                    Directory.Delete(dir, true);
                Directory.CreateDirectory(dir);

                // Set up Scriban template context
                var obj = new ScribanEx();
                obj.Add("context", context);
                obj.Add("include_path", includePath);
                var ctx = new TemplateContext();
                ctx.PushGlobal(obj);

                // Generate final code file
                var dest = Path.Join(dir, $"protocol{ext}");
                if (!Directory.Exists(Path.GetDirectoryName(dest)))
                    Directory.CreateDirectory(Path.GetDirectoryName(dest));

                File.WriteAllText(dest, template.Render(ctx));
            }
        }

        /// <summary>
        /// Converts include file list to file names.
        /// (Currently unused method)
        /// </summary>
        /// <param name="includes">Include file list</param>
        /// <param name="parseResultList">Parse result list</param>
        /// <returns>File names</returns>
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