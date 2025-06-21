using FlatBufferEx.Model;

namespace FlatBufferEx.Services
{
    /// <summary>
    /// Implementation of code generation operations
    /// </summary>
    public class CodeGenerationService : ICodeGenerationService
    {
        private readonly IFileService _fileService;
        private readonly ITemplateService _templateService;

        public CodeGenerationService(IFileService fileService, ITemplateService templateService)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
        }

        /// <inheritdoc />
        public async Task<IEnumerable<string>> GenerateRawFlatBufferFilesAsync(Context context, string outputPath, string language)
        {
            var generatedFiles = new List<string>();

            // Initialize output directory
            _fileService.DeleteDirectory(outputPath);
            _fileService.CreateDirectory(outputPath);

            // Generate .fbs files for tables in each scope
            foreach (var scope in context.Scopes)
            {
                foreach (var table in scope.Tables)
                {
                    var contents = await _templateService.RenderTableTemplateAsync(table, language);
                    var fileName = $"{string.Join('.', scope.Namespace)}.{table.Name.ToLower()}.fbs";
                    var filePath = _fileService.CombinePath(outputPath, fileName);
                    
                    await _fileService.WriteAllTextAsync(filePath, contents);
                    generatedFiles.Add(_fileService.GetFullPath(filePath));
                }

                // Generate .fbs files for enums in each scope
                foreach (var enumModel in scope.Enums)
                {
                    var contents = await _templateService.RenderEnumTemplateAsync(enumModel, language);
                    var fileName = $"{string.Join('.', scope.Namespace)}.{enumModel.Name.ToLower()}.fbs";
                    var filePath = _fileService.CombinePath(outputPath, fileName);
                    
                    await _fileService.WriteAllTextAsync(filePath, contents);
                    generatedFiles.Add(_fileService.GetFullPath(filePath));
                }
            }

            // Generate .fbs files for nullable fields
            foreach (var nullableField in context.NullableFields)
            {
                var contents = await _templateService.RenderNullableTemplateAsync(nullableField);
                var fileName = $"nullable_{string.Join('_', nullableField.FixedNamespace.Concat(new[] { nullableField.Type }))}.fbs".ToLower();
                var filePath = _fileService.CombinePath(outputPath, fileName);
                
                await _fileService.WriteAllTextAsync(filePath, contents);
                generatedFiles.Add(_fileService.GetFullPath(filePath));
            }

            return generatedFiles;
        }

        /// <inheritdoc />
        public async Task GenerateLanguageCodeAsync(Context context, string language, string outputPath, string includePath)
        {
            var content = await _templateService.RenderLanguageTemplateAsync(context, language, includePath);
            await _fileService.WriteAllTextAsync(outputPath, content);
        }
    }
} 