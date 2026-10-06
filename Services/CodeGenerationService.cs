using FlatBufferEx.Model;

namespace FlatBufferEx.Services
{
    /// <summary>
    /// Implementation of code generation operations
    /// </summary>
    public class CodeGenerationService
    {
        private readonly FileService _fileService;
        private readonly TemplateService _templateService;

        public CodeGenerationService(FileService fileService, TemplateService templateService)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
        }

        /// <summary>
        /// Generates a single raw FlatBuffer schema file containing every scope, so flatc emits one file per language
        /// </summary>
        /// <returns>Full path of the generated schema file</returns>
        public async Task<string> GenerateRawSchemaFileAsync(Context context, string outputPath, string language)
        {
            _fileService.DeleteDirectory(outputPath);
            _fileService.CreateDirectory(outputPath);

            var contents = await _templateService.RenderRawTemplateAsync(context, language);
            var filePath = _fileService.CombinePath(outputPath, "protocol_raw.fbs");
            await _fileService.WriteAllTextAsync(filePath, contents);

            return _fileService.GetFullPath(filePath);
        }

        /// <summary>
        /// Generates final code file for the target language
        /// </summary>
        public async Task GenerateLanguageCodeAsync(Context context, string language, string outputPath, string includePath)
        {
            var content = await _templateService.RenderLanguageTemplateAsync(context, language, includePath);
            await _fileService.WriteAllTextAsync(outputPath, content);
        }
    }
} 