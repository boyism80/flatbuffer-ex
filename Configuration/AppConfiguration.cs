using System.ComponentModel.DataAnnotations;

namespace FlatBufferEx.Configuration
{
    /// <summary>
    /// Application configuration class that holds all settings for the FlatBuffer code generator
    /// </summary>
    public class AppConfiguration
    {
        /// <summary>
        /// Input directory path containing .fbs schema files
        /// </summary>
        public string InputPath { get; set; } = string.Empty;

        /// <summary>
        /// Target languages for code generation (e.g., "c++|c#")
        /// </summary>
        public string Languages { get; set; } = "c#";

        /// <summary>
        /// Output directory path for generated code
        /// </summary>
        public string OutputPath { get; set; } = "output";

        /// <summary>
        /// Include directory path for additional schema files
        /// </summary>
        public string IncludePath { get; set; } = string.Empty;

        /// <summary>
        /// Supported languages
        /// </summary>
        public static readonly HashSet<string> SupportedLanguages = new() { "c++", "c#" };

        /// <summary>
        /// Default FlatBuffer compiler download URL
        /// </summary>
        public string FlatBufferCompilerUrl { get; set; } = "https://github.com/google/flatbuffers/releases/download/v25.2.10/Windows.flatc.binary.zip";

        /// <summary>
        /// Temporary directory for raw FlatBuffer files
        /// </summary>
        public string TempDirectory { get; set; } = "raw";

        /// <summary>
        /// FlatBuffer compiler directory
        /// </summary>
        public string CompilerDirectory { get; set; } = "flatbuffer";

        /// <summary>
        /// Template directory path
        /// </summary>
        public string TemplateDirectory { get; set; } = "Template";

        /// <summary>
        /// Gets the parsed target languages
        /// </summary>
        public IEnumerable<string> GetTargetLanguages()
        {
            return Languages
                .Split('|')
                .Select(x => x.Trim().ToLower())
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct();
        }

        /// <summary>
        /// Validates the configuration and returns validation errors
        /// </summary>
        /// <param name="errors">Collection of validation errors</param>
        /// <returns>True if configuration is valid, false otherwise</returns>
        public bool IsValid(out List<string> errors)
        {
            errors = new List<string>();

            // Validate input path
            if (string.IsNullOrWhiteSpace(InputPath))
            {
                errors.Add("Input path is required");
            }
            else if (!Directory.Exists(InputPath))
            {
                errors.Add($"Input directory does not exist: {InputPath}");
            }

            // Validate output path
            if (string.IsNullOrWhiteSpace(OutputPath))
            {
                errors.Add("Output path is required");
            }

            // Validate languages
            if (string.IsNullOrWhiteSpace(Languages))
            {
                errors.Add("At least one target language must be specified");
            }
            else
            {
                var targetLanguages = GetTargetLanguages().ToList();
                if (!targetLanguages.Any())
                {
                    errors.Add("No valid target languages specified");
                }

                var unsupportedLanguages = targetLanguages.Where(lang => !SupportedLanguages.Contains(lang)).ToList();
                if (unsupportedLanguages.Any())
                {
                    errors.Add($"Unsupported languages: {string.Join(", ", unsupportedLanguages)}. Supported languages: {string.Join(", ", SupportedLanguages)}");
                }
            }

            // Validate template directory
            if (!Directory.Exists(TemplateDirectory))
            {
                errors.Add($"Template directory does not exist: {TemplateDirectory}");
            }

            return !errors.Any();
        }

        /// <summary>
        /// Gets the full output path
        /// </summary>
        public string GetFullOutputPath()
        {
            return Path.GetFullPath(OutputPath);
        }

        /// <summary>
        /// Gets the language-specific compiler environment name
        /// </summary>
        /// <param name="language">Target language</param>
        /// <returns>Compiler environment name</returns>
        public string GetCompilerEnvironment(string language)
        {
            return language switch
            {
                "c++" => "cpp",
                "c#" => "csharp",
                _ => throw new ArgumentException($"Unsupported language: {language}")
            };
        }

        /// <summary>
        /// Gets the file extension for the target language
        /// </summary>
        /// <param name="language">Target language</param>
        /// <returns>File extension</returns>
        public string GetFileExtension(string language)
        {
            return language switch
            {
                "c++" => ".h",
                "c#" => ".cs",
                _ => throw new ArgumentException($"Unsupported language: {language}")
            };
        }
    }
}