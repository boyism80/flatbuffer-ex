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

            // Split files into batches to avoid Windows command line length limit (~8,191 characters)
            const int maxCommandLineLength = 7000; // Leave some margin for safety
            var batches = SplitIntoBatches(inputFileList, outputPath, includePath, environment, maxCommandLineLength);
            
            Console.WriteLine($"  Compiling {inputFileList.Count} files in {batches.Count} batch(es)...");
            
            // Compile each batch
            for (int i = 0; i < batches.Count; i++)
            {
                var batch = batches[i];
                Console.WriteLine($"  Batch {i + 1}/{batches.Count}: {batch.Count} files");
                
                var arguments = BuildCompilerArguments(environment, batch, outputPath, includePath);
                
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

                if (process.ExitCode != 0)
                {
                    return false;
                }
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

        /// <summary>
        /// Splits input files into batches to avoid Windows command line length limit
        /// </summary>
        /// <param name="inputFiles">All input files</param>
        /// <param name="outputPath">Output path</param>
        /// <param name="includePath">Include path</param>
        /// <param name="environment">Compiler environment</param>
        /// <param name="maxLength">Maximum command line length</param>
        /// <returns>List of file batches</returns>
        private static List<List<string>> SplitIntoBatches(IList<string> inputFiles, string outputPath, string includePath, string environment, int maxLength)
        {
            var batches = new List<List<string>>();
            var currentBatch = new List<string>();
            var baseArgsLength = CalculateBaseArgsLength(environment, outputPath, includePath);
            
            foreach (var file in inputFiles)
            {
                var fileArgLength = $"\"{file}\"".Length + 1; // +1 for space
                var currentBatchLength = baseArgsLength + currentBatch.Sum(f => $"\"{f}\"".Length + 1);
                
                if (currentBatchLength + fileArgLength > maxLength && currentBatch.Count > 0)
                {
                    batches.Add(currentBatch);
                    currentBatch = new List<string>();
                }
                
                currentBatch.Add(file);
            }
            
            if (currentBatch.Count > 0)
            {
                batches.Add(currentBatch);
            }
            
            return batches;
        }

        /// <summary>
        /// Calculates the length of base compiler arguments (without input files)
        /// </summary>
        /// <param name="environment">Compiler environment</param>
        /// <param name="outputPath">Output path</param>
        /// <param name="includePath">Include path</param>
        /// <returns>Length of base arguments</returns>
        private static int CalculateBaseArgsLength(string environment, string outputPath, string includePath)
        {
            var length = $"--{environment} -o \"{outputPath}\"".Length + 1; // +1 for space before file args
            
            if (!string.IsNullOrWhiteSpace(includePath))
            {
                length += $" -I \"{includePath}\"".Length;
            }
            
            return length;
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