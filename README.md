# FlatBufferEx

A powerful extension tool for Google FlatBuffers that simplifies schema parsing and code generation with enhanced features like nullable types, object pooling, and multi-language support.

## Features

- **Nullable Type Support**: Add nullable fields to your FlatBuffer schemas
- **Multiple Tables per File**: Declare multiple tables in a single `.fbs` file
- **Object Pooling**: Built-in FlatBufferBuilder object pooling for better performance
- **Multi-Language Support**: Generate code for C++ and C#
- **Template-Based Generation**: Uses Scriban templates for flexible code generation
- **Simplified API**: No need to manually handle FlatBufferBuilder for serialization

## Installation

### Prerequisites
- .NET 6.0 or later
- FlatBuffers compiler (automatically downloaded in release mode)

### Build from Source
```bash
git clone https://github.com/boyism80/flatbuffer-ex.git
cd flatbuffer-ex
dotnet build
```

## Usage

### Command Line Interface
```bash
dotnet run -- --path <schema-directory> --lang <languages> --output <output-directory>
```

**Parameters:**
- `--path` or `-p`: Directory containing `.fbs` schema files
- `--lang` or `-l`: Target languages (e.g., "c++|c#")
- `--output` or `-o`: Output directory for generated code
- `--include` or `-i`: Include directory path (optional)

### Example
```bash
dotnet run -- --path ./schemas --lang "c#" --output ./generated
```

## Schema Definition

### Basic Schema
```flatbuffers
// db.fbs
namespace fb.protocol.db;

table Character {
    name: string;
    weapon_color: ubyte?;  // Nullable field
    armor_color: ubyte?;
    level: uint = 1;       // Default value
}

table Item {
    model: uint;
    durability: uint?;     // Nullable field
    custom_name: string?;
}

table Spell {
    id: uint;
    name: string;
}
```

### Including Other Schemas
```flatbuffers
// db.response.fbs
include "db.fbs";
namespace fb.protocol.db.response;

table Login {
    character: fb.protocol.db.Character;
    items: [fb.protocol.db.Item];      // Array of items
    spells: [fb.protocol.db.Spell];
}
```

## Generated Code Usage

### C# Example
```csharp
using fb.protocol.db;
using System.Diagnostics;

var response = new fb.protocol.db.response.Login
{ 
    Character = new Character
    { 
        Name = "character",
        WeaponColor = null,
        ArmorColor = 1,
    },
    Items = new List<Item>
    { 
        new Item
        { 
            Model = 1,
            Durability = null
        },
        new Item
        { 
            Model = 2,
            Durability = 100
        }
    }
};

var bytes = response.Serialize();
var deserialized = fb.protocol.db.response.Login.Deserialize(bytes);

Debug.Assert(deserialized.Character.Name == "character");
Debug.Assert(deserialized.Character.WeaponColor == null);
Debug.Assert(deserialized.Character.ArmorColor == 1);

Debug.Assert(deserialized.Items.Count == 2);
Debug.Assert(deserialized.Items[0].Model == 1);
Debug.Assert(deserialized.Items[0].Durability == null);
Debug.Assert(deserialized.Items[1].Model == 2);
Debug.Assert(deserialized.Items[1].Durability == 100);

Debug.Assert(deserialized.Spells.Count == 0);
```

### C++ Example
```cpp
#include <protocol.h> // Generated header file

int main(int argc, const char** argv)
{
    auto character = fb::protocol::db::Character{};
    character.name = "character";
    character.weapon_color = std::nullopt;
    character.armor_color = 1;

    auto item1 = fb::protocol::db::Item{};
    item1.model = 1;
    item1.durability = std::nullopt;

    auto item2 = fb::protocol::db::Item{};
    item2.model = 2;
    item2.durability = 100;

    auto response = fb::protocol::db::response::Login
    {
        character,
        { item1, item2 }, 
        {}
    };

    auto bytes = response.Serialize();
    auto deserialized = fb::protocol::db::response::Login::Deserialize(bytes.data());

    assert(deserialized.character.name == "character");
    assert(deserialized.character.weapon_color == std::nullopt);
    assert(deserialized.character.armor_color == 1);

    assert(deserialized.items.size() == 2);
    assert(deserialized.items[0].model == 1);
    assert(deserialized.items[0].durability == std::nullopt);
    assert(deserialized.items[1].model == 2);
    assert(deserialized.items[1].durability == 100);

    assert(deserialized.spells.size() == 0);

    return 0;
}
```

## Performance Features

### Object Pooling (C#)
FlatBufferEx automatically uses object pooling for FlatBufferBuilder instances to reduce garbage collection pressure:

```csharp
// Object pooling is handled automatically in Serialize() method
var bytes = myObject.Serialize(); // Uses pooled FlatBufferBuilder
```

The pool configuration:
- Initial buffer size: 4KB
- Maximum retained objects: 256
- Thread-safe implementation

## Architecture

### Core Components
- **Parser**: Parses `.fbs` schema files and extracts structure information
- **Generator**: Creates raw FlatBuffer files and generates target language code
- **Model**: Represents parsed schema elements (tables, fields, enums)
- **Templates**: Scriban templates for code generation

### Template System
FlatBufferEx uses Scriban templates for flexible code generation:
- `Template/c#.txt`: C# code generation template
- `Template/cpp.txt`: C++ code generation template
- `Template/raw.table.txt`: Raw table generation template
- `Template/raw.enum.txt`: Raw enum generation template

## Supported Types

### Primitive Types
- `byte`, `ubyte`, `bool`
- `short`, `ushort`, `int`, `uint`
- `long`, `ulong`, `float`, `double`
- `string`

### Complex Types
- **Arrays**: `[type]` (e.g., `[int]`, `[string]`)
- **Nullable**: `type?` (e.g., `int?`, `string?`)
- **Custom Tables**: References to other tables
- **Enums**: Custom enumeration types

## Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests if applicable
5. Submit a pull request

## License

This project is licensed under the MIT License - see the LICENSE file for details.

## Related Projects

This tool is used in the [fb](https://github.com/boyism80/fb) 2D MMORPG game server project.
