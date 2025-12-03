using FlatBufferEx.Configuration;

namespace FlatBufferEx.Services
{
    /// <summary>
    /// Main processor that orchestrates the FlatBuffer code generation process
    /// </summary>
    public class FlatBufferProcessor
    {
        private readonly FileService _fileService;
        private readonly TemplateService _templateService;
        private readonly FlatBufferCompilerService _compilerService;
        private readonly CodeGenerationService _codeGenerationService;

        public FlatBufferProcessor(FileService fileService, TemplateService templateService, FlatBufferCompilerService compilerService, CodeGenerationService codeGenerationService)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
            _compilerService = compilerService ?? throw new ArgumentNullException(nameof(compilerService));
            _codeGenerationService = codeGenerationService ?? throw new ArgumentNullException(nameof(codeGenerationService));
        }

        /// <summary>
        /// Processes the FlatBuffer generation based on configuration
        /// </summary>
        /// <param name="config">Application configuration</param>
        public async Task ProcessAsync(AppConfiguration config)
        {
            Console.WriteLine("Starting FlatBuffer code generation...");

            // Prepare output directory
            var outputPath = config.GetFullOutputPath();
            _fileService.DeleteDirectory(outputPath);
            _fileService.CreateDirectory(outputPath);

            // Setup compiler
            await _compilerService.SetupCompilerAsync(config.FlatBufferCompilerUrl, config.CompilerDirectory);

            // Parse FlatBuffer schema files
            Console.WriteLine($"Parsing schema files from: {config.InputPath}");
            var context = Parser.Parse(config.InputPath, "*.fbs");
            Console.WriteLine($"Found {context.Scopes.Count} scopes with {context.Scopes.Sum(s => s.Tables.Count)} tables and {context.Scopes.Sum(s => s.Enums.Count)} enums");

            // Generate code for each language
            foreach (var language in config.GetTargetLanguages())
            {
                Console.WriteLine($"Generating code for language: {language}");
                await ProcessLanguageAsync(config, context, language);
            }

            // Clean up temporary files
            CleanupTemporaryFiles(config);

            Console.WriteLine("FlatBuffer code generation completed successfully.");
        }

        /// <summary>
        /// Processes code generation for a specific language
        /// </summary>
        /// <param name="config">Application configuration</param>
        /// <param name="context">Parsing context</param>
        /// <param name="language">Target language</param>
        private async Task ProcessLanguageAsync(AppConfiguration config, Model.Context context, string language)
        {
            var tempRawPath = config.TempDirectory;

            try
            {
                // Generate raw FlatBuffer files
                Console.WriteLine($"  Generating raw FlatBuffer files...");
                var rawFiles = await _codeGenerationService.GenerateRawFlatBufferFilesAsync(context, tempRawPath, language);
                var rawFilesList = rawFiles.ToList();
                Console.WriteLine($"  Generated {rawFilesList.Count} raw files");

                // Compile with FlatBuffer compiler
                Console.WriteLine($"  Compiling with FlatBuffer compiler...");
                var compilerOutputPath = _fileService.CombinePath(config.GetFullOutputPath(), "raw", language);
                var compileSuccess = await _compilerService.CompileAsync(
                    language,
                    rawFilesList,
                    compilerOutputPath,
                    _fileService.GetFullPath(tempRawPath),
                    config.CompilerDirectory);

                if (!compileSuccess)
                {
                    throw new InvalidOperationException($"FlatBuffer compilation failed for language: {language}");
                }

                // Generate final code file
                Console.WriteLine($"  Generating final code file...");
                var finalOutputDir = _fileService.CombinePath(config.GetFullOutputPath(), language);
                _fileService.CreateDirectory(finalOutputDir);

                var finalOutputPath = _fileService.CombinePath(finalOutputDir, $"protocol{config.GetFileExtension(language)}");
                await _codeGenerationService.GenerateLanguageCodeAsync(context, language, finalOutputPath, config.IncludePath);

                Console.WriteLine($"  Generated: {finalOutputPath}");
            }
            finally
            {
                // Clean up temporary raw files for this language
#if !DEBUG
                _fileService.DeleteDirectory(tempRawPath);
#endif
            }
        }

        /// <summary>
        /// Cleans up temporary files
        /// </summary>
        /// <param name="config">Application configuration</param>
        private void CleanupTemporaryFiles(AppConfiguration config)
        {
#if !DEBUG
            // Clean up compiler directory
            _fileService.DeleteDirectory(config.CompilerDirectory);

            // Clean up any remaining temporary files
            _fileService.DeleteDirectory(config.TempDirectory);
#endif
        }
    }
}