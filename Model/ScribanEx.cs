using Scriban.Runtime;

namespace FlatBufferEx.Model
{
    public class ScribanEx : ScriptObject
    {
        private static List<Model.Enum> _enums = new List<Enum>();
        private static List<Model.Table> _tables = new List<Table>();

        public ScribanEx(IEnumerable<FlatBufferFileInfo> infos)
        {
            _enums = infos.SelectMany(x => x.Enums).ToList();
            _tables = infos.SelectMany(x => x.Tables).ToList();
        }

        public static bool IsEnum(object x)
        {
            var type = string.Empty;
            List<string> currentNamespace;
            List<string> referencedNamespace;
            switch (x)
            {
                case ScriptObject so:
                    type = so.GetSafeValue<string>("type");
                    referencedNamespace = so.GetSafeValue<List<string>>("refer_namespace");
                    currentNamespace = so.GetSafeValue<List<string>>("namespace");
                    break;

                case Model.Field f:
                    type = f.Type;
                    referencedNamespace = f.ReferNamespace;
                    currentNamespace = f.Namespace;
                    break;

                default:
                    return false;
            }

            foreach (var e in _enums)
            {
                if (type != e.Name)
                    continue;

                var ns1 = string.Join('.', referencedNamespace ?? new List<string>());
                if (string.IsNullOrEmpty(ns1))
                    ns1 = string.Join('.', currentNamespace);

                var ns2 = string.Join('.', e.Namespace ?? new List<string>());
                if (ns1 != ns2)
                    continue;

                return true;
            }

            return false;
        }

        public static bool IsClassType(object x)
        {
            var name = string.Empty;
            var type = string.Empty;
            switch (x)
            {
                case ScriptObject so:
                    name = so.GetSafeValue<string>("name");
                    type = so.GetSafeValue<string>("type");
                    break;

                case Model.Field f:
                    name = f.Name;
                    type = f.Type;
                    break;
            }

            if (IsEnum(new Field { Name = name }))
                return false;

            if (IsPrimeType(type))
                return false;

            return true;
        }

        public static bool IsCustomTable(object x)
        {
            var name = string.Empty;
            var type = string.Empty;
            switch (x)
            {
                case ScriptObject so:
                    name = so.GetSafeValue<string>("name");
                    type = so.GetSafeValue<string>("type");
                    break;

                case Model.Field f:
                    name = f.Name;
                    type = f.Type;
                    break;
            }
            return _tables.Select(x => x.Name).Contains(type);
        }

        public static string UpperCamel(string value)
        {
            if (value == null)
                return null;

            return value.ToLower().Split(new[] { "_" }, StringSplitOptions.RemoveEmptyEntries).Select(s => char.ToUpperInvariant(s[0]) + s.Substring(1, s.Length - 1)).Aggregate(string.Empty, (s1, s2) => s1 + s2);
        }

        public static string LowerCamel(string value)
        {
            value = UpperCamel(value);
            if (string.IsNullOrEmpty(value))
                return value;

            return char.ToLowerInvariant(value[0]) + value.Substring(1, value.Length - 1);
        }

        public static bool IsArray(Field field)
        {
            return field.Type == "array";
        }

        public static bool IsNonArray(Field field)
        {
            return !IsArray(field);
        }

        public static bool IsPrimeType(string type)
        {
            switch (type.Trim().ToLower())
            {
                case "byte":
                case "ubyte":
                case "bool":
                case "short":
                case "ushort":
                case "int":
                case "uint":
                case "float":
                case "long":
                    return true;

                default:
                    return false;
            }
        }

        public static bool IsCustomArray(Field field)
        {
            if (IsArray(field) == false)
                return false;

            if (IsPrimeType(field.ArrayElement.Type))
                return false;

            return true;
        }

        public static string CsReplaceReservedKeyword(string value)
        {
            switch (value)
            {
                case "internal":
                    return "inter";

                default:
                    return value;
            }
        }
    }
}
