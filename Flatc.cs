using FlatBufferEx.Model;
using System.Diagnostics;
using System.IO.Compression;
using System.Text.RegularExpressions;

namespace FlatBufferEx
{
    public static class Flatc
    {
        private const string Version = "25.2.10";
        private const string Url = $"https://github.com/google/flatbuffers/releases/download/v{Version}/Windows.flatc.binary.zip";

        private static readonly string InstallPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "FlatBufferEx", $"flatc-{Version}");
        private static readonly string Executable = Path.Combine(InstallPath, "flatc.exe");

        public static async Task Install()
        {
            if (File.Exists(Executable))
                return;

            Console.WriteLine($"Downloading flatc {Version} to {InstallPath}");
            var zip = Path.Combine(Path.GetTempPath(), $"flatc-{Version}-{Guid.NewGuid():N}.zip");
            try
            {
                using (var client = new HttpClient())
                using (var source = await client.GetStreamAsync(Url))
                using (var file = new FileStream(zip, FileMode.Create))
                {
                    await source.CopyToAsync(file);
                }

                ZipFile.ExtractToDirectory(zip, InstallPath, true);
            }
            finally
            {
                File.Delete(zip);
            }
        }

        public static async Task Compile(Language language, string schema, string outputPath, IEnumerable<Namespace> namespaces)
        {
            if (Directory.Exists(outputPath))
                Directory.Delete(outputPath, true);
            Directory.CreateDirectory(outputPath);

            // flatc emits one C# file per type, and --gen-onefile puts every type under the last namespace of the schema,
            // so C# files go next to the schema first and are merged below. C++ already emits one header per schema.
            var flatcOutputPath = language == Language.CSharp ? Path.Combine(Path.GetDirectoryName(schema), "split") : outputPath;

            var startInfo = new ProcessStartInfo
            {
                FileName = Executable,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false
            };
            startInfo.ArgumentList.Add($"--{language.FlatcOption}");
            startInfo.ArgumentList.Add("-o");
            startInfo.ArgumentList.Add(flatcOutputPath);
            startInfo.ArgumentList.Add(schema);

            using var process = Process.Start(startInfo);
            var output = process.StandardOutput.ReadToEndAsync();
            var error = process.StandardError.ReadToEndAsync();
            await process.WaitForExitAsync();

            if (!string.IsNullOrWhiteSpace(await output))
                Console.WriteLine($"Compiler output: {await output}");
            if (!string.IsNullOrWhiteSpace(await error))
                Console.Error.WriteLine($"Compiler error: {await error}");
            if (process.ExitCode != 0)
                throw new InvalidOperationException($"FlatBuffer compilation failed for language: {language.Name}");

            if (language == Language.CSharp)
            {
                // Each flatc C# file wraps its type in its own namespace block, so plain concatenation stays valid
                var files = Directory.GetFiles(flatcOutputPath, "*.cs", SearchOption.AllDirectories).OrderBy(x => x, StringComparer.Ordinal);
                var merged = string.Join(Environment.NewLine, files.Select(File.ReadAllText));
                if (!language.EscapesNamespaceInSchema)
                {
                    // Longer namespaces first, so a shorter prefix does not match a namespace that is already escaped
                    var naming = Naming.Code(language);
                    foreach (var ns in namespaces.OrderByDescending(x => x.Segments.Count))
                    {
                        var name = string.Join('.', ns.Segments);
                        var escaped = naming.Namespace(ns);
                        if (name != escaped)
                            merged = Regex.Replace(merged, $@"(?<![\w.@]){Regex.Escape(name)}(?!\w)", escaped);
                    }
                }
                await File.WriteAllTextAsync(Path.Combine(outputPath, "protocol_raw_generated.cs"), merged);
            }
        }
    }
}
