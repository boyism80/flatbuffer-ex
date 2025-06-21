using System.Diagnostics;
using System.IO.Compression;
using FlatBufferEx.Configuration;
using FlatBufferEx.Util;

namespace FlatBufferEx.Services
{
    /// <summary>
    /// Implementation of FlatBuffer compiler operations
    /// </summary>
    public class FlatBufferCompilerService : IFlatBufferCompilerService
    {
        private readonly IFileService _fileService;

        public FlatBufferCompilerService(IFileService fileService)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public async Task<bool> CompileAsync(string language, IEnumerable<string> inputFiles, string outputPath, string includePath, string compilerPath)
        {
            var environment = GetCompilerEnvironment(language);
            var inputFileList = inputFiles.ToList();
            
            if (!inputFileList.Any())
            {
                throw new ArgumentException("No input files specified", nameof(inputFiles));
            }

            // Prepare output directory
            _fileService.DeleteDirectory(outputPath);
            _fileService.CreateDirectory(outputPath);

            // Build compiler arguments
            var arguments = BuildCompilerArguments(environment, inputFileList, outputPath, includePath);
            
            // Execute compiler
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

            // Read output
            var output = await process.StandardOutput.ReadToEndAsync();
            var error = await process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            // Log output
            if (!string.IsNullOrWhiteSpace(output))
            {
                Console.WriteLine($"Compiler output: {output}");
            }

            if (!string.IsNullOrWhiteSpace(error))
            {
                Console.Error.WriteLine($"Compiler error: {error}");
            }

            return process.ExitCode == 0;
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

        /// <summary>
        /// Builds compiler arguments
        /// </summary>
        /// <param name="environment">Compiler environment</param>
        /// <param name="inputFiles">Input files</param>
        /// <param name="outputPath">Output path</param>
        /// <param name="includePath">Include path</param>
        /// <returns>Compiler arguments string</returns>
        private static string BuildCompilerArguments(string environment, IList<string> inputFiles, string outputPath, string includePath)
        {
            var args = new List<string>
            {
                $"--{environment}",
                $"-o \"{outputPath}\""
            };

            if (!string.IsNullOrWhiteSpace(includePath))
            {
                args.Add($"-I \"{includePath}\"");
            }

            // Add input files
            args.AddRange(inputFiles.Select(f => $"\"{f}\""));

            return string.Join(" ", args);
        }
    }
} 