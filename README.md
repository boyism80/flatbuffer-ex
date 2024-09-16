# flatwrapper

## Introduce
C++이랑 C#만 됨 (내가쓰려고 ㅋ)

## Declaration
```
// monster.fbs

namespace fb.game.protocol;

enum Color:byte { Red = 0, Green, Blue = 2 }
 
table Vec3 {
  x:float;
  y:float;
  z:float;
}
 
table Weapon {
  name:string;
  damage:short;
}

table Monster {
  pos:Vec3; // Struct.
  mana:short = 150;
  hp:short = 100;
  name:string;
  friendly:bool = false (deprecated);
  inventory:[ubyte];  // Vector of scalars.
  color:Color = Blue; // Enum.
  weapons:[Weapon];   // Vector of tables.
  path:[Vec3];        // Vector of structs.
}
```

```
// response.fbs

include "monster.fbs";

namespace fb.game.protocol;

table Response {
  monsters: [Monster];
}
 
root_type Response;
```

## Use in C++
```
// Move monster.h and response.h into path "include/model"
// main.cpp

#include "include/model/response.h"

using namespace fb::game::protocol::model;

int main(int argc, const char** argv)
{
    auto monster = Monster();
    monster.pos = { 1, 2, 3 };
    monster.hp = 100;
    monster.mana = 150;
    monster.name = "orc";
    monster.inventory = { 1, 2, 3, 4, 5 };
    monster.color = fb::game::protocol::Color_Blue;
    monster.weapons =
    {
        Weapon("sword", 10), Weapon("Axe", 20)
    };
    monster.path =
    {
        {1, 2, 3},
        {4, 5, 6}
    };

    auto response = Response();
    response.monsters.push_back(monster);

    auto bytes = response.Serialize();
    auto deserialized = Response::Deserialize(bytes.data());
    return 0;
}
```

## Use in C#
```
// Move Monster.cs and Response.cs into path "fb/game/protocol/model"
// Program.cs

using fb.game.protocol.model;

var response = new Response
{
    Monsters = new List<Monster>
    {
        new Monster
        {
            Pos = new Vec3 { X = 1.0f, Y = 2.0f, Z = 3.0f },
            Weapons = new List<Weapon>
            { 
                new Weapon { Name = "Sword", Damage = 3 },
                new Weapon { Name = "Axe", Damage = 5 } 
            },
            Name = "Orc",
            Inventory = new List<byte> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 
            Hp = 100,
            Mana = 150, 
            Color = fb.game.protocol.Color.Red,
            Path = new List<Vec3> { new Vec3 { X = 1.0f, Y = 2.0f, Z = 3.0f } }
        },
        new Monster
        {
            Pos = new Vec3 { X = 4.0f, Y = 5.0f, Z = 6.0f },
            Weapons = new List<Weapon> 
            { 
                new Weapon { Name = "Sword", Damage = 3 },
                new Weapon { Name = "Axe", Damage = 5 }
            },
            Name = "Spider",
            Inventory = new List<byte> { 0, 1, 2, 3, 4, 5, 6, 7, 8, 9 }, 
            Hp = 300,
            Mana = 20, 
            Color = fb.game.protocol.Color.Blue,
            Path = new List<Vec3> { new Vec3 { X = 4.0f, Y = 5.0f, Z = 6.0f } }
        }
    }
};
var bytes = response.Serialize();
response = Response.Deserialize(bytes);

```