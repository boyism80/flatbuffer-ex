using FlatBufferEx.Model;
using FlatBufferEx.Services;
using Scriban;

namespace FlatBufferEx
{
    /// <summary>
    /// Static generator class for creating FlatBuffer schema content
    /// Generates raw FlatBuffer (.fbs) content for tables and enums using Scriban templates
    /// </summary>
    [Obsolete("Use ITemplateService instead. This class is kept for backward compatibility.")]
    public static class Generator
    {
        // Pre-compiled Scriban templates for generating raw FlatBuffer content
        private static readonly Lazy<Template> RawTableTemplate = new(() => Template.Parse(File.ReadAllText("Template/raw.table.txt")));
        private static readonly Lazy<Template> RawEnumTemplate = new(() => Template.Parse(File.ReadAllText("Template/raw.enum.txt")));

        /// <summary>
        /// Generates raw FlatBuffer table content using the table template
        /// </summary>
        /// <param name="table">Table model to generate content for</param>
        /// <param name="lang">Target language for code generation</param>
        /// <returns>Generated FlatBuffer table content as string</returns>
        public static string RawFlatBufferTableContents(Model.Table table, string lang)
        {
            // Create Scriban context with table and language data
            var obj = new ScribanEx();
            obj.Add("table", table);
            obj.Add("lang", lang);
            var ctx = new TemplateContext();
            ctx.PushGlobal(obj);

            // Render the template with the context
            return RawTableTemplate.Value.Render(ctx);
        }

        /// <summary>
        /// Generates raw FlatBuffer enum content using the enum template
        /// </summary>
        /// <param name="e">Enum model to generate content for</param>
        /// <param name="lang">Target language for code generation</param>
        /// <returns>Generated FlatBuffer enum content as string</returns>
        public static string RawFlatBufferEnumContents(Model.Enum e, string lang)
        {
            // Create Scriban context with enum and language data
            var obj = new ScribanEx();
            obj.Add("enum", e);
            obj.Add("lang", lang);
            var ctx = new TemplateContext();
            ctx.PushGlobal(obj);
            
            // Render the template with the context
            return RawEnumTemplate.Value.Render(ctx);
        }
    }
}
