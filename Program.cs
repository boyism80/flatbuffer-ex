using FlatBufferEx.Model;
using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;

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
                    await File.WriteAllTextAsync(schema, Render("raw.txt", context, options.IncludePath, Naming.Schema(language)));
                    await Flatc.Compile(language, schema, Path.Combine(outputPath, "raw", language.Name), context.Scopes.Select(x => x.Namespace));

                    var languageOutputPath = Path.Combine(outputPath, language.Name);
                    Directory.CreateDirectory(languageOutputPath);
                    var file = Path.Combine(languageOutputPath, "protocol" + language.Extension);
                    await File.WriteAllTextAsync(file, Render(language.Template, context, options.IncludePath, Naming.Code(language)));
                    Console.WriteLine($"  Generated: {file}");
                }
            }
            finally
            {
                Directory.Delete(workPath, true);
            }
        }
        private static string Render(string templateName, Context context, string includePath, Naming naming)
        {
            var path = Path.Combine(Options.TemplateDirectory, templateName);
            var template = Template.Parse(File.ReadAllText(path), path);
            if (template.HasErrors)
                throw new InvalidOperationException($"Template parsing errors in {path}:{Environment.NewLine}{string.Join(Environment.NewLine, template.Messages)}");

            var globals = new ScriptObject
            {
                ["context"] = context,
                ["include_path"] = includePath,
            };
            globals.Import("name", new Func<object, string>(naming.Name));
            globals.Import("full_name", new Func<object, string>(naming.FullName));
            globals.Import("raw_name", new Func<object, string>(naming.RawName));
            globals.Import("ns", new Func<Scope, string>(naming.Namespace));
            globals.Import("raw_ns", new Func<Scope, string>(naming.RawNamespace));
            globals.Import("upper_camel", new Func<string, string>(UpperCamel));

            var templateContext = new TemplateContext
            {
                TemplateLoader = new TemplateLoader(),
                LoopLimit = 0,
            };
            templateContext.PushGlobal(globals);
            return template.Render(templateContext);
        }
        private static string UpperCamel(string value)
        {
            if (value == null)
                return null;

            return string.Concat(value.ToLower().Split('_', StringSplitOptions.RemoveEmptyEntries).Select(x => char.ToUpperInvariant(x[0]) + x.Substring(1)));
        }

        private sealed class TemplateLoader : ITemplateLoader
        {
            public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName) => Path.Combine(Options.TemplateDirectory, templateName);
            public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath) => File.ReadAllText(templatePath);
            public ValueTask<string> LoadAsync(TemplateContext context, SourceSpan callerSpan, string templatePath) => new(File.ReadAllText(templatePath));
        }
    }
}