using FlatBufferEx.Model;
using Scriban;

namespace FlatBufferEx
{
    public static class Generator
    {
        private static readonly Template RawTableTemplate = Template.Parse(File.ReadAllText("Template/raw.table.txt"));
        private static readonly Template RawEnumTemplate = Template.Parse(File.ReadAllText("Template/raw.enum.txt"));


        public static string RawFlatBufferTableContents(Model.Table table, string lang)
        {
            var obj = new ScribanEx();
            obj.Add("table", table);
            obj.Add("lang", lang);
            var ctx = new TemplateContext();
            ctx.PushGlobal(obj);

            return RawTableTemplate.Render(ctx);
        }

        public static string RawFlatBufferEnumContents(Model.Enum e, string lang)
        {
            var obj = new ScribanEx();
            obj.Add("enum", e);
            obj.Add("lang", lang);
            var ctx = new TemplateContext();
            ctx.PushGlobal(obj);
            return RawEnumTemplate.Render(ctx);
        }
    }
}
