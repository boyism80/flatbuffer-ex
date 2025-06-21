using FlatBufferEx.Model;

namespace FlatBufferEx.Services
{
    /// <summary>
    /// Interface for template operations
    /// </summary>
    public interface ITemplateService
    {
        /// <summary>
        /// Renders a template for raw FlatBuffer table content
        /// </summary>
        /// <param name="table">Table model</param>
        /// <param name="language">Target language</param>
        /// <returns>Rendered content</returns>
        Task<string> RenderTableTemplateAsync(Table table, string language);

        /// <summary>
        /// Renders a template for raw FlatBuffer enum content
        /// </summary>
        /// <param name="enumModel">Enum model</param>
        /// <param name="language">Target language</param>
        /// <returns>Rendered content</returns>
        Task<string> RenderEnumTemplateAsync(Model.Enum enumModel, string language);

        /// <summary>
        /// Renders a template for nullable field content
        /// </summary>
        /// <param name="field">Field model</param>
        /// <returns>Rendered content</returns>
        Task<string> RenderNullableTemplateAsync(Field field);

        /// <summary>
        /// Renders the main language template
        /// </summary>
        /// <param name="context">Parsing context</param>
        /// <param name="language">Target language</param>
        /// <param name="includePath">Include path</param>
        /// <returns>Rendered content</returns>
        Task<string> RenderLanguageTemplateAsync(Context context, string language, string includePath);
    }
} 