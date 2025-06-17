using Newtonsoft.Json;
using Scriban.Runtime;

namespace FlatBufferEx.Model
{
    /// <summary>
    /// Extended ScriptObject for Scriban template engine
    /// Provides utility functions and language-specific keyword mapping for code generation
    /// </summary>
    public class ScribanEx : ScriptObject
    {
        public ScribanEx()
        {
        }

        /// <summary>
        /// Mapping of reserved keywords for different programming languages
        /// Used to avoid conflicts with language-specific reserved words
        /// </summary>
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

        /// <summary>
        /// Maps a value to its language-specific alternative if it's a reserved keyword
        /// </summary>
        /// <param name="env">Target environment/language (e.g., "c++", "c#")</param>
        /// <param name="value">Value to check and potentially map</param>
        /// <returns>Mapped value or original value if not a reserved keyword</returns>
        public static string ToMappedKwd(string env, string value)
        {
            if (KeywordMap.TryGetValue(env, out var keywords) == false)
                return value;

            if (keywords.TryGetValue(value, out var result) == false)
                return value;

            return result;
        }

        /// <summary>
        /// Creates a deep clone of a Field object
        /// Preserves object references that can't be serialized
        /// </summary>
        /// <param name="field">Field to clone</param>
        /// <returns>Cloned Field object</returns>
        public static Field CloneField(Field field)
        {
            var clone = JsonConvert.DeserializeObject<Field>(JsonConvert.SerializeObject(field));
            clone.Context = field.Context;
            clone.Scope = field.Scope;
            clone.Table = field.Table;
            if (field.ArrayElement != null)
                clone.ArrayElement = CloneField(field.ArrayElement);

            return clone;
        }

        /// <summary>
        /// Maps a value for C++ keyword conflicts
        /// </summary>
        /// <param name="value">Value to map</param>
        /// <returns>C++-safe value</returns>
        public static string CppMappedKwd(string value) => ToMappedKwd("c++", value);

        /// <summary>
        /// Maps a value for C# keyword conflicts
        /// </summary>
        /// <param name="value">Value to map</param>
        /// <returns>C#-safe value</returns>
        public static string CsMappedKwd(string value) => ToMappedKwd("c#", value);

        /// <summary>
        /// Converts a string to UpperCamelCase (PascalCase)
        /// Handles underscore-separated words
        /// </summary>
        /// <param name="value">String to convert</param>
        /// <returns>UpperCamelCase string</returns>
        public static string UpperCamel(string value)
        {
            if (value == null)
                return null;

            return value.ToLower().Split(new[] { "_" }, StringSplitOptions.RemoveEmptyEntries).Select(s => char.ToUpperInvariant(s[0]) + s.Substring(1, s.Length - 1)).Aggregate(string.Empty, (s1, s2) => s1 + s2);
        }

        /// <summary>
        /// Converts a string to lowerCamelCase
        /// First converts to UpperCamelCase, then lowercases the first character
        /// </summary>
        /// <param name="value">String to convert</param>
        /// <returns>lowerCamelCase string</returns>
        public static string LowerCamel(string value)
        {
            value = UpperCamel(value);
            if (string.IsNullOrEmpty(value))
                return value;

            return char.ToLowerInvariant(value[0]) + value.Substring(1, value.Length - 1);
        }

        /// <summary>
        /// Checks if a field is an array type
        /// </summary>
        /// <param name="field">Field to check</param>
        /// <returns>True if field is an array</returns>
        public static bool IsArray(Field field)
        {
            return field.Type == "array";
        }

        /// <summary>
        /// Checks if a field is not an array type
        /// </summary>
        /// <param name="field">Field to check</param>
        /// <returns>True if field is not an array</returns>
        public static bool IsNonArray(Field field)
        {
            return !IsArray(field);
        }

        /// <summary>
        /// Checks if a type is a primitive type
        /// </summary>
        /// <param name="type">Type name to check</param>
        /// <returns>True if type is primitive</returns>
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

        /// <summary>
        /// Checks if a field is an array of custom (non-primitive) types
        /// </summary>
        /// <param name="field">Field to check</param>
        /// <returns>True if field is a custom array</returns>
        public static bool IsCustomArray(Field field)
        {
            if (IsArray(field) == false)
                return false;

            if (IsPrimeType(field.ArrayElement.Type))
                return false;

            return true;
        }

        /// <summary>
        /// Replaces C# reserved keywords with safe alternatives
        /// Legacy method for specific keyword replacement
        /// </summary>
        /// <param name="value">Value to check and replace</param>
        /// <returns>Safe value for C# usage</returns>
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
