using FlatBufferEx.Model;
using Scriban;

namespace FlatBufferEx.Services
{
    /// <summary>
    /// Implementation of template operations using Scriban
    /// </summary>
    public class TemplateService
    {
        private readonly FileService _fileService;
        private readonly Dictionary<string, Template> _templateCache;

        public TemplateService(FileService fileService)
        {
            _fileService = fileService ?? throw new ArgumentNullException(nameof(fileService));
            _templateCache = new Dictionary<string, Template>();
        }

        /// <summary>
        /// Renders the raw FlatBuffer schema containing every enum, nullable wrapper and table
        /// </summary>
        public async Task<string> RenderRawTemplateAsync(Context context, string language)
        {
            var template = await GetTemplateAsync("Template/raw.txt");
            var scribanEx = new ScribanEx
            {
                ["context"] = context,
                ["lang"] = language
            };
            var ctx = new TemplateContext();
            ctx.PushGlobal(scribanEx);

            return await template.RenderAsync(ctx);
        }

        /// <summary>
        /// Renders the main language template
        /// </summary>
        public async Task<string> RenderLanguageTemplateAsync(Context context, string language, string includePath)
        {
            var templatePath = language switch
            {
                "c++" => "Template/cpp.txt",
                "c#" => "Template/c#.txt",
                _ => throw new ArgumentException($"Unsupported language: {language}")
            };

            var template = await GetTemplateAsync(templatePath);

            var scribanEx = new ScribanEx();
            scribanEx.Add("context", context);
            scribanEx.Add("include_path", includePath);

            var ctx = new TemplateContext();
            ctx.PushGlobal(scribanEx);

            return await template.RenderAsync(ctx);
        }

        /// <summary>
        /// Gets a template from cache or loads it from file
        /// </summary>
        /// <param name="templatePath">Template file path</param>
        /// <returns>Compiled template</returns>
        private async Task<Template> GetTemplateAsync(string templatePath)
        {
            if (_templateCache.TryGetValue(templatePath, out var cachedTemplate))
            {
                return cachedTemplate;
            }

            var templateContent = await _fileService.ReadAllTextAsync(templatePath);
            var template = Template.Parse(templateContent);

            if (template.HasErrors)
            {
                var errors = string.Join(Environment.NewLine, template.Messages.Select(m => m.ToString()));
                throw new InvalidOperationException($"Template parsing errors in {templatePath}:{Environment.NewLine}{errors}");
            }

            _templateCache[templatePath] = template;
            return template;
        }
    }
}