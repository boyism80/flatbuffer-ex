using System.Diagnostics;
using System.IO.Compression;
using FlatBufferEx.Configuration;
using FlatBufferEx.Util;

namespace FlatBufferEx.Services
{
    /// <summary>
    /// Implementation of FlatBuffer compiler operations
    /// </summary>
    public class FlatBufferCompilerService
    {
        private readonly FileService _fileService;

        public FlatBufferCompilerService(FileService fileService)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        }

        /// <summary>
        /// Sets up the FlatBuffer compiler by downloading and extracting it
        /// </summary>
        public async Task SetupCompilerAsync(string downloadUrl, string extractPath)
        {
#if !DEBUG
            const string zipFileName = "flatbuffer.zip";

            // Download the compiler
            await Http.DownloadFile(downloadUrl, zipFileName);

            // Clean up existing directory
            _fileService.DeleteDirectory(extractPath);

            // Extract the compiler
            ZipFile.ExtractToDirectory(zipFileName, extractPath);

            // Clean up zip file
            if (File.Exists(zipFileName))
            {
                File.Delete(zipFileName);
            }
#else
            // In debug mode, assume compiler is already available
            await Task.CompletedTask;
#endif
        }

        /// <summary>
        /// Compiles the raw FlatBuffer schema file into a single source file for the target language
        /// </summary>
        public async Task<bool> CompileAsync(string language, string inputFile, string outputPath, string compilerPath)
        {
            var environment = GetCompilerEnvironment(language);

            _fileService.DeleteDirectory(outputPath);
            _fileService.CreateDirectory(outputPath);

            // flatc emits one C# file per type, and --gen-onefile puts every type under the last namespace of the schema,
            // so C# files go to a split directory first and are merged below. C++ already emits one header per schema.
            var flatcOutputPath = language == "c#" ? _fileService.CombinePath(outputPath, "split") : outputPath;
            var arguments = $"--{environment} -o \"{flatcOutputPath}\" \"{inputFile}\"";

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "cmd.exe",
                    Arguments = $"/c flatc.exe {arguments}",
                    WorkingDirectory = compilerPath,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false
                }
            };

            process.Start();

            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            if (!string.IsNullOrWhiteSpace(output))
            {
                Console.WriteLine($"Compiler output: {output}");
            }

            if (!string.IsNullOrWhiteSpace(error))
            {
                Console.Error.WriteLine($"Compiler error: {error}");
            }

            if (process.ExitCode != 0)
            {
                return false;
            }

            if (language == "c#")
            {
                // Each flatc C# file wraps its type in its own namespace block, so plain concatenation stays valid
                var files = Directory.GetFiles(flatcOutputPath, "*.cs", SearchOption.AllDirectories).OrderBy(x => x, StringComparer.Ordinal);
                var merged = string.Join(Environment.NewLine, files.Select(File.ReadAllText));
                await _fileService.WriteAllTextAsync(_fileService.CombinePath(outputPath, "protocol_raw_generated.cs"), merged);
                _fileService.DeleteDirectory(flatcOutputPath);
            }

            return true;
        }

        /// <summary>
        /// Gets the compiler environment name for the language
        /// </summary>
        /// <param name="language">Target language</param>
        /// <returns>Compiler environment name</returns>
        private static string GetCompilerEnvironment(string language)
        {
            return language switch
            {
                "c++" => "cpp",
                "c#" => "csharp",
                _ => throw new ArgumentException($"Unsupported language: {language}")
            };
        }
    }
}
