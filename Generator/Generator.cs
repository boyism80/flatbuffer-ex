using FlatBufferEx.Model;
using Scriban;
using Scriban.Parsing;
using Scriban.Runtime;
using Enum = FlatBufferEx.Model.Enum;

namespace FlatBufferEx
{
    // Names are spelled for the output: Name is the identifier, SchemaName the name as written in the schema,
    // FullName the qualified generated type and RawName the qualified flatc type.
    public record ScopeModel(string Namespace, string RawNamespace, List<TableModel> Tables, List<EnumModel> Enums);
    public record TableModel(string Name, string SchemaName, string FullName, string RawName, List<object> Fields);
    public record EnumModel(string Name, string SchemaName, string FullName, string RawName, string Type, List<EnumValueModel> Values);
    public record EnumValueModel(string Name, string SchemaName, string Value);

    // Renders one template. The base builds the scopes, tables and enums; each output only builds the view model of a field.
    public abstract class Generator
    {
        protected Context Context { get; }
        protected Naming Naming { get; }
        protected IReadOnlyDictionary<Table, TableModel> Tables { get; private set; }

        protected Generator(Context context, Naming naming)
        {
            Context = context;
            Naming = naming;
        }

        protected abstract string Template { get; }

        // Underlying type of an enum.
        protected abstract string Scalar(string name);
        protected abstract object Field(Field field);

        protected virtual void Globals(ScriptObject globals)
        { }

        protected string FullName(Table table) => Naming.Qualify(table.Scope.Namespace, table.Name);
        protected string FullName(Enum e) => Naming.Qualify(e.Scope.Namespace, e.Name);
        protected string RawName(Table table) => Naming.Qualify(table.Scope.Namespace.Raw, table.Name);
        protected string RawName(Enum e) => Naming.Qualify(e.Scope.Namespace.Raw, e.Name);

        public string Render()
        {
            Tables = Context.Tables.ToDictionary(x => x, x => new TableModel(Naming.Identifier(x.Name), x.Name, FullName(x), RawName(x), x.Fields.Select(Field).ToList()));
            var scopes = Context.Scopes.Select(scope => new ScopeModel(
                Naming.Namespace(scope.Namespace),
                Naming.Namespace(scope.Namespace.Raw),
                scope.Tables.Select(x => Tables[x]).ToList(),
                scope.Enums.Select(x => new EnumModel(
                    Naming.Identifier(x.Name),
                    x.Name,
                    FullName(x),
                    RawName(x),
                    Scalar(x.Type),
                    x.Values.Select(v => new EnumValueModel(Naming.Identifier(v.Name), v.Name, v.Value)).ToList())).ToList())).ToList();

            var globals = new ScriptObject { ["scopes"] = scopes };
            Globals(globals);

            var path = Path.Combine(Options.TemplateDirectory, Template);
            var template = Scriban.Template.Parse(File.ReadAllText(path), path);
            if (template.HasErrors)
                throw new InvalidOperationException($"Template parsing errors in {path}:{Environment.NewLine}{string.Join(Environment.NewLine, template.Messages)}");

            var context = new TemplateContext
            {
                TemplateLoader = new TemplateLoader(),
                LoopLimit = 0,
            };
            context.PushGlobal(globals);
            return template.Render(context);
        }

        private sealed class TemplateLoader : ITemplateLoader
        {
            public string GetPath(TemplateContext context, SourceSpan callerSpan, string templateName) => Path.Combine(Options.TemplateDirectory, templateName);
            public string Load(TemplateContext context, SourceSpan callerSpan, string templatePath) => File.ReadAllText(templatePath);
            public ValueTask<string> LoadAsync(TemplateContext context, SourceSpan callerSpan, string templatePath) => new(File.ReadAllText(templatePath));
        }
    }
}
