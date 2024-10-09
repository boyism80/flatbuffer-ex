using FlatBufferEx.Model;
using Scriban;

namespace FlatBufferEx
{
    public static class Generator
    {
        private static readonly Template RawTableTemplate = Template.Parse(File.ReadAllText("Template/raw.table.txt"));
        private static readonly Template RawEnumTemplate = Template.Parse(File.ReadAllText("Template/raw.enum.txt"));


        public static string RawFlatBufferTableContents(Table table)
        {
            return RawTableTemplate.Render(table);
        }

        public static string RawFlatBufferEnumContents(Model.Enum e)
        {
            return RawEnumTemplate.Render(e);
        }
    }
}
