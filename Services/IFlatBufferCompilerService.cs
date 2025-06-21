namespace FlatBufferEx.Services
{
    /// <summary>
    /// Interface for FlatBuffer compiler operations
    /// </summary>
    public interface IFlatBufferCompilerService
    {
        /// <summary>
        /// Downloads and sets up the FlatBuffer compiler
        /// </summary>
        /// <param name="downloadUrl">Compiler download URL</param>
        /// <param name="extractPath">Path to extract the compiler</param>
        Task SetupCompilerAsync(string downloadUrl, string extractPath);

        /// <summary>
        /// Compiles FlatBuffer schema files
        /// </summary>
        /// <param name="language">Target language</param>
        /// <param name="inputFiles">Input .fbs files</param>
        /// <param name="outputPath">Output directory</param>
        /// <param name="includePath">Include directory</param>
        /// <param name="compilerPath">Compiler executable path</param>
        /// <returns>True if compilation succeeded</returns>
        Task<bool> CompileAsync(string language, IEnumerable<string> inputFiles, string outputPath, string includePath, string compilerPath);
    }
} 