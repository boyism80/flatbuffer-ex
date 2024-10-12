using Newtonsoft.Json;
using Scriban.Runtime;

namespace FlatBufferEx.Model
{
    public class ScribanEx : ScriptObject
    {
        public ScribanEx()
        {
        }

        public static IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> KeywordMap { get; private set; } = new Dictionary<string, IReadOnlyDictionary<string, string>>
        {
            ["c++"] = new Dictionary<string, string>
            {
                ["class"] = "_class"
            },
            ["c#"] = new Dictionary<string, string>
            {
                ["internal"] = "_internal"
            }
        };

        public static string ToMappedKwd(string env, string value)
        {
            if (KeywordMap.TryGetValue(env, out var keywords) == false)
                return value;

            if (keywords.TryGetValue(value, out var result) == false)
                return value;

            return result;
        }

        public static Field CloneField(Field field)
        {
            var clone = JsonConvert.DeserializeObject<Field>(JsonConvert.SerializeObject(field));
            clone.Context = field.Context;
            clone.Scope = field.Scope;
            clone.Table = field.Table;
            return clone;
        }

        public static string CppMappedKwd(string value) => ToMappedKwd("c++", value);

        public static string CsMappedKwd(string value) => ToMappedKwd("c#", value);

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
