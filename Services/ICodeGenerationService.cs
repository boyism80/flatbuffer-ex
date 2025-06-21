using FlatBufferEx.Model;

namespace FlatBufferEx.Services
{
    /// <summary>
    /// Interface for code generation operations
    /// </summary>
    public interface ICodeGenerationService
    {
        /// <summary>
        /// Generates raw FlatBuffer files from context
        /// </summary>
        /// <param name="context">Parsing context</param>
        /// <param name="outputPath">Output directory</param>
        /// <param name="language">Target language</param>
        /// <returns>Collection of generated file paths</returns>
        Task<IEnumerable<string>> GenerateRawFlatBufferFilesAsync(Context context, string outputPath, string language);

        /// <summary>
        /// Generates final code file for the target language
        /// </summary>
        /// <param name="context">Parsing context</param>
        /// <param name="language">Target language</param>
        /// <param name="outputPath">Output file path</param>
        /// <param name="includePath">Include path</param>
        Task GenerateLanguageCodeAsync(Context context, string language, string outputPath, string includePath);
    }
} 