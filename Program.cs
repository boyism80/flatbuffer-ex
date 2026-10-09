namespace FlatBufferEx
{
    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            var options = Options.Parse(args);
            if (options == null)
                return 1;

            try
            {
                await Generate(options);
                Console.WriteLine("FlatBuffer code generation completed successfully.");
                return 0;
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"Error: {e.Message}");
                Console.Error.WriteLine($"Stack trace: {e.StackTrace}");
                return 1;
            }
        }

        private static async Task Generate(Options options)
        {
            var outputPath = Path.GetFullPath(options.OutputPath);
            if (Directory.Exists(outputPath))
                Directory.Delete(outputPath, true);
            Directory.CreateDirectory(outputPath);

            await Flatc.Install();

            Console.WriteLine($"Parsing schema files from: {options.InputPath}");
            var context = Parser.Parse(options.InputPath);
            Console.WriteLine($"Found {context.Scopes.Count} scopes with {context.Scopes.Sum(s => s.Tables.Count)} tables and {context.Scopes.Sum(s => s.Enums.Count)} enums");

            var workPath = Directory.CreateTempSubdirectory("flatbuffer-ex-").FullName;
            try
            {
                foreach (var language in options.Languages)
                {
                    Console.WriteLine($"Generating code for language: {language.Name}");

                    // The raw schema is the flatc input. Its namespaces depend on the target language's keywords, so it is
                    // rendered again for every language.
                    var schema = Path.Combine(workPath, language.FlatcOption, "protocol_raw.fbs");
                    Directory.CreateDirectory(Path.GetDirectoryName(schema));
                    await File.WriteAllTextAsync(schema, new SchemaGenerator(context, language).Render());
                    await Flatc.Compile(language, schema, Path.Combine(outputPath, "raw", language.Name), context.Scopes.Select(x => x.Namespace));

                    Generator generator = language == Language.Cpp ? new CppGenerator(context, options.IncludePath) : new CSharpGenerator(context);
                    var languageOutputPath = Path.Combine(outputPath, language.Name);
                    Directory.CreateDirectory(languageOutputPath);
                    var file = Path.Combine(languageOutputPath, "protocol" + language.Extension);
                    await File.WriteAllTextAsync(file, generator.Render());
                    Console.WriteLine($"  Generated: {file}");
                }
            }
            finally
            {
                Directory.Delete(workPath, true);
            }
        }
    }
}
