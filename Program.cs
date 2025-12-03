using FlatBufferEx;
using FlatBufferEx.Configuration;
using FlatBufferEx.Services;
using Microsoft.Extensions.DependencyInjection;
using NDesk.Options;

namespace FlatBufferExample
{
    /// <summary>
    /// Service enumeration (extensible)
    /// </summary>
    public enum Service : uint
    { }

    /// <summary>
    /// Main program class for FlatBuffer extension tool
    /// Parses FlatBuffer schema files (.fbs) and generates code for various languages.
    /// </summary>
    class Program
    {
        /// <summary>
        /// Application entry point
        /// Parses command line arguments and executes FlatBuffer compiler to generate code.
        /// </summary>
        /// <param name="args">Command line arguments</param>
        static async Task<int> Main(string[] args)
        {
            try
            {
                var config = ParseCommandLineArguments(args);
                if (config == null)
                {
                    ShowUsage();
                    return 1;
                }

                using var serviceProvider = CreateServiceProvider();
                var processor = serviceProvider.GetRequiredService<FlatBufferProcessor>();

                await processor.ProcessAsync(config);

                Console.WriteLine("FlatBuffer code generation completed successfully.");
                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Error: {ex.Message}");
                Console.Error.WriteLine($"Stack trace: {ex.StackTrace}");
                return 1;
            }
        }

        /// <summary>
        /// Parses command line arguments and returns configuration
        /// </summary>
        /// <param name="args">Command line arguments</param>
        /// <returns>Configuration object or null if parsing failed</returns>
        private static AppConfiguration ParseCommandLineArguments(string[] args)
        {
            var config = new AppConfiguration();
            var showHelp = false;

            var options = new OptionSet
            {
                { "p|path=", "input directory containing .fbs files", v => config.InputPath = v },
                { "l|lang=", "target languages (e.g., \"c++|c#\")", v => config.Languages = v },
                { "o|output=", "output directory for generated code", v => config.OutputPath = v },
                { "i|include=", "include directory path", v => config.IncludePath = v },
                { "h|help", "show this help message", v => showHelp = v != null },
            };

            try
            {
                options.Parse(args);

                if (showHelp)
                {
                    ShowUsage(options);
                    return null;
                }

                if (!config.IsValid(out var validationErrors))
                {
                    Console.Error.WriteLine("Configuration validation failed:");
                    foreach (var error in validationErrors)
                    {
                        Console.Error.WriteLine($"  - {error}");
                    }
                    return null;
                }

                return config;
            }
            catch (OptionException ex)
            {
                Console.Error.WriteLine($"Command line parsing error: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Creates and configures service dependencies using ASP.NET Core DI
        /// </summary>
        /// <returns>Service provider with all services registered</returns>
        private static ServiceProvider CreateServiceProvider()
        {
            var services = new ServiceCollection();

            // Register services as singletons
            services.AddSingleton<FileService>();
            services.AddSingleton<TemplateService>();
            services.AddSingleton<FlatBufferCompilerService>();
            services.AddSingleton<CodeGenerationService>();
            services.AddSingleton<FlatBufferProcessor>();

            return services.BuildServiceProvider();
        }

        /// <summary>
        /// Shows usage information
        /// </summary>
        /// <param name="options">Option set for detailed help</param>
        private static void ShowUsage(OptionSet options = null)
        {
            Console.WriteLine("FlatBufferEx - Enhanced FlatBuffer code generator");
            Console.WriteLine();
            Console.WriteLine("Usage: dotnet run -- [OPTIONS]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            options?.WriteOptionDescriptions(Console.Out);
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  dotnet run -- --path ./schemas --lang \"c#\" --output ./generated");
            Console.WriteLine("  dotnet run -- --path ./schemas --lang \"c++|c#\" --output ./generated --include ./common");
        }
    }
}