using Scriban.Runtime;

namespace FlatBufferEx.Model
{
    public class ScribanEx : ScriptObject
    {
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
    }
}
