#include "flatbuffers/flatbuffers.h"
#include <string>
#include <vector>

namespace fb { namespace game { namespace protocol { namespace model { 

class Weapon
{
public:
    std::string name;
    int16_t damage;

public:
    Weapon()
    { }

    Weapon(const std::string& name, int16_t damage)
        : name(name), damage(damage)
    { }

    Weapon(const fb::game::protocol::Weapon& raw)
        : name(raw.name()), damage(raw.damage())
    {
    }


public:
    flatbuffers::Offset<fb::game::protocol::Weapon> Build(flatbuffers::FlatBufferBuilder& builder)
    {
        return fb::game::protocol::CreateWeapon(builder,
            builder.CreateString(this->name),
            this->damage);
    }
};

class Monster
{
public:
    Vec3 pos;
    int16_t mana;
    int16_t hp;
    std::string name;
    std::vector<uint8_t> inventory;
    fb::game::protocol::Color color;
    std::vector<Weapon> weapons;
    std::vector<Vec3> path;

public:
    Monster()
    { }

    Monster(const Vec3& pos, int16_t mana, int16_t hp, const std::string& name, const std::vector<uint8_t>& inventory, fb::game::protocol::Color color, const std::vector<Weapon>& weapons, const std::vector<Vec3>& path)
        : pos(pos), mana(mana), hp(hp), name(name), inventory(inventory), color(color), weapons(weapons), path(path)
    { }

    Monster(const fb::game::protocol::Monster& raw)
        : pos(raw.pos()), mana(raw.mana()), hp(raw.hp()), name(raw.name()), color(raw.color())
    {
        for (int i = 0; i < raw.inventory()->size(); i++)
            this->inventory.push_back(*raw.inventory()->Get(i));

        for (int i = 0; i < raw.weapons()->size(); i++)
            this->weapons.push_back(*raw.weapons()->Get(i));

        for (int i = 0; i < raw.path()->size(); i++)
            this->path.push_back(*raw.path()->Get(i));
    }

private:
    std::vector<flatbuffers::Offset<fb::game::protocol::Weapon>> CreateWeapons(flatbuffers::FlatBufferBuilder& builder)
    {
        auto result = std::vector<flatbuffers::Offset<fb::game::protocol::Weapon>>();
        for(auto& x : this->weapons)
        {
            result.push_back(x.Build(builder));
        }

        return result;
    }

    std::vector<flatbuffers::Offset<fb::game::protocol::Vec3>> CreatePath(flatbuffers::FlatBufferBuilder& builder)
    {
        auto result = std::vector<flatbuffers::Offset<fb::game::protocol::Vec3>>();
        for(auto& x : this->path)
        {
            result.push_back(x.Build(builder));
        }

        return result;
    }

public:
    flatbuffers::Offset<fb::game::protocol::Monster> Build(flatbuffers::FlatBufferBuilder& builder)
    {
        return fb::game::protocol::CreateMonster(builder,
            this->pos,
            this->mana,
            this->hp,
            builder.CreateString(this->name),
            this->CreateInventory(builder),
            this->color,
            this->CreateWeapons(builder),
            this->CreatePath(builder));
    }
};

} } } }