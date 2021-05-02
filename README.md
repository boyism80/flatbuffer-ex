# flatwrapper

## Introduce
내가 쓰려고 만들었다. 실력을 쌓고 거만해지자. 겸손을 피하자. 기만하지 말고 위선떨지 말자.

## Declaration
```
namespace flatbuffer.response;

table Equipment {
	id: ulong;
	name: string;
	type: int;
}

table Item {
	id: ulong;
	name: string;
}

table Items {
	inventory: [Item];
	equipment: [Equipment];
	gold: ulong;
}
```

## Use in Go
```
items := response.Items{
	Inventory: []response.Item{
		{
			Id:   100,
			Name: "item 0",
		},
		{
			Id:   101,
			Name: "item 1",
		},
	},
	Equipment: []response.Equipment{
		{
			Id:   200,
			Name: "equip 0",
			Type: 210,
		},
		{
			Id:   201,
			Name: "equip 1",
			Type: 220,
		},
	},
	Gold: 10000,
}
bytes := items.Serialize()
fmt.Println(bytes)

des := response.Items{}
des.Deserialize(bytes)
fmt.Println(des)
```


## Use in C#
```
static void Main(string[] args)
{
    var items = new Items
    {
        Inventory = new List<Item>
        {
            new Item
            { Id = 0, Name = "item 0" },
            new Item
            { Id = 1, Name = "item 1" }
        },
        Equipment = new List<Equipment>
        {
            new Equipment
            { Id = 0, Name = "Equipment 0", Type = 0 },
            new Equipment
            { Id = 1, Name = "Equipment 1", Type = 1 }
        },
        Gold = 1000
    };

    var bytes = items.Serialize();
    var item2 = Items.Deserialize(bytes);
}
```