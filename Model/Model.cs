using Google.FlatBuffers;

namespace FlatBufferEx.Model
{
    public class Field
    {
        public Context Context { get; set; }
        public Scope Scope { get; set; }
        public Table Table { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public string Init { get; set; }
        public List<string> ReferNamespace { get; set; }
        public Field ArrayElement { get; set; }
        public bool IsNullable { get; set; }

        public IEnumerable<string> GetReferenceTypes()
        {
            if (ArrayElement != null)
            {
                foreach (var x in ArrayElement.GetReferenceTypes())
                    yield return x;
            }

            if (ReferNamespace != null)
            {
                yield return string.Join('.', ReferNamespace.Concat(new[] { Type }));
            }

            if (Scope.Tables.Select(x => x.Name).Contains(Type))
            {
                yield return string.Join('.', Scope.Namespace.Concat(new[] { Type }));
            }
        }

        public string Key => string.Join('.', ReferNamespace ?? new List<string>()) + Type;

        public IEnumerable<Field> NullableFields
        {
            get
            {
                var visit = new HashSet<string>();
                if (ArrayElement != null)
                {
                    foreach (var field in ArrayElement.NullableFields)
                    {
                        if (visit.Contains(field.Key))
                            continue;

                        visit.Add(Key);
                        yield return field;
                    }
                }

                if (IsNullable)
                {
                    if (visit.Contains(Key) == false)
                        yield return this;
                }
            }
        }

        public bool IsArray => Type == "array";
        public bool IsCustomClass => Context.IsCustomClass(this);
        public bool IsEnum => Context.IsEnum(this);
        public IEnumerable<string> Namespace => ReferNamespace ?? Scope.Namespace;
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

        public IEnumerable<string> FixedRawNamespace
        {
            get
            {
                if (IsPrimitive)
                    return new List<string>();

                return FixedNamespace.Concat(new[] { "raw" });
            }
        }
    }

    public class Table
    {
        public Context Context { get; set; }
        public Scope Scope { get; set; }
        public string Type { get; set; }
        public string Name { get; set; }
        public List<Field> Fields { get; set; }
        public bool Root { get; set; }
        public List<string> Namespace { get; set; }

        public bool ContainsNullableField => Fields.Any(x => x.IsNullable);
        public IEnumerable<string> ReferenceFiles
        {
            get
            {
                var result = Fields.SelectMany(x => x.GetReferenceTypes()).Select(x => x.ToLower()).Distinct();
                return result;
            }
        }

        public IEnumerable<Field> NullableFields
        {
            get
            {
                var visit = new HashSet<string>();
                foreach (var field in Fields.SelectMany(x => x.NullableFields))
                {
                    if (visit.Contains(field.Key))
                        continue;

                    visit.Add(field.Key);
                    yield return field;
                }
            }
        }
    }

    public class Enum
    { 
        public Context Context { get; set; }
        public Scope Scope { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public List<string> Values { get; set; }
    }

    public class Union
    { 
        public string Name { get; set; }
        public List<string> Values { get; set; }
    }

    public class Scope
    { 
        public Context Context { get; set; }
        public string FileName { get; set; }
        public List<string> Namespace { get; set; }
        public List<string> IncludeFiles { get; set; }
        public List<Table> Tables { get; set; }
        public List<Enum> Enums { get; set; }
    }

    public class Context
    { 
        public List<Scope> Scopes { get; set; }

        public bool IsCustomClass(Field field)
        {
            foreach (var scope in Scopes)
            {
                foreach (var table in scope.Tables)
                {
                    if (table.Name != field.Type)
                        continue;

                    if (field.Scope == scope)
                        return true;

                    var refer1 = string.Join('.', scope.Namespace ?? new List<string>());
                    var refer2 = string.Join('.', field.ReferNamespace ?? new List<string>());
                    if (refer1 == refer2)
                        return true;
                }
            }

            return false;
        }

        public bool IsEnum(Field field)
        {
            foreach (var scope in Scopes)
            {
                foreach (var e in scope.Enums)
                {
                    if (e.Name != field.Type)
                        continue;

                    if (field.Scope == scope)
                        return true;

                    var refer1 = string.Join('.', scope.Namespace ?? new List<string>());
                    var refer2 = string.Join('.', field.ReferNamespace ?? new List<string>());
                    if (refer1 == refer2)
                        return true;
                }
            }

            return false;
        }

        public IEnumerable<Field> NullableFields
        {
            get
            {
                var visit = new HashSet<string>();

                foreach (var scope in Scopes)
                {
                    foreach (var field in scope.Tables.SelectMany(x => x.NullableFields))
                    {
                        if (visit.Contains(field.Key))
                            continue;

                        visit.Add(field.Key);
                        yield return field;
                    }
                }
            }
        }
    }

    public class FlatBufferFileInfo
    {
        public string File { get; set; }
        public string RootType { get; set; }
        public List<string> Namespace { get; set; }
        public List<string> Includes { get; set; } = new List<string>();
        public List<Table> Tables { get; set; } = new List<Table>();
        public List<Enum> Enums { get; set; } = new List<Enum>();
    }
}
