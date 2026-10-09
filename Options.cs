using NDesk.Options;

namespace FlatBufferEx
{
    public sealed class Options
    {
        public static readonly string TemplateDirectory = Path.Combine(AppContext.BaseDirectory, "Template");

        public string InputPath { get; private set; } = string.Empty;
        public string OutputPath { get; private set; } = "output";
        public string IncludePath { get; private set; } = string.Empty;
        public List<Language> Languages { get; } = new();

        public static Options Parse(string[] args)
        {
            var options = new Options();
            var languages = "c#";
            var help = false;

            var set = new OptionSet
            {
                { "p|path=", "input directory containing .fbs files", v => options.InputPath = v },
                { "l|lang=", "target languages (e.g., \"c++|c#\")", v => languages = v },
                { "o|output=", "output directory for generated code", v => options.OutputPath = v },
                { "i|include=", "include directory of the raw header", v => options.IncludePath = v },
                { "h|help", "show this help message", v => help = v != null },
            };

            try
            {
                set.Parse(args);
            }
            catch (OptionException e)
            {
                Console.Error.WriteLine($"Command line parsing error: {e.Message}");
                return null;
            }

            if (help)
            {
                PrintUsage(set);
                return null;
            }

            var errors = new List<string>();
            if (string.IsNullOrWhiteSpace(options.InputPath))
                errors.Add("Input path is required");
            else if (!Directory.Exists(options.InputPath))
                errors.Add($"Input directory does not exist: {options.InputPath}");

            if (string.IsNullOrWhiteSpace(options.OutputPath))
                errors.Add("Output path is required");

            if (!Directory.Exists(TemplateDirectory))
                errors.Add($"Template directory does not exist: {TemplateDirectory}");

            var names = languages.Split('|').Select(x => x.Trim().ToLower()).Where(x => x != string.Empty).Distinct().ToList();
            if (names.Count == 0)
                errors.Add("At least one target language must be specified");

            foreach (var name in names)
            {
                if (Language.All.TryGetValue(name, out var language))
                    options.Languages.Add(language);
                else
                    errors.Add($"Unsupported language: {name}. Supported languages: {string.Join(", ", Language.All.Keys)}");
            }

            if (errors.Count > 0)
            {
                Console.Error.WriteLine("Configuration validation failed:");
                foreach (var error in errors)
                    Console.Error.WriteLine($"  - {error}");
                PrintUsage(set);
                return null;
            }

            return options;
        }

        private static void PrintUsage(OptionSet set)
        {
            Console.WriteLine("FlatBufferEx - Enhanced FlatBuffer code generator");
            Console.WriteLine();
            Console.WriteLine("Usage: dotnet run -- [OPTIONS]");
            Console.WriteLine();
            Console.WriteLine("Options:");
            set.WriteOptionDescriptions(Console.Out);
            Console.WriteLine();
            Console.WriteLine("Examples:");
            Console.WriteLine("  dotnet run -- --path ./schemas --lang \"c#\" --output ./generated");
            Console.WriteLine("  dotnet run -- --path ./schemas --lang \"c++|c#\" --output ./generated --include fb/protocol/flatbuffer/raw");
        }
    }
}
