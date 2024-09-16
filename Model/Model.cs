namespace FlatBufferEx.Model
{
    public class Field
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string Init { get; set; }
        public Field ArrayElement { get; set; }
        public bool Deprecated { get; set; }
    }

    public class Table
    {
        public string Type { get; set; }
        public string Name { get; set; }
        public List<Field> Fields { get; set; }
    }

    public class Enum
    { 
        public string Name { get; set; }
        public string Type { get; set; }
        public List<string> Values { get; set; }
    }

    public class Union
    { 
        public string Name { get; set; }
        public List<string> Values { get; set; }
    }

    public class FlatBufferFileInfo
    {
        public string File { get; set; }
        public string OutputDir { get; set; }
        public string RootType { get; set; }
        public List<string> Namespace { get; set; }
        public List<string> Includes { get; set; } = new List<string>();
        public List<Table> Tables { get; set; } = new List<Table>();
        public List<Enum> Enums { get; set; } = new List<Enum>();
    }
}
