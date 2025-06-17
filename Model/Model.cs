using Newtonsoft.Json;

namespace FlatBufferEx.Model
{
    /// <summary>
    /// Represents a field definition in a FlatBuffer table
    /// Contains information about field type, nullability, arrays, and references
    /// </summary>
    public class Field
    {
        [JsonIgnore]
        public Context Context { get; set; }
        [JsonIgnore]
        public Scope Scope { get; set; }
        [JsonIgnore]
        public Table Table { get; set; }
        
        /// <summary>
        /// Field name
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Field type (primitive, custom class, or "array")
        /// </summary>
        public string Type { get; set; }
        
        /// <summary>
        /// Default initialization value
        /// </summary>
        public string Init { get; set; }
        
        /// <summary>
        /// Namespace reference for custom types
        /// </summary>
        public List<string> ReferNamespace { get; set; }
        
        /// <summary>
        /// Element type for array fields
        /// </summary>
        public Field ArrayElement { get; set; }
        
        /// <summary>
        /// Whether this field can be null
        /// </summary>
        public bool IsNullable { get; set; }

        /// <summary>
        /// Gets all reference types used by this field (including array elements)
        /// </summary>
        /// <returns>Collection of reference type names</returns>
        public IEnumerable<string> GetReferenceTypes()
        {
            // Recursively get reference types from array elements
            if (ArrayElement != null)
            {
                foreach (var x in ArrayElement.GetReferenceTypes())
                    yield return x;
            }

            // Add reference namespace types
            if (ReferNamespace != null)
            {
                yield return string.Join('.', ReferNamespace.Concat(new[] { Type }));
            }

            // Add types from current scope
            if (Scope.Tables.Select(x => x.Name).Contains(Type))
            {
                yield return string.Join('.', Scope.Namespace.Concat(new[] { Type }));
            }
        }

        /// <summary>
        /// Unique key identifier for this field type
        /// </summary>
        [JsonIgnore]
        public string Key
        {
            get
            {
                var key = string.Empty;
                if (IsArray)
                    key = $"[{ArrayElement.Type}]";
                else
                    key = string.Join('.', ReferNamespace ?? new List<string>()) + Type;
                if (IsNullable)
                    key = $"{key}?";

                return key;
            }
        }

        /// <summary>
        /// Gets all nullable fields (excluding strings)
        /// </summary>
        [JsonIgnore]
        public IEnumerable<Field> NullableFields => AllFields.Where(x => x.IsNullable).Where(x => x.Type != "string");

        /// <summary>
        /// Gets all fields including nested array element fields
        /// </summary>
        [JsonIgnore]
        public IEnumerable<Field> AllFields
        {
            get
            {
                var visit = new HashSet<string>();
                if (ArrayElement != null)
                {
                    foreach (var field in ArrayElement.AllFields)
                    {
                        if (visit.Contains(field.Key))
                            continue;

                        visit.Add(Key);
                        yield return field;
                    }
                }

                yield return this;
            }
        }

        /// <summary>
        /// Whether this field is an array type
        /// </summary>
        [JsonIgnore]
        public bool IsArray => Type == "array";
        
        /// <summary>
        /// Whether this field is a custom class type
        /// </summary>
        [JsonIgnore]
        public bool IsCustomClass => Context.IsCustomClass(this);
        
        /// <summary>
        /// Whether this field is an enum type
        /// </summary>
        [JsonIgnore]
        public bool IsEnum => Context.IsEnum(this);
        
        /// <summary>
        /// Namespace for this field type
        /// </summary>
        [JsonIgnore]
        public IEnumerable<string> Namespace => ReferNamespace ?? Scope.Namespace;
        
        /// <summary>
        /// Whether this field is a primitive type
        /// </summary>
        [JsonIgnore]
        public bool IsPrimitive
        {
            get
            {
                switch (Type)
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
                    case "ulong":
                    case "double":
                    case "string":
                        return true;

                    default:
                        return false;
                }
            }
        }
        
        /// <summary>
        /// Whether this field or its array elements contain primitive types
        /// </summary>
        [JsonIgnore]
        public bool ContainsPrimitive
        {
            get
            {
                if (ArrayElement != null)
                {
                    if (ArrayElement.ContainsPrimitive)
                        return true;
                }

                return IsPrimitive;
            }
        }

        /// <summary>
        /// Fixed namespace for code generation
        /// </summary>
        [JsonIgnore]
        public IEnumerable<string> FixedNamespace
        {
            get
            {
                if (IsPrimitive)
                    return new List<string>();

                if (ReferNamespace != null)
                    return ReferNamespace;

                return Scope.Namespace;
            }
        }

        /// <summary>
        /// Fixed namespace with "raw" suffix for raw FlatBuffer types
        /// </summary>
        [JsonIgnore]
        public IEnumerable<string> FixedRawNamespace
        {
            get
            {
                if (IsPrimitive)
                    return new List<string>();

                return FixedNamespace.Concat(new[] { "raw" });
            }
        }

        /// <summary>
        /// Whether this field requires bound nullable wrapper
        /// </summary>
        [JsonIgnore]
        public bool IsBoundNullable
        {
            get
            {
                if (!IsNullable)
                    return false;

                if (IsCustomClass)
                    return false;

                if (Type == "string")
                    return false;

                return true;
            }
        }
    }

    /// <summary>
    /// Represents a FlatBuffer table or struct definition
    /// </summary>
    public class Table
    {
        public Context Context { get; set; }
        public Scope Scope { get; set; }
        
        /// <summary>
        /// Table type ("table" or "struct")
        /// </summary>
        public string Type { get; set; }
        
        /// <summary>
        /// Table name
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// List of fields in this table
        /// </summary>
        public List<Field> Fields { get; set; }
        
        /// <summary>
        /// Whether this is a root table
        /// </summary>
        public bool Root { get; set; }
        
        /// <summary>
        /// Namespace for this table
        /// </summary>
        public List<string> Namespace { get; set; }

        /// <summary>
        /// Whether this table contains any nullable fields
        /// </summary>
        [JsonIgnore]
        public bool ContainsNullableField => Fields.Any(x => x.IsNullable);
        
        /// <summary>
        /// Gets all reference files needed by this table
        /// </summary>
        [JsonIgnore]
        public IEnumerable<string> ReferenceFiles
        {
            get
            {
                var result = Fields.SelectMany(x => x.GetReferenceTypes()).Select(x => x.ToLower()).Distinct();
                return result;
            }
        }

        /// <summary>
        /// Gets all fields including nested fields from arrays
        /// </summary>
        [JsonIgnore]
        public IEnumerable<Field> AllFields => Fields.SelectMany(x => x.AllFields);

        /// <summary>
        /// Converts this table to a field representation
        /// </summary>
        /// <returns>Field object representing this table</returns>
        public Field ToField()
        {
            return new Field
            {
                Context = Context,
                Scope = Scope,
                Name = Name,
                Type = Name,
                ReferNamespace = Namespace,
            };
        }
    }

    /// <summary>
    /// Represents a FlatBuffer enum definition
    /// </summary>
    public class Enum
    {
        public Context Context { get; set; }
        public Scope Scope { get; set; }
        
        /// <summary>
        /// Enum name
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// Underlying enum type
        /// </summary>
        public string Type { get; set; }
        
        /// <summary>
        /// List of enum values
        /// </summary>
        public List<string> Values { get; set; }
    }

    /// <summary>
    /// Represents a FlatBuffer union definition
    /// </summary>
    public class Union
    {
        /// <summary>
        /// Union name
        /// </summary>
        public string Name { get; set; }
        
        /// <summary>
        /// List of union member types
        /// </summary>
        public List<string> Values { get; set; }
    }

    /// <summary>
    /// Represents a scope (namespace) containing tables and enums
    /// </summary>
    public class Scope
    {
        public Context Context { get; set; }
        
        /// <summary>
        /// Source file name
        /// </summary>
        public string FileName { get; set; }
        
        /// <summary>
        /// Namespace components
        /// </summary>
        public List<string> Namespace { get; set; }
        
        /// <summary>
        /// List of included files
        /// </summary>
        public List<string> IncludeFiles { get; set; }
        
        /// <summary>
        /// Tables defined in this scope
        /// </summary>
        public List<Table> Tables { get; set; }
        
        /// <summary>
        /// Enums defined in this scope
        /// </summary>
        public List<Enum> Enums { get; set; }
    }

    /// <summary>
    /// Root context containing all parsed FlatBuffer definitions
    /// </summary>
    public class Context
    {
        /// <summary>
        /// All parsed scopes
        /// </summary>
        public List<Scope> Scopes { get; set; }

        /// <summary>
        /// Determines if a field represents a custom class type
        /// </summary>
        /// <param name="field">Field to check</param>
        /// <returns>True if field is a custom class</returns>
        public bool IsCustomClass(Field field)
        {
            if (field.IsPrimitive)
                return false;

            if (field.IsArray)
                return false;

            foreach (var scope in Scopes)
            {
                if (field.ReferNamespace != null)
                {
                    if (!field.ReferNamespace.SequenceEqual(scope.Namespace))
                        continue;
                }
                else
                {
                    if (!field.Scope.Namespace.SequenceEqual(scope.Namespace))
                        continue;
                }

                if (scope.Tables.Select(x => x.Name).Contains(field.Type))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Determines if a field represents an enum type
        /// </summary>
        /// <param name="field">Field to check</param>
        /// <returns>True if field is an enum</returns>
        public bool IsEnum(Field field)
        {
            if (field.IsPrimitive)
                return false;

            if (field.IsArray)
                return false;

            foreach (var scope in Scopes)
            {
                if (field.ReferNamespace != null)
                {
                    if (!field.ReferNamespace.SequenceEqual(scope.Namespace))
                        continue;
                }
                else
                {
                    if (!field.Scope.Namespace.SequenceEqual(scope.Namespace))
                        continue;
                }

                if (scope.Enums.Select(x => x.Name).Contains(field.Type))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Gets all nullable fields across all scopes (excluding strings and arrays)
        /// </summary>
        public IEnumerable<Field> NullableFields => AllFields.Where(x => x.IsNullable && x.Type != "string" && x.Type != "array");

        /// <summary>
        /// Gets all fields across all scopes and tables
        /// </summary>
        public IEnumerable<Field> AllFields => Scopes.SelectMany(x => x.Tables).SelectMany(x => x.AllFields).GroupBy(x => x.Key).Select(g => g.First());

        /// <summary>
        /// Gets all array fields across all scopes
        /// </summary>
        public IEnumerable<Field> ArrayFields => AllFields.Where(x => x.IsArray);
    }

    /// <summary>
    /// Information about a FlatBuffer file
    /// </summary>
    public class FlatBufferFileInfo
    {
        /// <summary>
        /// File name
        /// </summary>
        public string File { get; set; }
        
        /// <summary>
        /// Root type name
        /// </summary>
        public string RootType { get; set; }
        
        /// <summary>
        /// Namespace components
        /// </summary>
        public List<string> Namespace { get; set; }
        
        /// <summary>
        /// Included files
        /// </summary>
        public List<string> Includes { get; set; } = new List<string>();
        
        /// <summary>
        /// Tables in this file
        /// </summary>
        public List<Table> Tables { get; set; } = new List<Table>();
        
        /// <summary>
        /// Enums in this file
        /// </summary>
        public List<Enum> Enums { get; set; } = new List<Enum>();
    }
}
